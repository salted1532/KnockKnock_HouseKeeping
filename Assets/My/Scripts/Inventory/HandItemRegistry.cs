using System.Collections.Generic;
using UnityEngine;

// 플레이어 손 루트(항상 활성인 부모)에 붙인다. 자식의 HandItem 들을 ItemId 로 색인.
// PickupEffect 가 ItemId → 손 오브젝트 를 여기서 조회한다.
public class HandItemRegistry : MonoBehaviour
{
    public static HandItemRegistry Instance { get; private set; }

    private readonly Dictionary<ItemId, GameObject> map = new();

    private void Awake()
    {
        Instance = this;
        map.Clear();
        foreach (var hi in GetComponentsInChildren<HandItem>(true))
        {
            if (hi.Id == ItemId.None) continue;
            if (map.ContainsKey(hi.Id))
                Debug.LogWarning($"[HandItemRegistry] ItemId {hi.Id} 중복 — '{hi.name}'", hi);
            map[hi.Id] = hi.gameObject;
        }
    }

    // 해당 ItemId 의 손 오브젝트. 없으면 null.
    public GameObject Resolve(ItemId id)
        => id != ItemId.None && map.TryGetValue(id, out var go) ? go : null;
}
