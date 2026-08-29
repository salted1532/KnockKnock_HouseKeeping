using UnityEngine;

// 접객 중 손님을 클릭하면 체크인(승인). 대화가 끝나 ReceptionManager 가 승인 대기 중일 때만 동작.
// (b) 테스트판: 열쇠·방배정 검사 없이 "이 손님 승인" 통지만. 후속 doc 에서 RoomKey 대조 추가.
public class CheckInGuestEffect : InteractionEffect
{
    public override void Play(in InteractionContext ctx)
    {
        var rm = ReceptionManager.Instance;
        if (rm == null || !rm.AwaitingCheckIn)
        {
            Debug.Log("[CheckInGuestEffect] 지금은 체크인 대기 상태가 아님", this);
            return;
        }
        rm.ConfirmCheckIn();
    }
}
