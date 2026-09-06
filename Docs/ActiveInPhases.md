# ActiveInPhases

`Assets/My/Scripts/Environment/ActiveInPhases.cs`

지정한 시간대에만 `target` 들을 켜고, 나머지 시간대엔 끈다. [`doc/0156`](../doc/0156-street-light-phase-toggle.md). [`PhaseVisuals`](PhaseVisuals.md) 와 같은 패턴(암전 시점 `OnPhaseChanged` 구독 → 즉시 적용, 페이드에 가려짐).

| 필드 | 기본 | 설명 |
|---|---|---|
| `activePhases` (`DayPhase[]`) | `{Evening, Dawn}` | 이 단계들에서만 `target` on |
| `targets` (`GameObject[]`) | — | 켜고 끌 오브젝트. **비우면 이 GameObject 의 자식 전부** (자신을 끄면 이벤트를 못 받으므로 자식만) |

## 동작

`Start` 에서 `DayPhaseManager.OnPhaseChanged` 구독 + 현재 단계로 1회 `Apply`. `DayPhaseManager` 없으면 경고. `Apply(phase)` → `on = activePhases.Contains(phase)` → `targets`(또는 자식) `SetActive(on)`.

## 용도

가로등(`street light` 프리팹): 루트에 부착, `targets = [Open Cylinder(램프 렌즈), Point Light]`. 저녁·새벽만 점등, 폴 메시는 루트라 항상 보임.

## 관련

[DayPhaseManager](DayPhaseManager.md) · [PhaseVisuals](PhaseVisuals.md) · [PhaseCondition](PhaseCondition.md) · [`doc/0156`](../doc/0156-street-light-phase-toggle.md)
