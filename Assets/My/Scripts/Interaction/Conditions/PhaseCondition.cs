using UnityEngine;

// 지정한 하루 단계에서만 상호작용 허용. (책상 접객 = Evening 등)
public class PhaseCondition : InteractionCondition
{
    [SerializeField] private DayPhase[] allowedPhases = { DayPhase.Evening };

    public override bool IsMet
    {
        get
        {
            var mgr = DayPhaseManager.Instance;
            if (mgr == null) return true;   // 매니저 없으면 통과
            foreach (var p in allowedPhases)
                if (p == mgr.Current) return true;
            return false;
        }
    }
}
