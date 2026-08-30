# ReceptionManager

`Assets/My/Scripts/Game/ReceptionManager.cs`

저녁 "접객" 파트 관리자. 싱글턴. 오늘 일차 편성(`CampaignData`)이 있으면 **손님 큐**(활성 → 걸어옴 → 대화 → 거절이면 퇴장 / 아니면 손님 클릭 → 입실)를 돌린다. 상세는 [DialogueSystem](DialogueSystem.md).

## 필드

| 필드 | 설명 |
|---|---|
| `receptionAnchor` (`Transform`) | 접객 시 플레이어가 앉을 위치/정면 — 접객 테이블의 `Player_Anchor` |
| `campaign` (`CampaignData`) | 캠페인 편성 (일차 리스트). `Day(DayCount)` 로 오늘 것 조회 |
| `catalog` (`NpcCatalog`) | 손님 번호 → `NpcData` |
| `guestPrefab` (`GameObject`) | 접객 NPC 프리팹 (`GuestMover` + `GuestView` + 자식 `SpeechBubble` + `Interactable`/`CheckInGuestEffect`). 세션당 1개 인스턴스를 손님마다 재활용 |
| `guestSpawn` (`Transform`) | 스폰/리셋 위치 |
| `entryPath` / `exitPath` / `roomPath` (`Transform[]`) | 스폰→카운터 / 카운터→밖 / 카운터→방 (씬 트랜스폼) |
| `entryFacing` / `exitFacing` / `roomFacing` (`GuestView.Facing[]`) | 레그별 손님 방향 (waypoint[i] 로 걷는 동안. 짧으면 Auto). `doc/0116` |
| `guestDoor` (`Interactable`) | 맵 출입문. 손님이 `entryDoorElement`(1) / `exitDoorElement`(0) / `roomDoorElement`(0) **웨이포인트에 도착**하면 닫혀 있을 때만 `SetState(true)` 로 연다. **자동으로 닫지 않음** — 열고 그냥 감. 입장은 `entryPath[1]`(Pos2) 도착 시(스폰 아님), 퇴장·입실은 시작점(카운터) 즉시. `doc/0117` |
| `firstRoomNumber` (int, 기본 101) | 승인 시 자동 증가하는 객실번호 |
| `enterDelay` (float, 기본 0.6) | 착석/페이드 후 첫 손님까지 |
| `debugEndKey` (bool, 기본 on) | 디버그: 세션 중 `K` 로 즉시 종료(큐 중단 → 새벽) |

## 프로퍼티 / 이벤트

| 이름 | 설명 |
|---|---|
| `Instance` (static) | 싱글턴 |
| `InSession` (bool) | 접객 세션 진행 중 (일시정지 중에도 true) |
| `Paused` (bool) | ESC 로 UI 모드를 빠져나온 상태 = 일시정지. 손님 이동·대화가 멈추고 대화패널 숨김. 세션은 유지 |
| `AwaitingCheckIn` (bool) | 대화 끝, 손님 클릭 대기 중 (`CheckInGuestEffect`·`AwaitingCheckInCondition` 이 확인). 재대화 중엔 다시 false |
| `ConfirmCheckIn()` | `CheckInGuestEffect` 가 **열쇠 든 채** 손님 클릭 시 호출 → 현재 손님 승인(입실) |
| `RequestDialogueReplay()` | `CheckInGuestEffect` 가 **빈손** 손님 클릭 시 호출 → 대화 다시 재생 |
| `OnSessionStarted` / `OnSessionEnded` (`event Action`) | 손님 큐/신분증 심사/정산을 이 위에 얹음 |

## 동작

- `DayPhaseManager.OnPhaseChanged` 구독 → `Evening` 되면 `BeginSession()`:
  `UIInteractionMode.Enter(receptionAnchor)` + `InSession=true` + `OnSessionStarted` + (오늘 편성 있으면) 손님 큐 코루틴.
