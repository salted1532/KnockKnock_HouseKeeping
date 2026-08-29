using UnityEngine;

// 손님 상호작용을 "접객 대화가 끝나 체크인 대기 중"일 때만 허용.
// 대화 중엔 아웃라인/프롬프트도 안 뜬다. 손님 프리팹의 Interactable 에 붙인다.
public class AwaitingCheckInCondition : InteractionCondition
{
    public override bool IsMet =>
        ReceptionManager.Instance != null && ReceptionManager.Instance.AwaitingCheckIn;
}
