using UnityEngine;

// 빈 고리에, 지금 손에 든 열쇠(HandItem.IsKey)를 걸어 고정한다. (Key_hook 등)
// socket 의 위치/회전에 맞춰 배치만 함 — 부모로 넣지 않음(부모 스케일 영향 안 받게).
// 다시 떼어가는 동작은 걸린 오브젝트 자신의 Interactable(줍기)+PickupEffect 를 그대로 재사용.
public class HookEffect : InteractionEffect
{
    [Tooltip("걸린 아이템이 위치할 지점. 비우면 이 오브젝트 자신 사용")]
    [SerializeField] private Transform socket;
    [Tooltip("씬에 미리 걸어둔 아이템(선택) — 시작부터 걸려있는 상태를 표현할 때 지정")]
    [SerializeField] private GameObject initialHungItem;

    private GameObject hungItem;

    private Transform Socket => socket != null ? socket : transform;
    private bool IsOccupied => hungItem != null && hungItem.activeSelf;

    private void Awake()
    {
        if (initialHungItem != null && initialHungItem.activeSelf)
            hungItem = initialHungItem;
    }

    public override void Play(in InteractionContext ctx)
    {
        if (IsOccupied || InventorySystem.Instance == null) return;

        var held = InventorySystem.Instance.ActiveHandItem;
        if (held == null || !held.IsKey) return;

        GameObject item = InventorySystem.Instance.RemoveActiveItem();
        if (item == null) return;

        item.transform.SetPositionAndRotation(Socket.position, Socket.rotation);
        item.SetActive(true);

        var rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        hungItem = item;
    }
}
