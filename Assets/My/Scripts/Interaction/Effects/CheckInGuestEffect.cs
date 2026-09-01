using UnityEngine;

// 대화가 끝나 ReceptionManager 가 승인 대기 중일 때 손님 클릭:
//  - 모니터에서 방 배정됨(PendingRoom>0) + 손에 열쇠(HandItem.IsKey) → 열쇠 소모 + 승인 → 손님이 방으로
//  - 손에 열쇠인데 방 미배정 → 넛지 (열쇠 소모 안 함)
//  - 빈손(또는 열쇠 아님) → 대화창 다시 재생
// 그 외 상태에선 무동작. (열쇠↔방번호 대조는 후속 doc)
public class CheckInGuestEffect : InteractionEffect, Interactable.IPromptOverride
{
    public string PromptOverride
    {
        get
        {
            var rm = ReceptionManager.Instance;
            if (rm == null || !rm.AwaitingCheckIn) return null;   // 기본 프롬프트로
            if (!HoldingKey()) return LocalizationManager.T("Talk", "대화");
            return rm.PendingRoom > 0
                ? LocalizationManager.T("Check in", "체크인")
                : LocalizationManager.T("Assign a room first", "방 배정 필요");
        }
    }

    public override void Play(in InteractionContext ctx)
    {
        var rm = ReceptionManager.Instance;
        if (rm == null || !rm.AwaitingCheckIn)
        {
            Debug.Log("[CheckInGuestEffect] 지금은 체크인 대기 상태가 아님", this);
            return;
        }

        if (HoldingKey())
        {
            if (rm.PendingRoom <= 0)
            {
                Debug.Log("[CheckInGuestEffect] 방 미배정 — 모니터에서 방을 먼저 배정", this);
                return;                                               // 열쇠 소모 안 함
            }
            var world = InventorySystem.Instance.RemoveActiveItem();   // 인벤토리에서 제거
            if (world != null) Destroy(world);                         // 손님이 가져감
            rm.ConfirmCheckIn();                                       // 승인 → 방으로
        }
        else
        {
            rm.RequestDialogueReplay();                                // 빈손 → 대화창 다시
        }
    }

    private static bool HoldingKey()
    {
        var held = InventorySystem.Instance != null ? InventorySystem.Instance.ActiveHandItem : null;
        return held != null && held.IsKey;
    }
}