- **손님 큐** (`GuestQueue`): `guestPrefab` 1개 `Instantiate`(재활용). `campaign.Day(DayCount).eveningGuestIds` 순회 → `catalog.Get(id)` → `GuestView.Apply(npc)` → `GuestMover.WarpTo(guestSpawn)` → `WalkThrough(entryPath)` → `DialogueRunner.Play(npc, bubble, Reception, onResult)` →
  - `visitorOnly` → `WalkThrough(exitPath)` (대화만)
  - `onResult == Rejected` (대화에서 거절 노드) → `SetVerdict(Rejected)` + `WalkThrough(exitPath)`
  - 그 외 → **승인 대기 루프**: `AwaitingCheckIn=true` → 손님 클릭 대기.
    - 빈손 클릭(`RequestDialogueReplay`) → `DialogueRunner.Play` 재실행(전체 대화) → 다시 대기. 재대화에서 거절 선택 시 퇴장.
    - 열쇠 든 채 클릭(`ConfirmCheckIn`, `CheckInGuestEffect` 가 `HandItem.IsKey` 확인) → 열쇠 `RemoveActiveItem`+`Destroy` → `CheckIn(npc, nextRoom++)` + "checkin" 대사 + `WalkThrough(roomPath)`
    - 프롬프트: 열쇠 있으면 "체크인", 없으면 "대화" (`Interactable.IPromptOverride`)
  → `GuestView.Clear()` → 다음. 큐 끝 → 인스턴스 `Destroy` + `EndSession()`.
- **`EndSession()`** (public) — 큐 중단 + `UIInteractionMode.ExitAll()` + `OnSessionEnded` + `DayPhaseManager.Advance()`(→새벽 페이드). `K` 디버그 키로도 호출.
- **일시정지 / 재개** (`doc/0115`):
  - `UIInteractionMode.Exited` 구독 → `HandleUIExit`: ESC 로 빠져나오면 **`Paused=true`** — `guestMover.Frozen`·`DialogueRunner.Paused` 켜고 보이던 대화패널 숨김. 코루틴·손님 인스턴스·세션 그대로. `OnSessionEnded` 안 쏨.
  - `UIInteractionMode.Entered` 구독 → `HandleUIEnter`: 접객 테이블을 다시 상호작용(E)해 UI 모드 재진입하면 **재개** — `Frozen`·`Paused` 끄고 대화패널 복원.
  - `Motel_Table` `PhaseCondition.allowedPhases = {Noon, Evening}` — 저녁에도 테이블 상호작용 가능해야 재개됨.

## 알려진 한계 (doc/0104 (b) 범위)

- **승인 = "손님 클릭 = 다음 방번호 자동".** 모니터 방배정 UI·`RoomKey` 대조는 후속 doc.
- `CheckInGuestEffect` 는 대화 중에도 클릭되지만 `AwaitingCheckIn` 아니면 무동작. 프롬프트 게이팅은 후속.
- **거절 = 대화 CSV 로 저작** ("거절한다" Question → 조르기 Node 체인 → `outcome=Rejected`). 코드 필드 없음.
- 신분증 확인(SYS-04), 객실 배치도(SYS-06 풀버전) 범위 밖.
- 손님 3D 모델은 공용 오브젝트 하나 재사용 — NPC별 모델 스왑은 후속 (`NpcData.modelPrefab`).
- `receptionAnchor` 미할당 시 경고 로그 + 이동 없이 세션만 시작.

## 관련

[DialogueSystem](DialogueSystem.md) · [DayPhaseManager](DayPhaseManager.md) · [UIInteractionMode](UIInteractionMode.md) · [`doc/0097`](../doc/0097-reception-ui-mode-anchor.md) · [`doc/0100`](../doc/0100-note-and-monitor-interactions-design.md) · [`doc/0104`](../doc/0104-dialogue-npc-data-and-reception-flow.md)
