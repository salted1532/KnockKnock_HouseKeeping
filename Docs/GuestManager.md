# GuestManager

`Assets/My/Scripts/Game/GuestManager.cs`

이번 플레이의 손님 상태 저장소 (싱글턴). 접객에서 판정·방배정·숙박비·하우스키핑 여부를 기록하고,
[`RoomController`](RoomController.md)(객실 생애주기)·[`MonitorRoomBoard`](MonitorRoomBoard.md)(방배정 보드)·
밤 판정 로직이 읽는다.

## `GuestState` — 한 손님의 이번 판 상태

| 필드 | 설명 |
|---|---|
| `npc` (`NpcData`) | 누구 |
| `room` (int, -1) | 배정된 방 번호 (101~110). 승인 시 세팅 |
| `verdict` (`Verdict`) | `None` / `Approved` / `Rejected` / `Killed` |
| `checkInDay` (int) | 체크인한 일차 |
| `stayNights` (int, 1) | 숙박 박수. 체크인 시 `npc.stayNights` 복사 (`doc/0132`) |
| `cleaningRequested` (bool) | 아침 하우스키핑 허용. 체크인 시 `npc.allowsMorningCleaning` — 대화에서 `clean_yes/no` 고르면 덮임 (`doc/0132`·`0135`·`0138`) |
| `nightlyRate` (int) | 1박 요금 ($). 체크인 시 확정 (`Wallet.RoomRate`, 2배 이벤트면 이미 2배) (`doc/0137`) |
| `payUpfront` (bool) | true=선불(체크인 시 입금) / false=후불(체크아웃 아침 입금) |
| `settled` (bool) | 숙박비 입금 완료 (중복 입금 방지) |

계산 프로퍼티:
- `CheckOutDay` = `checkInDay + stayNights` — 이 일차 아침에 방을 나간다.
- `TotalCharge` = `nightlyRate × stayNights`.

## API

| 메소드 | 설명 |
|---|---|
| `CheckIn(npc, room, day)` | `Approved` + `room`/`checkInDay` 세팅 + `stayNights`·`cleaningRequested` 를 `npc` 에서 복사 |
| `SetVerdict(npc, v, day)` | 판정만 기록 (거절/살해) |
| `CheckOut(npc)` | `active` 에서 제거 — `RoomController` 가 체크아웃 아침 지나면 호출 → 방 빈 상태, 모니터 재배정 가능 |
| `Get(npc)` | 그 손님 `GuestState` (없으면 null) |
| `StateInRoom(room)` | 그 방의 `Approved` 손님 `GuestState` — `RoomController` 가 잠금/청소/정산 계산에 씀 |
| `GuestInRoom(room)` / `RoomTaken(room)` | 그 방 배정 손님 `NpcData` / 사용중 여부 — 모니터 보드가 씀 |
| `Active` (`IReadOnlyList<GuestState>`) | 현재 체크인 손님 전부 (읽기 전용) |

## 관련

밤 판정(누가 죽나)은 별도 시스템 — 여기선 `npc.isSleepwalker`(정답) vs `verdict`(플레이어) 괴리만 보관.

[RoomController](RoomController.md) · [ReceptionManager](ReceptionManager.md) · [MonitorRoomBoard](MonitorRoomBoard.md) · [Wallet](Wallet.md) · [DialogueSystem](DialogueSystem.md) · [`doc/0132`](../doc/0132-guest-checkout-and-morning-cleaning.md)
