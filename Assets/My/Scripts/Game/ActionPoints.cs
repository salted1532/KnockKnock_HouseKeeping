using System;
using UnityEngine;

// 새벽 행동력. 새벽이 시작될 때마다 perDawn 으로 초기화, 손님과 대화할 때마다 KnockEffect 가 Use(1) 호출.
// 0 이 돼야 ActionPointsDepletedCondition 이 열려 침대로 새벽을 끝낼 수 있다.
// Can_Coke 등 "즉시 다 쓴 걸로" 아이템은 ForceDeplete 로 우회. HUD 는 ActionPointsHud 가 OnChanged 구독.
public class ActionPoints : MonoBehaviour
{
    public static ActionPoints Instance { get; private set; }

    [SerializeField] private int perDawn = 4;

    public int Max => perDawn;
    public int Current { get; private set; }

    // (current, max) — HUD 갱신용.
    public event Action<int, int> OnChanged;

    // Enter Play Mode Options(도메인 리로드 끔) 대비 — 매 재생 시작 시 static 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        Instance = this;
        Current = perDawn;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (DayPhaseManager.Instance != null) DayPhaseManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void Start()
    {
        if (DayPhaseManager.Instance != null) DayPhaseManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        OnChanged?.Invoke(Current, Max);   // HUD 초기 표시 (구독 순서 방어)
    }

    private void HandlePhaseChanged(DayPhase phase)
    {
        if (phase != DayPhase.Dawn) return;
        Current = perDawn;
        OnChanged?.Invoke(Current, Max);
    }

    public void Use(int amount = 1)
    {
        if (amount <= 0 || Current <= 0) return;
        Current = Mathf.Max(0, Current - amount);
        OnChanged?.Invoke(Current, Max);
    }

    // Can_Coke 등 소비 아이템용: 남은 행동력을 즉시 0으로 만든다.
    public void ForceDeplete()
    {
        if (Current == 0) return;
        Current = 0;
        OnChanged?.Invoke(Current, Max);
    }
}
