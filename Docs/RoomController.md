# RoomController

`Assets/My/Scripts/Interaction/RoomController.cs`

객실 1개의 관제. 각 방 루트에 컴포넌트로 붙이고 방 번호(101~110)와 가구를 인스펙터로 연결한다.
**새벽(Dawn)** 에 이 방에 배정된 손님이 있으면 정문을 잠그고 노크 상호작용으로 바꾼다. `doc/0118`.

## 필드

| 필드 | 설명 |
|---|---|
| `roomNumber` (int) | 이 방 번호 (101~110). `GuestState.room` 과 대조 |
| `frontDoor` (`Interactable`) | 정문 — `Interactable`(OpenClose) + `HingeEffect` + `SfxEffect`. 새벽에 잠기고 노크로 전환 |
| `knockTarget` (`GameObject`) | 정문의 노크 상호작용 자식 (`Interactable` promptType=Knock + `KnockEffect` + Collider). **시작 비활성** (Awake 가 꺼줌), 잠금 시 활성 |
| `knockAnchor` (`Transform`) | 노크 시 화면고정 위치/정면 ([EnterUIModeEffect](EnterUIModeEffect.md) 의 anchor 와 같은 방식). 정문 자식으로 배치 |
| `guestSpawnPoint` (`Transform`) | 노크 수락 시 지정 손님 스프라이트가 스폰될 위치 (문틈/문 앞) |
| `sealedInteractables` (`Interactable[]`) | 새벽 잠금 시 함께 `enabled=false` 할 이 방 가구 — 내부문·침대·기타. 원하는 만큼 |

## 프로퍼티

| 이름 | 설명 |
|---|---|
| `RoomNumber` | `roomNumber` |
| `KnockAnchor` / `GuestSpawnPoint` | `KnockEffect` 가 읽음 |
| `NightGuest` (`NpcData`) | `GuestManager.GuestInRoom(roomNumber)` — 이 방 배정 손님(Approved). 없으면 null |

## 동작

- `Start` 에서 `DayPhaseManager.OnPhaseChanged` 구독 + 현재 단계 즉시 적용.
- `Apply(phase)`: `seal = (phase == Dawn && NightGuest != null)`
  - `frontDoor`: `seal` 이면 `SetState(false)`(닫기) + `enabled = false`(여닫기 차단). 아니면 `enabled = true`
  - `sealedInteractables`: 전부 `enabled = !seal`
  - `knockTarget.SetActive(seal)`
- `PeekDoor(angle, time)` — `KnockEffect` 가 호출 → `frontDoor` 의 `HingeEffect.SwingTo(angle, time)` (문 살짝 열기/닫기). `Interactable.IsOn` 안 건드림.
- 아침이 되면 `Apply(Morning)` 로 문·가구 전부 원복.

## 배치

각 방: 루트에 `RoomController`. 정문은 자식 오브젝트로 `frontDoor`(Interactable+Hinge+Sfx) + `knockTarget`(비활성) + `knockAnchor`(빈 Transform). 방 앞/문틈에 `guestSpawnPoint`(빈 Transform). 내부문·침대 등은 각자 `Interactable` 두고 `sealedInteractables` 에 등록.

## 관련
[KnockEffect](KnockEffect.md) · [ReceptionManager](ReceptionManager.md) · [HingeEffect](HingeEffect.md) · [DayPhaseManager](DayPhaseManager.md) · [`doc/0118`](../doc/0118-monitor-room-assignment-and-dawn-knock.md)
