using System.Collections.Generic;
using UnityEngine;

// 상호작용 시 프리팹 생성. (구 ItemDispenser 대체)
// 획득형 아이템이면 생성 프리팹의 PickupEffect.itemId 로 손 오브젝트가 연결되므로 여기서 따로 연결 안 함.
public class SpawnObjectEffect : InteractionEffect
{
    [SerializeField] private GameObject prefab;
    [Tooltip("생성 위치/회전. 비우면 이 오브젝트 transform")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("생성물의 부모. 비우면 씬 최상위 (보통 비움)")]
    [SerializeField] private Transform parent;
    [Tooltip("동시에 존재 가능한 최대 개수 (0 = 무제한). 초과 시 생성 안 함")]
    [SerializeField] private int maxCount = 1;

    private readonly List<GameObject> spawned = new();

    public override void Play(in InteractionContext ctx)
    {
        if (prefab == null) return;

        if (maxCount > 0)
        {
            spawned.RemoveAll(g => g == null);
            if (spawned.Count >= maxCount) return;
        }

        Transform at = spawnPoint != null ? spawnPoint : transform;
        spawned.Add(Instantiate(prefab, at.position, at.rotation, parent));
    }
}
