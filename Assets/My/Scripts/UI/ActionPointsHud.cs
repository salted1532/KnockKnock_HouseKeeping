using UnityEngine;
using UnityEngine.UI;

// 새벽 행동력 HUD. Watch 밑에 두는 4칸 바 — 1칸 = 행동력 1. ActionPoints.OnChanged 구독.
// pips 에 이미지 4개를 순서대로 연결하면, 남은 행동력만큼 왼쪽부터 filledColor 로 켜진다.
public class ActionPointsHud : MonoBehaviour
{
    [SerializeField] private Image[] pips = new Image[4];
    [SerializeField] private Color filledColor = Color.yellow;
    [SerializeField] private Color emptyColor = new(1f, 1f, 1f, 0.25f);

    private bool subscribed;

    private void OnEnable() => TrySubscribe();
    private void Start() => TrySubscribe();

    // 새벽에만 보이게. LunchTasks/MorningTasks 와 동일 폴링 패턴 — 자기 자신은 계속 켜둔 채 pip만 토글.
    private void Update()
    {
        bool dawn = DayPhaseManager.Instance != null && DayPhaseManager.Instance.Current == DayPhase.Dawn;
        foreach (var p in pips)
            if (p != null && p.gameObject.activeSelf != dawn) p.gameObject.SetActive(dawn);
    }

    private void OnDisable()
    {
        if (subscribed && ActionPoints.Instance != null) ActionPoints.Instance.OnChanged -= Refresh;
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed || ActionPoints.Instance == null) return;
        ActionPoints.Instance.OnChanged += Refresh;
        subscribed = true;
        Refresh(ActionPoints.Instance.Current, ActionPoints.Instance.Max);
    }

    private void Refresh(int current, int max)
    {
        for (int i = 0; i < pips.Length; i++)
            if (pips[i] != null)
                pips[i].color = i < current ? filledColor : emptyColor;
    }
}
