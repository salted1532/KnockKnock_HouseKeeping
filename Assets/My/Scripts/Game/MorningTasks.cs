using TMPro;
using UnityEngine;

// 아침 할일(우상단 HUD). 지금은 "침대 개기" 1종 — 씬의 모든 RoomController 가 이번 아침에
// 흐트러뜨린 침대(랜덤 1~2/방)를 합산해 "☐ 침대 개기 3/6" 로 표시한다.
// 전부 개면 AllDone → 게시판의 TasksCompleteCondition 이 열려 점심 전환 가능.
// (완료 시 "게시판으로 가자" 안내는 게시판의 ObjectiveMarker 가 담당 — doc/0144)
// 아침이 아니면 패널을 끈다. 폴링 방식(매 프레임 방 순회) — Bed/이벤트 배선 0.
public class MorningTasks : MonoBehaviour
{
    public static MorningTasks Instance { get; private set; }

    [Tooltip("아침에만 켜지는 할일 패널 루트 (보통 이 컴포넌트가 붙은 오브젝트)")]
    [SerializeField] private GameObject panel;
    [Tooltip("\"☐ 침대 개기 3/6\" 를 그릴 텍스트")]
    [SerializeField] private TMP_Text line;

    private RoomController[] rooms;

    // ponytail: 매 프레임 10방 순회 + FindObjectsByType 1회 캐시. 방이 수백 개 되면 이벤트로.
    private RoomController[] Rooms =>
        rooms ??= FindObjectsByType<RoomController>(FindObjectsInactive.Include);

    private int Total { get { int n = 0; foreach (var r in Rooms) n += r.MessyTotal; return n; } }
    private int Made  { get { int n = 0; foreach (var r in Rooms) n += r.MessyDone;  return n; } }

    public bool AllDone =>
        DayPhaseManager.Instance != null
        && DayPhaseManager.Instance.Current == DayPhase.Morning
        && Made >= Total;

    private void Awake() => Instance = this;

    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Update()
    {
        bool morning = DayPhaseManager.Instance != null
                       && DayPhaseManager.Instance.Current == DayPhase.Morning;

        // 이 컴포넌트는 항상 켜진 오브젝트에 두고, panel(자식) 을 토글한다.
        // panel 미지정/자기 자신이면 텍스트만 토글 (자기 자신을 끄면 Update 가 멈춘다).
        var vis = panel != null && panel != gameObject ? panel
                : line != null ? line.gameObject : null;
        if (vis != null && vis.activeSelf != morning) vis.SetActive(morning);
        if (!morning || line == null) return;

        int total = Total, made = Made;
        string box = made >= total ? "☑" : "☐";   // ☑ / ☐
        line.text = $"{box} {LocalizationManager.T("Make beds", "침대 개기")} {made}/{total}";
    }
}
