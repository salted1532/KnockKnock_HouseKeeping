using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum DayPhase { Morning, Noon, Evening, Dawn }

// 하루 진행: 아침 → 점심 → 저녁(접객) → 새벽. 다음 날 아침으로 순환.
public class DayPhaseManager : MonoBehaviour
{
    public static DayPhaseManager Instance { get; private set; }

    [SerializeField] private DayPhase startPhase = DayPhase.Morning;
    [SerializeField] private bool debugAdvanceKey = true;   // 디버그: N 키로 다음 단계

    public DayPhase Current { get; private set; }
    public int DayCount { get; private set; } = 1;
    public event Action<DayPhase> OnPhaseChanged;

    private void Awake()
    {
        Instance = this;
        Current = startPhase;
    }

    private void Start() => OnPhaseChanged?.Invoke(Current);

    private void Update()
    {
        if (debugAdvanceKey && Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
            Advance();
    }

    public void Advance()
    {
        if (Current == DayPhase.Dawn) DayCount++;
        Current = (DayPhase)(((int)Current + 1) % 4);
        OnPhaseChanged?.Invoke(Current);
    }
}
