using UnityEngine;

// 상호작용 시 인벤토리에 추가. (구 Pickup, Flashlight 케이스 대체)
// 획득형 아이템은 보통 프리팹이라 손 오브젝트를 직접 연결 못 함 → ItemId 로 HandItemRegistry 에서 조회.
public class PickupEffect : InteractionEffect
{
    [SerializeField] private Sprite icon;
    [Tooltip("이 아이템 번호. 플레이어 손의 HandItem 과 매칭됨 (손전등=001, 소다=002)")]
    [SerializeField] private ItemId itemId;
    [Tooltip("씬에 직접 배치한 경우의 손 오브젝트 오버라이드 (비우면 itemId 로 조회)")]
    [SerializeField] private GameObject equipTargetOverride;
    [SerializeField] private AudioClip useClip;
    [SerializeField] private bool consumeOnUse;

    public override void Play(in InteractionContext ctx)
    {
        if (InventorySystem.Instance == null) return;

        GameObject target = equipTargetOverride;
        if (target == null && HandItemRegistry.Instance != null)
            target = HandItemRegistry.Instance.Resolve(itemId);

        if (target == null)
        {
            if (itemId != ItemId.None)
                Debug.LogWarning($"[PickupEffect] '{name}' ItemId {itemId} 에 해당하는 손 오브젝트 없음", this);
            Destroy(gameObject);   // 연출용 줍기 (손에 드는 것 없음)
            return;
        }

        bool isFlashlight = target.GetComponentInChildren<Game.PlayerHandItem.Flashlight>(true) != null;
        if (InventorySystem.Instance.AddItem(icon, target, gameObject, isFlashlight, useClip, consumeOnUse))
            gameObject.SetActive(false);
    }
}
