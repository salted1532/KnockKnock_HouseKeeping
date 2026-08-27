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
- `target` 이 없으면 그냥 `Destroy(gameObject)` (연출용 줍기). `itemId` 지정됐는데 못 찾으면 경고 로그.
- `InventorySystem.Instance.AddItem(icon, target, gameObject, isFlashlight, useClip, consumeOnUse)` 성공 시 이 오브젝트 `SetActive(false)`.

## 관련
[Interactable](Interactable.md) · [HandItemRegistry](HandItemRegistry.md) · [InventorySystem](InventorySystem.md) · [`doc/0078`](../doc/0078-interaction-system-redesign.md)
