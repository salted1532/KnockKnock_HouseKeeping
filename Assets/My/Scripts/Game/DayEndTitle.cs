using System.Collections;
using TMPro;
using UnityEngine;

// 하루가 끝나 다음 날 아침으로 넘어간 직후(OnPhaseChangeFinished · Morning) 화면 중앙에
// "N일차" 를 크게 1회. N = 이제 시작하는 일차(= DayCount, HUD PhaseLabel 과 일치). 게임 시작 아침(1일차)엔 안 뜬다.
// 자기 오브젝트에 CanvasGroup + 큰 중앙 TMP_Text 를 두고 붙인다 (HUD Canvas 아래, 풀스크린 권장).
[RequireComponent(typeof(CanvasGroup))]
public class DayEndTitle : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text label;
    [SerializeField] private float fadeIn = 0.5f;
    [SerializeField] private float hold = 1.8f;
    [SerializeField] private float fadeOut = 0.9f;

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
    }

    private void Start()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChangeFinished += Handle;
        else Debug.LogWarning("[DayEndTitle] DayPhaseManager 없음 — 일차 타이틀 안 뜸", this);
    }

    private void OnDestroy()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChangeFinished -= Handle;
    }

    private void Handle(DayPhase phase)
    {
        if (phase != DayPhase.Morning) return;
        int day = DayPhaseManager.Instance.DayCount;
        if (day < 2) return;   // 게임 시작 아침(1일차)엔 표시 안 함

        if (label != null)
            label.text = LocalizationManager.Korean ? $"{day}일차" : $"Day {day}";

        StopAllCoroutines();
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        yield return To(1f, fadeIn);
        yield return new WaitForSeconds(hold);
        yield return To(0f, fadeOut);
    }

    private IEnumerator To(float target, float dur)
    {
        float from = group != null ? group.alpha : 0f;
        for (float t = 0f; t < dur && dur > 0f; t += Time.deltaTime)
        {
            if (group != null) group.alpha = Mathf.Lerp(from, target, t / dur);
            yield return null;
        }
        if (group != null) group.alpha = target;
    }
}
