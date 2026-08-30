# PhaseCondition

`Assets/My/Scripts/Interaction/Conditions/PhaseCondition.cs`

지정한 하루 단계에서만 상호작용 허용. `InteractionCondition` 구현체.

## 필드

| 필드 | 설명 |
|---|---|
| `allowedPhases` (`DayPhase[]`, 기본 `{Evening}`) | 이 단계들에서만 상호작용 허용 |

## 동작

- `IsMet` : `DayPhaseManager.Instance` 없으면 항상 true. 있으면 `Current` 가 `allowedPhases` 에 포함될 때만 true.
- 불만족 시 `Interactable.CanInteract == false` → 클릭·프롬프트·**아웃라인** 모두 안 뜸 (`CursorInteractor`/`GazeInteractor` 가 `CanInteract` 통과한 대상에서만 아웃라인을 켬, `doc/0111`).

## 용도
접객 테이블(`Motel_Table`) = `{Noon, Evening}` — Noon 클릭 = 저녁 전환 트리거, Evening 클릭 = ESC 로 일시정지한 접객 세션 재개 (`doc/0115`). Morning/Dawn 은 차단. 밤에만 열리는 문 = `{Evening, Dawn}` 등.

## 관련
[InteractionCondition](InteractionCondition.md) · [DayPhaseManager](DayPhaseManager.md) · [EnterUIModeEffect](EnterUIModeEffect.md)
