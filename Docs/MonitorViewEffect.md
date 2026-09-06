# MonitorViewEffect

`Assets/My/Scripts/Interaction/Effects/MonitorViewEffect.cs`

모니터처럼 "가볍게 보다 마는" 화면고정 — 클릭으로 진입, **다시 클릭하거나 ESC 로 바로 해제**. [`doc/0144`](../doc/0144-morning-todo-list-and-bed-tidy-reset.md) 에서 [`EnterUIModeEffect`](EnterUIModeEffect.md) 를 두 갈래로 나눈 것 중 하나.

| | `MonitorViewEffect` | [`EnterUIModeEffect`](EnterUIModeEffect.md) |
|---|---|---|
| 용도 | CRT 모니터 방배정 화면 | 접객 자리·연출용 화면고정 |
| 나가기 | ESC 1회 / 재클릭 (`escExits: true`) | `exitKey`(Backspace) 홀드만 |
| 스택 | 접객 모드 **위에 중첩** 가능 — ESC/재클릭은 이 뷰만 벗고 접객 복귀 | — |

## 필드

| 필드 | 설명 |
|---|---|
| `anchor` (`Transform`) | 화면고정 위치 + 정면. 비우면 이 오브젝트 |

## 동작

`Play()` → `UIInteractionMode.Instance` 확인 → `IsTopAnchor(anchor)` 면 `Exit()`(다시 클릭 = 풀기), 아니면 `Enter(anchor, 1f, escExits: true)`.

> `CRTMonitor` 는 수동 배선 — `Interactable` 우클릭 "효과 재설정"(`SyncEffectsToPrompt`) 을 돌리면 `ViewScreen` 프롬프트가 `EnterUIModeEffect` 를 도로 붙이므로 주의.

## 관련

[EnterUIModeEffect](EnterUIModeEffect.md) · [UIInteractionMode](UIInteractionMode.md) · [MonitorRoomBoard](MonitorRoomBoard.md) · [CursorInteractor](CursorInteractor.md) · [`doc/0144`](../doc/0144-morning-todo-list-and-bed-tidy-reset.md)
