# EnterUIModeEffect

`Assets/My/Scripts/Interaction/Effects/EnterUIModeEffect.cs`

상호작용 시 UI 모드 진입 (모니터·컴퓨터 등 "화면고정"). `InteractionPrompt.화면고정` 표준 효과 (managed — 우클릭 "재설정" 으로 `EnterUIModeEffect` + `SfxEffect` 자동).

> 구 프롬프트 이름은 `접객`. index 8 을 `화면고정` 으로 rename (기존 프리팹 자동 매핑). **`PhaseCondition` 자동 추가 없음** — 모니터는 시간대 제한이 없다. 저녁 접객 세션 진입은 [`ReceptionManager`](ReceptionManager.md) 가 별도로 구동한다.

## 필드

| 필드 | 설명 |
|---|---|
| `anchor` (`Transform`) | 플레이어가 이동할 위치/정면 (앉은 시점, `Player_Anchor`). 비우면 이 오브젝트 transform |
| `toggle` (기본 true) | 이미 이 `anchor` 뷰가 최상위면 다시 상호작용 시 `Exit()`(화면고정 해제/한 겹 pop). 끄면 진입만 |

## 동작

`Play` → `toggle && UIInteractionMode.IsTopAnchor(anchor)` 면 `Exit()` (모니터 다시 클릭 = 화면고정 풀기), 아니면 `Enter(anchor)`. 이후는 [UIInteractionMode](UIInteractionMode.md) 가 처리 (플레이어 고정, 커서 표시, Gaze↔Cursor 전환, ESC 로 한 겹 해제).
모니터 화면 배경 클릭도 [`InteractableProxyClick`](../doc/0130-monitor-background-click-interacts.md) 로 이 효과를 태워 같은 토글이 된다.

접객 모드 중 모니터를 누르면 앵커 스택에 쌓인다 → 모니터의 `anchor` 는 접객 앵커와 **다른** 오브젝트여야 함(같으면 재진입으로 무시).

## 관련

[UIInteractionMode](UIInteractionMode.md) · [ReceptionManager](ReceptionManager.md) · [Interactable](InteractionSystem.md) · [`doc/0100`](../doc/0100-note-and-monitor-interactions-design.md)
