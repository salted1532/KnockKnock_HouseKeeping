# 0093 - 줍기 소리, 순서 맞춰도 여전히 안 남 (즉시 SetActive(false) 때문)

## 요청
> 그래도 소리 안나는데 결국엔 소리 나는 중이라도 gameObject.SetActive(false)로 꺼지면 소리도 멈추는거 아니야?

맞음. doc/0092에서 `SfxEffect`를 먼저 실행하도록 순서를 고쳤지만, 그 바로 다음 줄(같은 프레임, 같은 호출 스택)에서 `PickupEffect.Play()`가 `gameObject.SetActive(false)`를 호출함 — `OnDisable`이 즉시 그 오브젝트의 `AudioSource` 재생을 끊어버려서, 사실상 소리가 **한 샘플도 못 내고** 멈춤. 순서 문제가 아니라 "재생 시작하자마자 오브젝트를 꺼버린다"가 진짜 원인.

## 조사
`PickupEffect.Play()`:
```csharp
if (InventorySystem.Instance.AddItem(...))
    gameObject.SetActive(false);
```
`AudioSource`도 같은 `gameObject`에 있으므로(`SfxEffect`가 `[RequireComponent(AudioSource)]`), 이 한 줄이 그 소스도 같이 끔.

## 계획
`PickupEffect`가 오브젝트를 **완전히 끄기 전에**, 재생 중인 소리가 있으면 끝날 때까지 기다린다. 그동안 안 보이고 다시 상호작용도 안 되게 렌더러/콜라이더만 먼저 꺼둠(월드에서 사라진 것처럼 보이지만, `AudioSource`가 붙은 오브젝트 자체는 아직 살아있어서 소리가 끝까지 남).

```csharp
using System.Collections;
using UnityEngine;

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
}
```

## 리스크
- 낮음. 다운스트림(`InventorySystem`, `HookEffect`)은 `pickupSource`를 참조만 들고 있다가 나중에(던지기/고리 걸기) 다시 켤 때만 `SetActive(true)` 하므로, 최종 `SetActive(false)`가 몇백ms 늦어져도 문제없음.
- 아주 미세한 예외: 고리에 걸린 열쇠를 다시 주울 때, 소리가 끝날 때까지(보통 1초 미만) `HookEffect.IsOccupied`가 계속 "점유됨"으로 남음 — 그 사이에 같은 고리에 다른 열쇠를 걸려고 하면 잠깐 막힘. 사실상 감지되기 힘든 타이밍이라 무시함.
- `SfxEffect` 자체나 다른 프롬프트 타입(여닫기/밀기 등)은 안 건드림 — 자기 자신을 비활성화하는 효과가 `PickupEffect` 뿐이라 여기만 고치면 됨.

## 결과 (2026-08-28, 승인 후 적용)
계획대로 `PickupEffect.cs`를 `Hide()`/`FinishAfterSound()`/`Finish()` 구조로 교체. `Destroy(gameObject)` 즉시 호출 경로, `SetActive(false)` 즉시 호출 경로 둘 다 `Hide()`를 거치도록 통일. `Docs/PickupEffect.md` 갱신(관련 링크에 `SfxEffect`, `doc/0093` 추가).

## 검증
- 정적 확인만 완료. Unity Play 모드에서 Key1 등 줍기 시 소리가 끝까지 나는지, 줍는 즉시 화면에서 안 보이고 다시 상호작용 안 되는지 확인 필요.

## 상태
2026-08-28 완료.
