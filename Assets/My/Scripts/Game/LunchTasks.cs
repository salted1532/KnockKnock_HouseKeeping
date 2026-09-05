using TMPro;
using UnityEngine;

// 점심 할일(우상단 HUD). 씬의 모든 LunchTaskTarget(울타리 수리·불법주차 신고 등)을 합산해
// "☐ 점심 일과 처리 1/2" 로 표시한다. 전부 처리하면 AllDone → 접객 테이블의
// LunchTasksCompleteCondition 이 열려 저녁 전환 가능. 폴링 방식(MorningTasks 와 동일 패턴, doc/0144).
public class LunchTasks : MonoBehaviour
{
    public static LunchTasks Instance { get; private set; }

    [Tooltip("점심에만 켜지는 할일 패널 루트 (보통 이 컴포넌트가 붙은 오브젝트)")]
    [SerializeField] private GameObject panel;
    [Tooltip("\"☐ 점심 일과 처리 1/2\" 를 그릴 텍스트")]
    [SerializeField] private TMP_Text line;

    private LunchTaskTarget[] targets;

    // ponytail: 매 프레임 씬 순회 + FindObjectsByType 1회 캐시. 태스크 오브젝트가 많아지면 이벤트로.
    private LunchTaskTarget[] Targets =>
        targets ??= FindObjectsByType<LunchTaskTarget>(FindObjectsInactive.Include);

    private int Total => Targets.Length;
    private int Done { get { int n = 0; foreach (var t in Targets) if (t.IsDone) n++; return n; } }

    public bool AllDone =>
        DayPhaseManager.Instance != null
        && DayPhaseManager.Instance.Current == DayPhase.Noon
        && Done >= Total;

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (DayPhaseManager.Instance != null) DayPhaseManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    // 아침으로 전환될 때(=새 날) 모든 태스크를 재활용: doc 요청대로 일단은 같은 오브젝트를 다시 씀.
    private void Start()
    {
        if (DayPhaseManager.Instance != null) DayPhaseManager.Instance.OnPhaseChanged += HandlePhaseChanged;
    }

    private void HandlePhaseChanged(DayPhase phase)
    {
        if (phase != DayPhase.Morning) return;
        foreach (var t in Targets) t.ResetForNewDay();
    }

    private void Update()
    {
        bool noon = DayPhaseManager.Instance != null
                    && DayPhaseManager.Instance.Current == DayPhase.Noon;

        var vis = panel != null && panel != gameObject ? panel
                : line != null ? line.gameObject : null;
        if (vis != null && vis.activeSelf != noon) vis.SetActive(noon);
        if (!noon || line == null) return;

        int total = Total, done = Done;
        string box = done >= total ? "☑" : "☐";
        line.text = $"{box} {LocalizationManager.T("Handle lunch tasks", "점심 일과 처리")} {done}/{total}";
    }
}
