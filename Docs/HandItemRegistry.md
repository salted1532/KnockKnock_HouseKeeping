# 아이템 ID 연결 (ItemId / HandItem / HandItemRegistry)

`Assets/My/Scripts/Inventory/ItemId.cs`, `HandItem.cs`, `HandItemRegistry.cs`

바닥에서 줍는 아이템은 프리팹이라 손 오브젝트(씬)를 직접 참조 못 함 → **번호로 연결**.

## ItemId (enum)

```csharp
public enum ItemId { None = 0, Flashlight = 1, Soda = 2 }
```
새 아이템은 여기에 번호와 함께 추가.

## HandItem

플레이어 손에 드는 오브젝트마다 붙인다.

| 필드 | 설명 |
|---|---|
| `id` (`ItemId`) | 어떤 아이템인지 |
| `isKey` (bool) | 열쇠 종류인가 — [HookEffect](HookEffect.md)가 고리에 걸 수 있는지 판정에 씀 |

프로퍼티 `Id`, `IsKey` 노출.

## HandItemRegistry

플레이어 손 루트(**항상 활성**인 부모)에 1개. 싱글턴.

- `Awake` : 자식의 `HandItem` 들(비활성 포함)을 `id` 로 색인. `id == None` 은 건너뜀. 중복 id 는 경고 로그.
- `Resolve(ItemId id) → GameObject` : 해당 손 오브젝트. 없으면 null.

## 세팅

손전등 손 오브젝트에 `HandItem(id=Flashlight)`, 소다 손 오브젝트에 `HandItem(id=Soda)`, 그 공통 부모에 `HandItemRegistry`.
줍는 프리팹의 [`PickupEffect.itemId`](PickupEffect.md) 를 맞춰줌.

## 관련
[PickupEffect](PickupEffect.md) · [InventorySystem](InventorySystem.md)
