using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum DayPhase { Morning, Noon, Evening, Dawn }

// 하루 진행: 아침 → 점심 → 저녁(접객) → 새벽. 다음 날 아침으로 순환.
// 전환은 ScreenFader 를 거친다: 암전 시점에 Current 갱신 + OnPhaseChanged,
// 페이드 인 완료 후 OnPhaseChangeFinished. ScreenFader 없으면 즉시.
public class DayPhaseManager : MonoBehaviour
{
    public static DayPhaseManager Instance { get; private set; }

    [SerializeField] private DayPhase startPhase = DayPhase.Morning;
    [SerializeField] private bool debugAdvanceKey = true;   // 디버그: N 또는 Q 로 다음 단계

    public DayPhase Current { get; private set; }
    public int DayCount { get; private set; } = 1;
    public bool Transitioning { get; private set; }

    public event Action<DayPhase> OnPhaseChanged;          // 암전 중 (게이트·비주얼용)
    public event Action<DayPhase> OnPhaseChangeFinished;   // 페이드 인 완료 후 (후속 연출용)

    private void Awake()
    {
        Instance = this;
        Current = startPhase;
    }

    private void Start() => OnPhaseChanged?.Invoke(Current);

    private void Update()
    {
        if (!debugAdvanceKey || Keyboard.current == null) return;
        if (Keyboard.current.nKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame)
            Advance();
    }

    // 순환상 다음 단계로 (디버그 N/Q, 접객 종료 등). 보통은 PhaseSwitchEffect 가 TransitionTo 를 명시.
    public void Advance() => TransitionTo((DayPhase)(((int)Current + 1) % 4));

    // target 단계로 전환 (페이드 경유). 이미 전환 중이거나 같은 단계면 무시.
    public void TransitionTo(DayPhase target)
    {
        if (Transitioning || target == Current) return;
        Transitioning = true;

        // 전환 중엔 플레이어 조작 정지 (암전 동안 돌아다니거나 다른 상호작용 못 하게).
        // UI 모드로 진입한 경우(접객)엔 FreezeForOverlay 가 알아서 무시하고, 해제도 안 함.
        UIInteractionMode.Instance?.FreezeForOverlay(true);

        void AtBlack()
        {
            if (target == DayPhase.Morning) DayCount++;   // 아침으로 진입 = 새 날
            Current = target;
            OnPhaseChanged?.Invoke(Current);
        }

        void Done()
        {
            Transitioning = false;
            UIInteractionMode.Instance?.FreezeForOverlay(false);
            OnPhaseChangeFinished?.Invoke(Current);
        }

        if (ScreenFader.Instance != null)
            ScreenFader.Instance.FadeThrough(AtBlack, Done);
        else
        {
            AtBlack();
            Done();
        }
    }
}
