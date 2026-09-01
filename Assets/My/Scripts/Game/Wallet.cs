using System;
using UnityEngine;

// 플레이어 소지금. 씬에 싱글턴 오브젝트로 배치 (GuestManager 옆).
// 접객 대화의 선불/후불/2배 선택 → ReceptionManager 가 Add 호출. HUD·사운드는 MoneyHud 가 OnChanged 구독.
public class Wallet : MonoBehaviour
{
    public static Wallet Instance { get; private set; }

    [SerializeField] private int startingBalance = 100;
    [Tooltip("기본 1박 요금 ($). 낡은 시골 독립 모텔")]
    [SerializeField] private int roomRate = 70;

    public int Balance { get; private set; }
    public int RoomRate => roomRate;

    // (delta, newBalance) — delta>0 = 입금(사운드 트리거). HUD 갱신용.
    public event Action<int, int> OnChanged;

    // Enter Play Mode Options(도메인 리로드 끔) 대비 — 매 재생 시작 시 static 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        Instance = this;
        Balance = startingBalance;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start() => OnChanged?.Invoke(0, Balance);   // HUD 초기 표시 (구독 순서 방어)

    // amount 음수 = 지출 (지출도 여기로 — 현금음은 MoneyHud 가 delta>0 일 때만).
    public void Add(int amount)
    {
        if (amount == 0) return;
        Balance += amount;
        OnChanged?.Invoke(amount, Balance);
    }
}
