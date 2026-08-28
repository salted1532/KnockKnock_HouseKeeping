# PickupEffect

`Assets/My/Scripts/Interaction/Effects/PickupEffect.cs`

상호작용 시 인벤토리에 추가. 구 `Pickup` + `Flashlight` 케이스 대체.
획득형 아이템은 보통 프리팹이라 손 오브젝트(씬)를 직접 못 참조 → `ItemId` 로 [HandItemRegistry](HandItemRegistry.md) 에서 조회.

## 필드

| 필드 | 설명 |
|---|---|
| `icon` (`Sprite`) | 슬롯 아이콘 |
| `itemId` (`ItemId`) | 아이템 번호. 플레이어 손의 `HandItem` 과 매칭 (손전등=001, 소다=002) |
| `equipTargetOverride` (`GameObject`) | 씬에 직접 배치한 경우의 손 오브젝트 오버라이드 (비우면 `itemId` 로 조회) |
| `useClip` (`AudioClip`) | 좌클릭 사용 시 재생 소리 (InventorySystem 이 재생) |
| `consumeOnUse` (bool) | 사용 시 소모(1회용)인가 |

## 동작

- `target` = `equipTargetOverride` 있으면 그것, 없으면 `HandItemRegistry.Instance.Resolve(itemId)`.
- 손전등 여부 = `target` 에 `Flashlight` 컴포넌트(자식 포함) 있으면 자동 인식.
- `target` 이 없으면 `Hide(destroy: true)` (연출용 줍기). `itemId` 지정됐는데 못 찾으면 경고 로그.
- `InventorySystem.Instance.AddItem(icon, target, gameObject, isFlashlight, useClip, consumeOnUse)` 성공 시 `Hide(destroy: false)`.
- `Hide()`: 렌더러/콜라이더만 먼저 꺼서 즉시 안 보이고 안 부딪히게 함 — **바로 `SetActive(false)` 안 함**. 같은 오브젝트의 `AudioSource`(줍기 효과음, [SfxEffect](SfxEffect.md))가 재생 중이면 끝날 때까지 기다렸다가 그제서야 `SetActive(false)`(또는 `Destroy`). 즉시 꺼버리면 재생 중이던 소리도 같이 끊기기 때문([`doc/0093`](../doc/0093-pickup-sfx-survive-deactivate.md)). `Rigidbody`가 있으면 `isKinematic = true`도 같이 세팅 — 콜라이더가 꺼진 채로 숨어있는 동안 중력만 받아 바닥을 뚫고 떨어지는 것을 막음([`doc/0096`](../doc/0096-pickup-hidden-falls-through-floor.md)).
- `Reactivate(GameObject go)` (static): `Hide()`로 꺼둔 렌더러/콜라이더를 되살림. `Renderer.enabled`/`Collider.enabled`는 `SetActive`와 별개 상태라서, 이 아이템을 다시 세상에 내놓는 쪽(`InventorySystem.ThrowActiveItem`, `HookEffect`)이 `SetActive(true)` 다음에 반드시 같이 호출해야 함([`doc/0094`](../doc/0094-pickup-reactivate-renderer-collider.md)). 또한 `PickupEffect`에 남아있는 코루틴(줍기 소리가 아직 재생 중이어서 대기 중인 `FinishAfterSound`)도 `StopAllCoroutines()`로 같이 취소 — 안 그러면 소리가 끝나는 시점에 이미 던져지거나 걸린 오브젝트를 또 `SetActive(false)` 해버림([`doc/0095`](../doc/0095-pickup-stale-coroutine-on-throw.md)).

## 관련
[Interactable](Interactable.md) · [HandItemRegistry](HandItemRegistry.md) · [InventorySystem](InventorySystem.md) · [HookEffect](HookEffect.md) · [SfxEffect](SfxEffect.md) · [`doc/0078`](../doc/0078-interaction-system-redesign.md)
