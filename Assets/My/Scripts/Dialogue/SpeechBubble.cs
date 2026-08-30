using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// NPC 머리 오른쪽 위의 월드 스페이스 말풍선. root(캔버스) 를 켜고 끄며, 켜져 있는 동안 MainCamera 로 빌보드.
// 타이핑 연출 내장. 클릭/E/Space 로 즉시 완성 → 다음 줄.
// InGame 은 월드가 RenderTexture 경유지만 MainCamera 가 같이 그리므로 좌표 변환 불필요.
public class SpeechBubble : MonoBehaviour
{
    [Tooltip("켜고 끌 말풍선 루트 (World Space Canvas 또는 스크린 대화 패널). 시작 시 자동으로 꺼짐")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text label;
    [Tooltip("선택 — 손님 이름/구별용. 있으면 NpcData.displayName 표시")]
    [SerializeField] private TMP_Text nameLabel;
    [Tooltip("선택 — 있으면 줄별 표정 스프라이트로 교체")]
    [SerializeField] private Image portrait;
    [Tooltip("월드 말풍선이면 켜기(매 프레임 카메라 향해 회전). 스크린 스페이스 대화 패널이면 끄기")]
    [SerializeField] private bool billboard = true;
    [Tooltip("빌보드시킬 트랜스폼. 비우면 root 의 트랜스폼")]
    [SerializeField] private Transform billboardTarget;
    [Tooltip("비우면 Camera.main")]
    [SerializeField] private Camera faceCamera;
    [SerializeField] private float charInterval = 0.03f;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        else Debug.LogWarning($"[SpeechBubble] '{name}' root 미할당", this);
    }

    private void LateUpdate()
    {
        if (!billboard || root == null || !root.activeSelf) return;
        var cam = faceCamera != null ? faceCamera : Camera.main;
        if (cam == null) return;
        var t = billboardTarget != null ? billboardTarget : root.transform;
        Vector3 dir = t.position - cam.transform.position;
        if (dir.sqrMagnitude > 0.0001f)
            t.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    public IEnumerator Show(NpcData npc, IReadOnlyList<DialogueLine> lines)
    {
        if (lines == null || lines.Count == 0) yield break;
        if (root != null) root.SetActive(true);
        if (nameLabel != null) nameLabel.text = npc != null ? npc.DisplayName : "";

        foreach (var line in lines)
        {
            if (portrait != null && npc != null)
            {
                var s = npc.Portrait(line.expression);
                portrait.sprite = s;
                portrait.enabled = s != null;
            }
            DialogueRunner.Instance?.RaiseLineShown(npc, line);

            yield return TypeLine(line.Text);
            yield return WaitForAdvance();
        }
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    // 접객 일시정지용 — 대화 상태(코루틴/버튼)는 그대로 두고 루트만 껐다 켠다.
    public bool IsVisible => root != null && root.activeSelf;
    public void SetVisible(bool v)
    {
        if (root != null) root.SetActive(v);
    }

    private IEnumerator TypeLine(string text)
    {
        label.text = text ?? "";
        label.ForceMeshUpdate();
        int total = label.textInfo.characterCount;
        label.maxVisibleCharacters = 0;

        float acc = 0f;
        int shown = 0;
        while (shown < total)
        {
            if (DialogueRunner.Instance != null && DialogueRunner.Instance.Paused) { yield return null; continue; }
            if (AdvancePressed()) break;          // 즉시 완성
            acc += Time.deltaTime;
            while (acc >= charInterval && shown < total) { acc -= charInterval; shown++; }
            label.maxVisibleCharacters = shown;
            yield return null;
        }
        label.maxVisibleCharacters = total;
        yield return null;   // 완성시킨 그 입력이 곧바로 다음 대기로 넘어가지 않게 한 프레임 소비
    }

    private IEnumerator WaitForAdvance()
    {
        while (!AdvancePressed()) yield return null;
        yield return null;
    }

    private static bool AdvancePressed()
    {
        // 접객 일시정지(ESC) 중엔 월드 클릭이 대사 넘김으로 새지 않게 무시.
        if (DialogueRunner.Instance != null && DialogueRunner.Instance.Paused) return false;

        var m = Mouse.current;
        var k = Keyboard.current;
        return (m != null && m.leftButton.wasPressedThisFrame)
            || (k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame));
    }
}
