# MorningTasks

`Assets/My/Scripts/Game/MorningTasks.cs` · `Assets/My/Scripts/Interaction/Conditions/TasksCompleteCondition.cs`

아침 할일(우상단 HUD) + 게시판 점심 전환 게이트. 설계·구현 이력 [`doc/0144`](../doc/0144-morning-todo-list-and-bed-tidy-reset.md).

지금 할일은 **"침대 개기"** 1종. 씬의 모든 [`RoomController`](RoomController.md) 가 이번 아침에 흐트러뜨린 침대(체크아웃/하우스키핑 아침, 방당 랜덤 1~2개)를 합산해 `☐ 침대 개기 3/6` 로 표시하고, 전부 개면 게시판이 열린다.

## MorningTasks (싱글턴)

HUD Canvas 우상단 패널 오브젝트에 붙인다. 컴포넌트 자체는 항상 켜진 오브젝트에 두고 `panel`(자식)만 아침에 토글 — 자기를 끄면 `Update` 가 멈춘다.

| 필드 | 설명 |
|---|---|
| `panel` (`GameObject`) | 아침에만 켜지는 할일 패널 루트. 비우거나 자기 자신이면 `line` 오브젝트만 토글 |
| `line` (`TMP_Text`) | `☐ 침대 개기 made/total` 를 그릴 텍스트 |

- `AllDone` — `Current == Morning` && `Made >= Total`. `Total`/`Made` = 모든 `RoomController` 의 `MessyTotal`/`MessyDone` 합산.
- 폴링 방식 (매 프레임 방 순회 + `FindObjectsByType` 1회 캐시). `Bed`·이벤트 배선 0. `ponytail:` — 방이 수백 개 되면 이벤트로.
- 청소할 방이 없는 아침(`Total == 0`) → `AllDone == true` → 게시판 바로 통과.

## TasksCompleteCondition

`InteractionCondition` (9줄). `IsMet => MorningTasks.Instance.AllDone`.

게시판(`Owner's_Motel_Room/White_Board`) `Interactable` 에 얹는다. 할일 미완료 동안엔 게시판의 상호작용/프롬프트/아웃라인이 안 뜨고, 완료 순간 [`ObjectiveMarker`](ObjectiveMarker.md) 가 "게시판으로 가자" 안내 + HUD 마커를 띄운다.

## 관련

[RoomController](RoomController.md) · [DayPhaseManager](DayPhaseManager.md) · [PhaseSwitchEffect](PhaseSwitchEffect.md) · [ObjectiveMarker](ObjectiveMarker.md) · [LunchTasks](LunchTasks.md) · [`doc/0144`](../doc/0144-morning-todo-list-and-bed-tidy-reset.md)
