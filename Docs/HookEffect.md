# HookEffect

`Assets/My/Scripts/Interaction/Effects/HookEffect.cs`

열쇠 고리/걸이. **지금 손에 든 게 열쇠 종류(`HandItem.IsKey`)면** 그걸 걸어 고정한다 — 고리마다 특정 아이템을 지정하지 않음. **여러 개 걸 수 있다.** [Interactable](InteractionSystem.md)의 `걸기` 프롬프트 표준 효과(우클릭 "재설정"으로 자동 추가/제거되는 managed 효과).

## 필드

| 필드 | 설명 |
|---|---|
| `socket` (`Transform`) | 걸린 열쇠가 놓일 지점. 비우면 이 오브젝트 자신 사용 |
| `initialHungItems` (`GameObject[]`) | 게임 시작 시 이미 고리에 걸려 있는 열쇠들 — 씬의 열쇠 오브젝트를 넣어두면 `Awake` 에서 `socket` 에 배치 |
| `capacity` (int, 기본 1) | 걸 수 있는 최대 열쇠 수. 이미 차 있으면 플레이어가 더 못 검 (겹쳐 걸기 방지). `initialHungItems` 로 이미 1개 걸렸으면 기본값 1로 꽉 참. 스택하려면 2+. `Awake` 의 초기 배치는 이 제한 무시 |
| `stackOffset` (`Vector3`) | 열쇠가 여러 개일 때 `socket` 기준으로 하나씩 밀어낼 간격(socket 로컬 축). 기본 `(0, 0, 0.02)` |

## 동작

- 걸린 열쇠는 내부 리스트(`hung`)로 관리. `Hang` 진입 시 `hung.Remove(item)`(재걸기 중복 방지) + `RemoveAll(null || !activeSelf)`(떼어간 열쇠 정리).
- **자식에 `PickupEffect`/`Rigidbody` 가 있는 열쇠**(예: `Key2`→`Simple_03`): `Hang` 이 `GetComponentInChildren` 으로 실제 오브젝트를 찾아 배치·kinematic 처리 → `initialHungItems` 에 부모를 넣어도 동작 (`doc/0123`).
- `Awake`에서 `initialHungItems` 의 각 항목을 `socket`(+ `stackOffset × 순번`)에 배치 + `SetActive(true)`.
- `Play`: `InventorySystem.Instance.ActiveHandItem` 확인 — 없거나 `IsKey`가 아니면 무시. `hung` 정리 후 `Count >= capacity`(최소 1)면 무시 (겹쳐 걸기 방지).
- 자리 있으면 `InventorySystem.Instance.RemoveActiveItem()`으로 활성 슬롯을 비우고 원래 월드 오브젝트(`item`)를 받아옴.
- `item.transform.SetPositionAndRotation(Socket.position + Socket.rotation × (stackOffset × 걸린수), Socket.rotation)` — **부모는 바꾸지 않고** `Socket`의 월드 위치/회전만 복사, `SetActive(true)`. (부모로 넣으면 `Key_hook`처럼 스케일이 찌그러진 부모 밑에서 아이템도 같이 찌그러지는 문제가 있었음.)
- `item` 에 `Rigidbody` 가 있으면 `isKinematic = true` (고리에 걸린 동안 물리 영향 안 받음).
- **다시 떼어가는 동작은 따로 구현 안 함** — 걸린 `item` 은 원래 자기 `Interactable(줍기)+PickupEffect` 를 그대로 갖고 있어서, 다시 보고 상호작용하면 그 픽업 로직이 그대로 실행돼 인벤토리로 들어가고 `SetActive(false)` 됨 (= 고리가 다시 빈 상태로 인식됨).

## 세팅 시 주의

- 미리 걸려있는 상태로 배치하려면: 걸어둘 열쇠 오브젝트(각자 `Interactable` 줍기 + `PickupEffect` 보유, `HandItem.IsKey` 권장)를 `HookEffect`의 `initialHungItems` 배열에 넣기. 위치/회전은 `Awake`가 `socket` 기준으로 잡아주므로 씬에서 미리 안 맞춰도 됨.
- 플레이어가 열쇠를 도로 집으면 각 열쇠 자신의 `PickupEffect`가 실행 → 인벤토리로 들어가고 `SetActive(false)`.

## 관련
[Interactable](InteractionSystem.md) · [PickupEffect](PickupEffect.md) · [InventorySystem](InventorySystem.md) · [`doc/0087`](../doc/0087-key-hook-effect.md) · [`doc/0090`](../doc/0090-hookeffect-no-parenting.md)
