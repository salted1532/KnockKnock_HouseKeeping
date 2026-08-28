using UnityEngine;

// 상호작용 시 하루 단계를 from → to 로 넘긴다. 한 방향, 명시적.
// 현재 단계가 from 이 아니면 아무것도 안 한다 (엉뚱한 타이밍에 눌려도 안전).
// promptType(아침종료/점심종료/저녁종료/하루종료) "효과 재설정" 시 from/to 가 자동 설정된다.
public class PhaseSwitchEffect : InteractionEffect
{
    [Tooltip("이 단계일 때만 전환한다")]
    [SerializeField] private DayPhase from = DayPhase.Morning;
    [Tooltip("전환 목표 단계")]
    [SerializeField] private DayPhase to = DayPhase.Noon;

    public override void Play(in InteractionContext ctx)
    {
        var mgr = DayPhaseManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[PhaseSwitchEffect] DayPhaseManager 인스턴스 없음", this);
            return;
        }
        if (mgr.Current != from)
        {
            Debug.LogWarning($"[PhaseSwitchEffect] '{name}': 현재 {mgr.Current} 인데 from={from} — 전환 안 함", this);
            return;
        }
        mgr.TransitionTo(to);
    }
}
