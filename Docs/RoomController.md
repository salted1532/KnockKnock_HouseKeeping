# RoomController

`Assets/My/Scripts/Interaction/RoomController.cs`

객실 1개의 관제. 각 방 루트에 컴포넌트로 붙이고 방 번호(101~110)와 가구를 인스펙터로 연결한다.
배정 손님이 있으면 **체크인한 저녁을 빼고 체크아웃까지 정문을 잠그고** 노크 상호작용으로 바꾼다.
**아침 청소 창**(체크아웃 아침, 또는 청소 허용 손님의 숙박 중 아침)에는 정문을 열고 침대 등을
흐트러뜨려 플레이어가 청소하게 한다. `doc/0118` · `doc/0132`.

## 필드

| 필드 | 설명 |
|---|---|
| `roomNumber` (int) | 이 방 번호 (101~110). `GuestState.room` 과 대조 |
| `frontDoor` (`Interactable`) | 정문 — `Interactable`(OpenClose) + `HingeEffect` + `SfxEffect`. 새벽에 잠기고 노크로 전환 |
| `knockTarget` (`GameObject`) | 정문의 노크 상호작용 자식 (`Interactable` promptType=Knock + `KnockEffect` + Collider). **시작 비활성** (Awake 가 꺼줌), 잠금 시 활성 |
| `knockAnchor` (`Transform`) | 노크 시 화면고정 위치/정면 ([EnterUIModeEffect](EnterUIModeEffect.md) 의 anchor 와 같은 방식). 정문 자식으로 배치 |
| `guestSpawnPoint` (`Transform`) | 노크 수락 시 지정 손님 스프라이트가 스폰될 위치 (문틈/문 앞) |
| `sealedInteractables` (`Interactable[]`) | 잠금 시 함께 `enabled=false` 할 이 방 가구 — 내부문·침대·기타. 원하는 만큼 |
| `messyObjects` (`GameObject[]`) | 아침 청소 창이 열릴 때 **활성화** — 침대의 흐트러진 버전 등 (`Bed` 의 `ChangeObjectEffect` offObjects 와 같은 것) |
| `tidyObjects` (`GameObject[]`) | 아침 청소 창이 열릴 때 **비활성화** — 정리된 버전 (onObjects). 플레이어가 `Bed` CleanUp 하면 다시 켜짐 |
| `curtains` (`Interactable[]`) | 잠금 시 `SetState(false)`(커튼 OFF) + `enabled=false`. 창밖에서 방 안 안 보이게. 무음 (doc/0136 · 0141) |
| `lights` (`Interactable[]`) | 잠금 시 `SetState(false)`(전등 끄기) + `enabled=false`. 무음 (doc/0136 · 0141) |

## 프로퍼티

| 이름 | 설명 |
|---|---|
| `RoomNumber` | `roomNumber` |
| `KnockAnchor` / `GuestSpawnPoint` | `KnockEffect` 가 읽음 |
| `NightGuest` (`NpcData`) | `GuestManager.GuestInRoom(roomNumber)` — 이 방 배정 손님(Approved). 없으면 null |

## 동작

- `Start` 에서 `DayPhaseManager.OnPhaseChanged` 구독 + 현재 단계 즉시 적용.
- `Apply(phase)` (`doc/0132`):
  - `g = GuestManager.StateInRoom(roomNumber)`. `day >= g.CheckOutDay` 이고 체크아웃 아침이 아니면
    `GuestManager.CheckOut(g.npc)` 로 정산 (방 비움 → 모니터 재배정 가능).
  - **후불 정산** (`doc/0137`): `체크아웃 아침` && `!g.payUpfront` && `!g.settled` && `g.nightlyRate > 0`
    → `Wallet.Add(g.TotalCharge)` + `settled=true` (현금음 자동, `settled` 로 1회). 선불 손님은 체크인 시 이미 지불. 자세히는 [Wallet](Wallet.md).
  - `seal` = 손님 있음 && **체크인한 저녁 아님** && **아침 청소 창 아님**
    - 아침 청소 창 = `체크아웃 아침` 또는 `청소 허용 손님(cleaningRequested)의 숙박 중 아침`
  - `frontDoor`: `seal` 이면 `SetState(false)` + `enabled=false`. 아니면 `enabled=true`
  - `sealedInteractables`: 전부 `enabled = !seal`
  - `curtains`: `seal` 이면 `SetState(false)`(OFF), `enabled = !seal` / `lights`: `seal` 이면 `SetState(false)`(끄기), `enabled = !seal` — 방 안 안 보이게 (doc/0136 · 0141)
  - 문·커튼·조명 `SetState` 는 전부 `silent: true` — 봉인은 자동 연출이라 효과음 없음 (doc/0141)
  - `knockTarget.SetActive(seal)` — 잠겼으면 노크 노출. 새벽이 아니면 [`KnockEffect`](KnockEffect.md) 가 항상 거절
  - 아침 청소 창이면 `SetMessy()` — `messyObjects` 켜고 `tidyObjects` 끔
- `PeekDoor(angle, time)` — `KnockEffect` 가 호출 → `frontDoor` 의 `HingeEffect.SwingTo(angle, time)`.
- 손님이 없는 방(체크아웃 후·미배정)은 전 단계 정상.

## 배치

각 방: 루트에 `RoomController`. 정문은 자식 오브젝트로 `frontDoor`(Interactable+Hinge+Sfx) + `knockTarget`(비활성) + `knockAnchor`(빈 Transform). 방 앞/문틈에 `guestSpawnPoint`(빈 Transform). 내부문·침대 등은 각자 `Interactable` 두고 `sealedInteractables` 에 등록.

## 관련
[KnockEffect](KnockEffect.md) · [ReceptionManager](ReceptionManager.md) · [HingeEffect](HingeEffect.md) · [DayPhaseManager](DayPhaseManager.md) · [`doc/0118`](../doc/0118-monitor-room-assignment-and-dawn-knock.md)
