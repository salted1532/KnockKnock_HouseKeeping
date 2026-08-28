# HookEffect

`Assets/My/Scripts/Interaction/Effects/HookEffect.cs`

빈 고리에, **지금 손에 든 게 열쇠 종류(`HandItem.IsKey`)면** 그걸 걸어 고정한다 — 고리마다 특정 아이템을 지정하지 않음. [Interactable](InteractionSystem.md)의 `걸기` 프롬프트 표준 효과(우클릭 "재설정"으로 자동 추가/제거되는 managed 효과).

## 필드

| 필드 | 설명 |
|---|---|
| `socket` (`Transform`) | 걸린 아이템이 위치할 지점. 비우면 이 오브젝트 자신 사용 |
| `initialHungItem` (`GameObject`) | 씬에 미리 걸어둔 아이템(선택) — 시작부터 걸려있는 상태를 표현할 때 지정 |

## 동작

- 점유 여부는 **직접 참조**(`hungItem` 필드)로 판정 — `hungItem != null && hungItem.activeSelf`. 걸린 아이템이 자기 `PickupEffect`로 다시 집히면(`SetActive(false)`) 자동으로 "빈 고리"가 됨.
- `Awake`에서 `initialHungItem`이 지정+활성 상태면 `hungItem`으로 삼음(처음부터 걸려있는 상태 지원).
- 비어있으면 `InventorySystem.Instance.ActiveHandItem`(현재 활성 슬롯의 `HandItem`)을 확인 — 없거나 `IsKey`가 아니면 무시.
- 열쇠면 `InventorySystem.Instance.RemoveActiveItem()`으로 활성 슬롯을 비우고 원래 월드 오브젝트(`item`)를 받아옴.
- `item.transform.SetPositionAndRotation(Socket.position, Socket.rotation)` — **부모는 바꾸지 않고** `Socket`의 월드 위치/회전만 그대로 복사, `SetActive(true)`. (부모로 넣으면 `Key_hook`처럼 스케일이 찌그러진 부모 밑에서 아이템도 같이 찌그러지는 문제가 있었음 — 그래서 위치/회전만 맞추는 방식으로 변경.)
- `item` 에 `Rigidbody` 가 있으면 `isKinematic = true` (고리에 걸린 동안 물리 영향 안 받음).
- **다시 떼어가는 동작은 따로 구현 안 함** — 걸린 `item` 은 원래 자기 `Interactable(줍기)+PickupEffect` 를 그대로 갖고 있어서, 다시 보고 상호작용하면 그 픽업 로직이 그대로 실행돼 인벤토리로 들어가고 `SetActive(false)` 됨 (= 고리가 다시 빈 상태로 인식됨).

## 세팅 시 주의

- 미리 걸려있는 상태로 배치하려면: 아이템을 `Socket`의 월드 위치/회전에 맞춰 놓고(부모는 그대로 둠), `Rigidbody.Is Kinematic` 체크, 이 `HookEffect`의 `initialHungItem`에 그 아이템 연결.

## 관련
[Interactable](InteractionSystem.md) · [PickupEffect](PickupEffect.md) · [InventorySystem](InventorySystem.md) · [`doc/0087`](../doc/0087-key-hook-effect.md) · [`doc/0090`](../doc/0090-hookeffect-no-parenting.md)
