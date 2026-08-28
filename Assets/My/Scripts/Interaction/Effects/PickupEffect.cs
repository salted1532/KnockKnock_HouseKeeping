using System.Collections;
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
            Hide(destroy: true);   // 연출용 줍기 (손에 드는 것 없음)
            return;
        }

        bool isFlashlight = target.GetComponentInChildren<Game.PlayerHandItem.Flashlight>(true) != null;
        if (InventorySystem.Instance.AddItem(icon, target, gameObject, isFlashlight, useClip, consumeOnUse))
            Hide(destroy: false);
    }

    // 즉시 안 보이고/안 부딪히게만 하고, 재생 중인 소리(SfxEffect의 AudioSource)가 있으면 끝날 때까지
    // 기다렸다 최종적으로 SetActive(false)(또는 Destroy) — 그래야 소리가 안 끊김.
    private void Hide(bool destroy)
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;   // 숨어있는 동안 낙하 방지(콜라이더가 꺼져 바닥을 뚫고 떨어지는 것 막음)

        var src = GetComponent<AudioSource>();
        if (src != null && src.isPlaying)
            StartCoroutine(FinishAfterSound(src, destroy));
        else
            Finish(destroy);
    }

    private IEnumerator FinishAfterSound(AudioSource src, bool destroy)
    {
        yield return new WaitWhile(() => src != null && src.isPlaying);
        Finish(destroy);
    }

    private void Finish(bool destroy)
    {
        if (destroy) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    // 이 아이템을 다시 세상에 내놓을 때(던지기/고리 걸기 등) 호출 — Hide() 로 꺼둔 렌더러/콜라이더를 되살림.
    public static void Reactivate(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = true;

        var pe = go.GetComponent<PickupEffect>();
        if (pe != null) pe.StopAllCoroutines();   // 이전 줍기의 "소리 끝나면 끄기" 예약(FinishAfterSound) 취소
    }
}
