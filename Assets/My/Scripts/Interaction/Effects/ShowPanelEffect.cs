using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// "읽기" 상호작용: 노트/편지/사진 등. 상호작용 시 지정한 오브젝트를 켜고 플레이어를 정지시킨다
// (이동·시야 정지 + 커서 표시 — UIInteractionMode.FreezeForOverlay). ESC 로 닫는다.
// content 는 UI 이미지/패널이든, 별도 Canvas 든, 3D 오브젝트든 아무 GameObject 여도 됨.
// UI면 닫기 버튼/클릭 요소를 둬도 됨 (커서가 떠 있으므로).
public class ShowPanelEffect : InteractionEffect
{
    [Tooltip("상호작용 시 켤 오브젝트 (UI 이미지·패널, 별도 Canvas, 3D 오브젝트 등). 시작 시 자동으로 꺼짐. 필수")]
    [FormerlySerializedAs("panel")]
    [SerializeField] private GameObject content;

    private bool open;

    private static int openCount;
    private static int lastCloseFrame = -1;

    // UI 모드(접객/모니터) 의 ESC 처리보다 노트가 우선. 노트가 열려 있거나
    // 이번 프레임에 ESC 로 방금 닫혔으면 true → 그 ESC 는 노트가 소비, UI 모드는 그대로 유지.
    public static bool ConsumesEsc => openCount > 0 || lastCloseFrame == Time.frameCount;

    // Domain Reload 를 꺼도 플레이 시작마다 static 초기화 (안 그러면 openCount 가 새서 ESC 가 영영 막힘)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        openCount = 0;
        lastCloseFrame = -1;
    }

    private void Awake()
    {
        if (content != null) content.SetActive(false);
        else Debug.LogWarning($"[ShowPanelEffect] '{name}' content 미할당", this);
    }

    public override void Play(in InteractionContext ctx)
    {
        if (open) Close(); else Open();
    }

    private void Update()
    {
        if (open && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    private void Open()
    {
        open = true;
        openCount++;
        if (content != null) content.SetActive(true);
        Freeze(true);
    }

    // content 의 닫기 버튼(Unity UI)에서 호출해도 됨. content.SetActive(false) 직접 호출은 금지 (퍼즈 안 풀림).
    public void Close()
    {
        if (!open) return;
        open = false;
        openCount = Mathf.Max(0, openCount - 1);
        lastCloseFrame = Time.frameCount;
        if (content != null) content.SetActive(false);
        Freeze(false);
    }

    private void Freeze(bool on)
    {
        if (UIInteractionMode.Instance != null)
            UIInteractionMode.Instance.FreezeForOverlay(on);
        else
        {
            Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = on;
        }
    }

    private void OnDisable()
    {
        if (open) Close();
    }
}
