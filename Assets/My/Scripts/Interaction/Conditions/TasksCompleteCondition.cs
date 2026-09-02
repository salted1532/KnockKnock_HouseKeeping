using UnityEngine;

// 게시판 상호작용을 아침 할일(MorningTasks)이 전부 끝났을 때만 허용.
// 미완료 동안엔 아웃라인/프롬프트도 안 뜬다. 게시판 Interactable 에 붙인다. doc/0144.
public class TasksCompleteCondition : InteractionCondition
{
    public override bool IsMet =>
        MorningTasks.Instance != null && MorningTasks.Instance.AllDone;
}
