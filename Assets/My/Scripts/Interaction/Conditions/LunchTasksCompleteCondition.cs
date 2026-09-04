using UnityEngine;

// 접객 테이블 상호작용을 점심 일과(LunchTasks)가 전부 끝났을 때만 허용.
// 미완료 동안엔 아웃라인/프롬프트도 안 뜬다. 접객 테이블 Interactable 에 붙인다. TasksCompleteCondition 과 동일 패턴.
public class LunchTasksCompleteCondition : InteractionCondition
{
    public override bool IsMet =>
        LunchTasks.Instance != null && LunchTasks.Instance.AllDone;
}
