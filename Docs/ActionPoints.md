# ActionPoints

`Assets/My/Scripts/Game/ActionPoints.cs` · `Assets/My/Scripts/Interaction/Conditions/ActionPointsDepletedCondition.cs` · `Assets/My/Scripts/UI/ActionPointsHud.cs`

새벽 행동력 (SYS-10·11). 새벽에 할 수 있는 행동 횟수 제한 → 다 쓰면 침대로 새벽을 끝낼 수 있다. 커밋 `72135b17`("새벽 행동력 추가").

## ActionPoints (싱글턴)

`GameManager` 등에 붙인다. `[RuntimeInitializeOnLoadMethod]` 정적 리셋 있음 (도메인 리로드 끔 대비 — [`doc/0157`](../doc/0157-dayendtitle-off-by-one.md) 이 이 패턴을 다른 매니저에도 권장).

| 필드 | 기본 | 설명 |
|---|---|---|
| `perDawn` (`int`) | 4 | 새벽마다 이 값으로 초기화. `Max` |

- `Current` — 남은 행동력. `OnChanged(current, max)` 이벤트 (HUD 용).
- `OnPhaseChanged(Dawn)` 구독 → `Current = perDawn`.
- `Use(int = 1)` — 새벽 손님과 대화 1회당 [`KnockEffect`](KnockEffect.md) 가 호출 (`KnockEffect.cs:128`).
- `ForceDeplete()` — `Can_Coke`(에너지 드링크) 등 "새벽을 즉시 끝내는" 소비 아이템용. 남은 행동력을 0으로.

## ActionPointsDepletedCondition

`InteractionCondition`. `IsMet => ActionPoints.Instance.Current <= 0`. 새벽 침대(`bed_03_Interior`) `Interactable` 에 얹어, 행동력을 다 써야 잠자기(→ 아침/뉴스 브리핑)가 가능하게 한다. [`TasksCompleteCondition`](MorningTasks.md) 과 동일 패턴.

## ActionPointsHud

Watch(손목시계) HUD 밑 4칸 바. `pips` 에 `Image` 4개 연결 — 남은 행동력만큼 왼쪽부터 `filledColor`, 나머지는 `emptyColor`. `OnChanged` 구독, 새벽에만 표시(폴링).

## 관련

[KnockEffect](KnockEffect.md) · [DayPhaseManager](DayPhaseManager.md) · [NightNewsBriefing](NightNewsBriefing.md) · [ObjectiveMarker](ObjectiveMarker.md) · [`doc/0145`](../doc/0145-nightly-tv-news-briefing.md)
