using UnityEngine;

// 새벽 침대(잠자기) 상호작용을 행동력을 다 썼을 때만 허용. TasksCompleteCondition 과 동일 패턴.
public class ActionPointsDepletedCondition : InteractionCondition
{
    public override bool IsMet =>
        ActionPoints.Instance != null && ActionPoints.Instance.Current <= 0;
}
