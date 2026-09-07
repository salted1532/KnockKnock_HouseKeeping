using TMPro;
using UnityEngine;

// 접객 신분증 확인 컨트롤러. 항상 켜져 있는 오브젝트(Canvas 등)에 붙이고 데스크의 ID 오브젝트를 참조한다.
// 평소엔 idObject 가 비활성 — 보이지도 클릭되지도 않는다.
// 대화에서 "id_show" 노드에 도달하면 idObject 를 켜고 ID_image 텍스트를 그 손님 신분증 정보로 채운다.
// 이번 접객에서 그 손님의 판정이 나면(승인 → CheckIn / 거절) 바로 치운다.
public class ReceptionIdCard : MonoBehaviour
{
    [Tooltip("데스크에 놓이는 신분증 오브젝트 (Interactable + ShowPanelEffect). 시작 시 자동으로 SetActive(false)")]
    [SerializeField] private GameObject idObject;
    [Tooltip("신분증 이미지 안의 정보 텍스트 — Canvas/ID_image/Text (TMP)")]
    [SerializeField] private TMP_Text infoText;
    [Tooltip("신분증 사진 — Canvas/ID_image/profile_Image 의 PortraitFaceCrop (선택)")]
    [SerializeField] private PortraitFaceCrop profilePhoto;
    [Tooltip("이 nodeKey 에 도달하면 손님이 신분증을 낸다")]
    [SerializeField] private string triggerNode = "id_show";

    private bool subscribed;
    private NpcData shownFor;        // 지금 신분증을 보여주고 있는 손님
    private Verdict baseline;        // id_show 시점의 판정 (재방문 손님의 과거 판정 무시용)

    private void Awake()
    {
        if (idObject != null) idObject.SetActive(false);
        else Debug.LogWarning("[ReceptionIdCard] idObject 미할당", this);
    }

    private void OnEnable() => TrySubscribe();
    private void Start() => TrySubscribe();

    private void OnDestroy()
    {
        if (subscribed && DialogueRunner.Instance != null)
            DialogueRunner.Instance.OnNodeReached -= HandleNode;
    }

    private void TrySubscribe()
    {
        if (subscribed || DialogueRunner.Instance == null) return;
        DialogueRunner.Instance.OnNodeReached += HandleNode;
        subscribed = true;
    }

    private void Update()
    {
        if (idObject == null || !idObject.activeSelf) return;

        // 다음 손님으로 넘어갔거나 세션 종료 → 치움 (안전망)
        var cur = ReceptionManager.Instance != null ? ReceptionManager.Instance.CurrentGuest : null;
        if (cur != shownFor) { Hide(); return; }

        // 이번 접객에서 이 손님의 판정이 확정됨 (승인 = CheckIn / 거절) → 치움
        var v = GuestManager.Instance != null ? (GuestManager.Instance.Get(shownFor)?.verdict ?? Verdict.None) : Verdict.None;
        if (v != baseline && (v == Verdict.Approved || v == Verdict.Rejected)) Hide();
    }

    private void HandleNode(NpcData npc, string nodeKey)
    {
        if (nodeKey != triggerNode || idObject == null) return;
        shownFor = npc;
        baseline = GuestManager.Instance != null ? (GuestManager.Instance.Get(npc)?.verdict ?? Verdict.None) : Verdict.None;
        Populate(npc);
        idObject.SetActive(true);
    }

    private void Hide()
    {
        shownFor = null;
        if (idObject == null) return;
        var panel = idObject.GetComponent<ShowPanelEffect>();
        if (panel != null) panel.Close();   // 열려 있으면 닫기 (안 열렸으면 no-op)
        idObject.SetActive(false);
    }

    private void Populate(NpcData npc)
    {
        if (infoText == null || npc == null) return;
        var c = npc.idCard;
        string name = !string.IsNullOrEmpty(c.name) ? c.name : npc.DisplayName;
        string dob = !string.IsNullOrEmpty(c.birthDate) ? c.birthDate : "----.--.--";
        infoText.text = LocalizationManager.Korean
            ? $"성    명\n  {name}\n\n생년월일\n  {dob}"
            : $"NAME\n  {name}\n\nDATE OF BIRTH\n  {dob}";

        if (profilePhoto != null) profilePhoto.Show(npc.neutralPortrait, npc.faceRect01);
    }
}
