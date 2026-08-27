# PhaseCondition

`Assets/My/Scripts/Interaction/Conditions/PhaseCondition.cs`

지정한 하루 단계에서만 상호작용 허용. `InteractionCondition` 구현체.

## 필드

| 필드 | 설명 |
|---|---|
| `allowedPhases` (`DayPhase[]`, 기본 `{Evening}`) | 이 단계들에서만 상호작용 허용 |

## 동작

- `IsMet` : `DayPhaseManager.Instance` 없으면 항상 true. 있으면 `Current` 가 `allowedPhases` 에 포함될 때만 true.
- 불만족 시 `Interactable.CanInteract == false` → 아웃라인·프롬프트도 안 뜸.

## 용도
책상 접객 = `Evening` 만 허용. 밤에만 열리는 문 = `{Evening, Dawn}` 등.

## 관련
[InteractionCondition](InteractionCondition.md) · [DayPhaseManager](DayPhaseManager.md) · [EnterUIModeEffect](EnterUIModeEffect.md)
