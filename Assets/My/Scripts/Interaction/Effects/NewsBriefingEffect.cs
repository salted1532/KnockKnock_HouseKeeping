using UnityEngine;

// 새벽 침대 상호작용 → 일차 종료 뉴스 브리핑(NightNewsBriefing) → 아침 전환.
// 새벽→아침 자리의 PhaseSwitchEffect(from=Dawn, to=Morning) 를 대체한다 (doc/0145).
// 브리핑이 없거나 오늘 뉴스 콘텐츠가 없으면 바로 아침으로 전환 — 기존 동작 그대로.
public class NewsBriefingEffect : InteractionEffect
{
    [SerializeField] private NightNewsBriefing briefing;

    public override void Play(in InteractionContext ctx)
    {
        var mgr = DayPhaseManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[NewsBriefingEffect] DayPhaseManager 인스턴스 없음", this);
            return;
        }
        if (mgr.Current != DayPhase.Dawn) return;   // 엉뚱한 타이밍 안전장치 (PhaseCondition 이 이미 게이팅)
        if (NightNewsBriefing.Playing) return;

        if (briefing == null || !briefing.Play())
            mgr.TransitionTo(DayPhase.Morning);   // 브리핑 없음/콘텐츠 없음 → 바로 아침
    }
}
