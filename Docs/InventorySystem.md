# InventorySystem

`Assets/My/Scripts/Inventory/InventorySystem.cs`

5슬롯 인벤토리 싱글턴. 아이템 줍기/장착/사용/버리기, 손전등 슬롯 특수 처리.

## 인스펙터

| 필드 | 설명 |
|---|---|
| `slotIcons` (`Image[5]`) | 슬롯 아이콘 UI |
| `activateIcons` (`GameObject[5]`) | 활성 슬롯 표시 |
| `throwPos` (`Transform`) | 던지기 기준 위치/방향 |
| `throwForce` (기본 10) | 던지는 힘 |
| `audioSource` | 아이템 사용음 재생 |

## 입력 (`Update`)

- `1`~`5` : 슬롯 선택
- `F` : 활성 아이템 던지기 (`ThrowActiveItem`) — 원본 픽업 오브젝트를 되살려 `Rigidbody` 붙여 던짐, 플레이어 콜라이더와 충돌 무시
- 좌클릭 : 활성 아이템 사용 (`useClip` 재생, `consumeOnUse` 면 소모)
- 마우스 휠 : 슬롯 순환 (손전등 켜져 있으면 잠금)

## API

- `AddItem(Sprite icon, GameObject equipTarget, GameObject pickupSource, bool isFlashlight = false, AudioClip useClip = null, bool consumeOnUse = false)` → 빈 슬롯에 등록, 성공 시 true. [PickupEffect](PickupEffect.md) 가 호출.
- `ActiveHandItem` (프로퍼티) → 현재 활성 슬롯(손에 든 것)의 `HandItem`. 없으면 null.
- `RemoveActiveItem()` → 활성 슬롯을 비우고 원래 월드 오브젝트를 반환. 빈 손이면 null. [HookEffect](HookEffect.md) 가 호출.
- 손전등 슬롯: `equipTarget` 의 자식 `Flashlight` 참조를 잡아 `IsOpen` 으로 휠 잠금 판정.

## 관련
[PickupEffect](PickupEffect.md) · [HandItemRegistry](HandItemRegistry.md) · [HookEffect](HookEffect.md)
