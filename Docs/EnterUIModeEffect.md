# EnterUIModeEffect

`Assets/My/Scripts/Interaction/Effects/EnterUIModeEffect.cs`

상호작용 시 UI 모드 진입 (책상 접객 등). `Interactable`(접객) + `SfxEffect` + `PhaseCondition`(Evening) 과 함께 쓴다.

## 필드

| 필드 | 설명 |
|---|---|
| `anchor` (`Transform`) | 카메라가 이동할 위치/방향 (앉은 시점). 비우면 이 오브젝트 transform |

## 동작

`Play` → `UIInteractionMode.Instance.Enter(anchor)`.

이후는 [UIInteractionMode](UIInteractionMode.md) 가 처리 (플레이어/카메라 고정, 마우스 표시, `GazeInteractor`↔`CursorInteractor` 전환, ESC 해제).

## 관련
[UIInteractionMode](UIInteractionMode.md) · [PhaseCondition](PhaseCondition.md) · [Interactable](Interactable.md)
