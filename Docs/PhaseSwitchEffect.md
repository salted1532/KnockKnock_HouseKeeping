# PhaseSwitchEffect

`Assets/My/Scripts/Interaction/Effects/PhaseSwitchEffect.cs`

상호작용 시 하루 단계를 `from → to` 로 넘긴다. 한 방향, 명시적. 게시판/접객 테이블/침대 같은 "하루 진행 트리거" 에 붙인다.
`InteractionPrompt` 의 `아침종료`/`점심종료`/`저녁종료`/`하루종료` 표준 효과 (우클릭 "재설정" 으로 managed 자동 추가/제거).

## 필드

| 필드 | 기본 | 설명 |
|---|---|---|
| `from` (`DayPhase`) | Morning | 현재 단계가 이것일 때만 전환 (아니면 무시 — 안전장치) |
| `to` (`DayPhase`) | Noon | 전환 목표 |

우클릭 재설정 시 프롬프트에 맞춰 `from`/`to` 가 **자동 설정**됨:
아침종료 = Morning→Noon, 점심종료 = Noon→Evening, 저녁종료 = Evening→Dawn, 하루종료 = Dawn→Morning.
같은 오브젝트에 `PhaseCondition.allowedPhases = [from]` 도 자동으로 채워져 프롬프트/아웃라인이 해당 단계에서만 뜬다.

## 동작

`Play()` → `DayPhaseManager.Instance` 확인 → 현재 == `from` 이면 `TransitionTo(to)` ([ScreenFader](ScreenFader.md) 페이드 경유), 아니면 경고 로그만.

`Advance()`(순환상 다음)는 디버그 N/Q 와 `ReceptionManager` 접객 종료에서만 사용 — 이 효과는 항상 `TransitionTo` 로 목적지를 못박는다.

## 관련

[Interactable](InteractionSystem.md) · [PhaseCondition](PhaseCondition.md) · [DayPhaseManager](DayPhaseManager.md) · [`doc/0100`](../doc/0100-note-and-monitor-interactions-design.md)
