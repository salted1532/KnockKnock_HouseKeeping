using System.Collections.Generic;
using UnityEngine;

// 열쇠 고리/걸이. 손에 든 열쇠(HandItem.IsKey)를 걸어 고정한다. 여러 개 걸 수 있다.
// socket 의 위치/회전에 맞춰 배치만 함 — 부모로 넣지 않음(부모 스케일 영향 안 받게).
// 걸린 열쇠를 다시 떼어가는 동작은 그 열쇠 자신의 Interactable(줍기)+PickupEffect 를 그대로 재사용.
public class HookEffect : InteractionEffect
{
    [Tooltip("걸린 열쇠가 놓일 지점. 비우면 이 오브젝트 자신 사용")]
    [SerializeField] private Transform socket;

    [Tooltip("게임 시작 시 이미 고리에 걸려 있는 열쇠들. 여기에 씬의 열쇠 오브젝트를 넣어두면 시작할 때 socket 에 배치된다")]
    [SerializeField] private GameObject[] initialHungItems;

    [Tooltip("이 고리에 걸 수 있는 최대 열쇠 수. 처음부터 걸린 열쇠(initialHungItems)로 이미 찼으면 플레이어가 더 못 검")]
    [SerializeField] private int capacity = 1;

    [Tooltip("열쇠가 여러 개일 때 socket 기준으로 하나씩 밀어낼 간격 (socket 로컬 축)")]
    [SerializeField] private Vector3 stackOffset = new(0f, 0f, 0.02f);

    private readonly List<GameObject> hung = new();

    private Transform Socket => socket != null ? socket : transform;

    private void Awake()
    {
        if (initialHungItems == null) return;
        foreach (var item in initialHungItems)
            if (item != null) Hang(item, reactivate: false);
    }

    public override void Play(in InteractionContext ctx)
    {
        if (InventorySystem.Instance == null) return;

        var held = InventorySystem.Instance.ActiveHandItem;
        if (held == null || !held.IsKey) return;

        hung.RemoveAll(g => g == null || !g.activeSelf);   // 떼어간 것 정리 후 자리 확인
        if (hung.Count >= Mathf.Max(1, capacity)) return;  // 이미 참 — 겹쳐 걸기 금지

        GameObject item = InventorySystem.Instance.RemoveActiveItem();
        if (item != null) Hang(item, reactivate: true);
    }

    private void Hang(GameObject item, bool reactivate)
    {
        // 컴포넌트(PickupEffect/Rigidbody)가 자식에 있는 열쇠(Key2 등) → 실제 오브젝트로 정규화
        var pe = item.GetComponentInChildren<PickupEffect>();
        if (pe != null) item = pe.gameObject;

        // 재걸기: 이미 목록에 있으면 먼저 제거 (PickupEffect 의 "소리 끝나면 끄기" 코루틴이
        // Reactivate 로 취소되면 activeSelf 가 안 꺼져서 RemoveAll 로는 안 지워짐 → 중복 누적 방지)
        hung.Remove(item);
        hung.RemoveAll(g => g == null || !g.activeSelf);   // 떼어간 열쇠는 목록에서 정리

        item.SetActive(true);
        if (reactivate) PickupEffect.Reactivate(item);

        item.transform.SetPositionAndRotation(
            Socket.position + Socket.rotation * (stackOffset * hung.Count), Socket.rotation);

        var rb = item.GetComponentInChildren<Rigidbody>();   // 루트에 없으면 자식에서
        if (rb != null) rb.isKinematic = true;

        hung.Add(item);
    }
}
