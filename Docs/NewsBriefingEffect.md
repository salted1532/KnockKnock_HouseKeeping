# NewsBriefingEffect

`Assets/My/Scripts/Interaction/Effects/NewsBriefingEffect.cs`

새벽 침대 상호작용 → 일차 종료 뉴스 브리핑([`NightNewsBriefing`](NightNewsBriefing.md)) → 아침 전환. 새벽→아침 자리의 [`PhaseSwitchEffect`](PhaseSwitchEffect.md)`(Dawn→Morning)` 를 대체한다 ([`doc/0145`](../doc/0145-nightly-tv-news-briefing.md)).

## 필드

| 필드 | 설명 |
|---|---|
| `briefing` ([`NightNewsBriefing`](NightNewsBriefing.md)) | 재생할 브리핑. 비면 바로 아침으로 |

## 동작

`Play()`:
1. `DayPhaseManager.Instance` 없으면 경고 후 리턴.
2. `Current != Dawn` 이면 무시 (안전장치 — `PhaseCondition(Dawn)` 이 이미 게이팅).
3. `NightNewsBriefing.Playing` 이면 무시 (재상호작용 방지).
4. `briefing == null || !briefing.Play()` → `DayPhaseManager.TransitionTo(Morning)` (브리핑 없음/오늘 뉴스 콘텐츠 없음 → 기존 `PhaseSwitchEffect` 동작). 아니면 브리핑이 끝나며 스스로 아침 전환.

침대(`bed_03_Interior`)에는 `PhaseCondition(Dawn)` + [`ActionPointsDepletedCondition`](ActionPoints.md) + [`ObjectiveMarker`](ObjectiveMarker.md) 도 함께 붙는다.

## 관련

[NightNewsBriefing](NightNewsBriefing.md) · [PhaseSwitchEffect](PhaseSwitchEffect.md) · [DayPhaseManager](DayPhaseManager.md) · [ActionPoints](ActionPoints.md) · [`doc/0145`](../doc/0145-nightly-tv-news-briefing.md)
