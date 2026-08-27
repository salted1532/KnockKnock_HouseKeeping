# SpawnObjectEffect

`Assets/My/Scripts/Interaction/Effects/SpawnObjectEffect.cs`

상호작용 시 프리팹 생성. 구 `ItemDispenser` 대체 (자판기 등).

## 필드

| 필드 | 설명 |
|---|---|
| `prefab` (`GameObject`) | 생성할 프리팹 |
| `spawnPoint` (`Transform`) | 생성 위치/회전. 비우면 이 오브젝트 transform |
| `parent` (`Transform`) | 생성물이 자식으로 들어갈 Transform. 비우면 씬 최상위 (보통 비움) |
| `maxCount` (int, 기본 1) | 동시 존재 최대 개수. 0이면 무제한. 초과 시 생성 안 함 |

## 동작

- `prefab` null 이면 무시.
- `maxCount > 0` 이면 이미 생성한 목록에서 null(파괴됨) 제거 후, `count >= maxCount` 면 생성 안 함.
- `Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, parent)` 후 목록에 추가.

## 획득형 아이템 연결

생성되는 프리팹에 `PickupEffect(itemId=...)` 만 있으면 줍는 순간 `HandItemRegistry` 로 손 오브젝트가 자동 연결됨 — 이 효과에서 따로 안 함.

## 관련
[Interactable](Interactable.md) · [PickupEffect](PickupEffect.md)
