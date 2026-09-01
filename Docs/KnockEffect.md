# KnockEffect

`Assets/My/Scripts/Interaction/Effects/KnockEffect.cs`

새벽 노크 상호작용. `RoomController.knockTarget` 에 부착 (`Interactable` promptType = `Knock`).
앵커·손님 스폰 위치는 부모 [RoomController](RoomController.md) 에서 읽는다 (방 배선을 한 곳에 모음). `doc/0118`.

## 필드

| 필드 | 설명 |
|---|---|
| `guestPrefab` (`GameObject`) | 접객과 같은 Guest 프리팹. **비우면 [`ReceptionManager.GuestPrefab`](ReceptionManager.md) 폴백** |
| `dawnPanel` (`SpeechBubble`) | 비우면 스폰된 손님의 자식 `SpeechBubble`. **거절 대사는 이게 있어야 표시됨** (스크린 패널 권장) |
| `knockWait` (float, 기본 3) | 노크 후 응답까지 대기 |
| `peekAngle` (float, 기본 18) | 정문이 살짝 열리는 각도 |
| `openTime` (float, 기본 0.5) | 문 여닫는 시간 |

## 동작 (`Play` → 코루틴)

1. `busy` 면 무시. `RoomController.NightGuest` 없으면 로그 후 종료.
2. **즉시 화면고정** — `UIInteractionMode.Enter(rc.KnockAnchor)` (플레이어 이동도 정지).
3. `knockWait` 초 대기.
4. **새벽이 아니거나(`DayPhaseManager.Current != Dawn` — 아침·점심 청소 시간) `npc.refusesDawnKnock`** (거절):
   - `refuseMessages` 중 랜덤 하나 → `ScreenMessage.Show`
   - **새벽일 때만** `dawnPanel` + `Dawn/refuse` 노드로 문 너머 한 마디. 아침·점심엔 대사 없이 `refuseReadTime` 대기 (doc/0136)
   - `UIInteractionMode.Exit()` → 문 안 열림, 손님 스폰 안 함
5. **수락**:
   - `rc.PeekDoor(peekAngle, openTime)` — 정문 살짝 열림
   - `rc.GuestSpawnPoint` 에 `guestPrefab` 스폰, `GuestView.Apply(npc)`, 손님 `Interactable.enabled = false`
   - `DialogueRunner.Play(npc, bubble, Situation.Dawn, onDone)` — 질문 대화
   - `onDone`: `GuestView.Clear()` + 손님 파괴 + `rc.PeekDoor(0)`(문 닫기) + `UIInteractionMode.Exit()` + `busy = false`
6. 종료 후 재노크 가능 (거절 손님은 계속 거절).

## 데이터

- `situation=Dawn` CSV 행 = 수락 손님 대사.
- 거절 손님: `refusesDawnKnock` 플래그 + (선택) `nodeKey=refuse` 한 줄.

## 관련
[RoomController](RoomController.md) · [UIInteractionMode](UIInteractionMode.md) · [DialogueSystem](DialogueSystem.md) · [Interactable](Interactable.md) · [`doc/0118`](../doc/0118-monitor-room-assignment-and-dawn-knock.md)
