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
| `firstRoomNumber` (int, 기본 101) | 승인 시 자동 증가하는 객실번호 |
| `enterDelay` (float, 기본 0.6) | 착석/페이드 후 첫 손님까지 |
| `debugEndKey` (bool, 기본 on) | 디버그: 세션 중 `K` 로 즉시 종료(큐 중단 → 새벽) |

## 프로퍼티 / 이벤트

| 이름 | 설명 |
|---|---|
| `Instance` (static) | 싱글턴 |
| `InSession` (bool) | 접객 세션 진행 중 |
| `AwaitingCheckIn` (bool) | 대화 끝, 손님 클릭 대기 중 (`CheckInGuestEffect` 가 확인) |
| `ConfirmCheckIn()` | `CheckInGuestEffect` 가 손님 클릭 시 호출 → 현재 손님 체크인 |
| `OnSessionStarted` / `OnSessionEnded` (`event Action`) | 손님 큐/신분증 심사/정산을 이 위에 얹음 |

## 동작

- `DayPhaseManager.OnPhaseChanged` 구독 → `Evening` 되면 `BeginSession()`:
  `UIInteractionMode.Enter(receptionAnchor)` + `InSession=true` + `OnSessionStarted` + (오늘 편성 있으면) 손님 큐 코루틴.
- **손님 큐** (`GuestQueue`): `guestPrefab` 1개 `Instantiate`(재활용). `campaign.Day(DayCount).eveningGuestIds` 순회 → `catalog.Get(id)` → `GuestView.Apply(npc)` → `GuestMover.WarpTo(guestSpawn)` → `WalkThrough(entryPath)` → `DialogueRunner.Play(npc, bubble, Reception, onResult)` →
  - `visitorOnly` → `WalkThrough(exitPath)` (대화만)
  - `onResult == Rejected` (대화에서 거절 노드) → `SetVerdict(Rejected)` + `WalkThrough(exitPath)`
  - 그 외 → `AwaitingCheckIn=true` → 손님 클릭 대기 → `CheckIn(npc, nextRoom++)` + `WalkThrough(roomPath)`
  → `GuestView.Clear()` → 다음. 큐 끝 → 인스턴스 `Destroy` + `EndSession()`.
- **`EndSession()`** (public) — 큐 중단 + `UIInteractionMode.ExitAll()` + `OnSessionEnded` + `DayPhaseManager.Advance()`(→새벽 페이드).
- `UIInteractionMode.Exited` 구독 → ESC 로 접객 레벨까지 빠져나오면 `HandleUIExit`: 큐 중단 + 세션 정리, **하루 전환은 안 함** (테스트용).

## 알려진 한계 (doc/0104 (b) 범위)

- **승인 = "손님 클릭 = 다음 방번호 자동".** 모니터 방배정 UI·`RoomKey` 대조는 후속 doc.
- `CheckInGuestEffect` 는 대화 중에도 클릭되지만 `AwaitingCheckIn` 아니면 무동작. 프롬프트 게이팅은 후속.
- **거절 = 대화 CSV 로 저작** ("거절한다" Question → 조르기 Node 체인 → `outcome=Rejected`). 코드 필드 없음.
- 신분증 확인(SYS-04), 객실 배치도(SYS-06 풀버전) 범위 밖.
- 손님 3D 모델은 공용 오브젝트 하나 재사용 — NPC별 모델 스왑은 후속 (`NpcData.modelPrefab`).
- `receptionAnchor` 미할당 시 경고 로그 + 이동 없이 세션만 시작.

## 관련

[DialogueSystem](DialogueSystem.md) · [DayPhaseManager](DayPhaseManager.md) · [UIInteractionMode](UIInteractionMode.md) · [`doc/0097`](../doc/0097-reception-ui-mode-anchor.md) · [`doc/0100`](../doc/0100-note-and-monitor-interactions-design.md) · [`doc/0104`](../doc/0104-dialogue-npc-data-and-reception-flow.md)
