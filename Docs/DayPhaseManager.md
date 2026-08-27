# DayPhaseManager

`Assets/My/Scripts/Game/DayPhaseManager.cs`

하루 진행: **아침 → 점심 → 저녁(접객) → 새벽**, 다음 날 아침으로 순환. 싱글턴.

```csharp
public enum DayPhase { Morning, Noon, Evening, Dawn }
```

## 필드

| 필드 | 설명 |
|---|---|
| `startPhase` (`DayPhase`) | 시작 단계 |
| `debugAdvanceKey` (bool, 기본 on) | 디버그: `N` 키로 다음 단계 |

## 프로퍼티 / 이벤트

| 이름 | 설명 |
|---|---|
| `Instance` (static) | 싱글턴 |
| `Current` (`DayPhase`) | 현재 단계 |
| `DayCount` (int) | 며칠째 (새벽 → 아침 넘어갈 때 +1) |
| `OnPhaseChanged` (`event Action<DayPhase>`) | 단계 변경 시 발동 (`Start` 에서 최초 1회 포함) |

## 메소드
- `Advance()` — 다음 단계로. 새벽이면 `DayCount++`.

## 현재 상태 / 연동 예정

- [PhaseCondition](PhaseCondition.md) 이 소비 중 (책상 접객 = Evening 게이트).
- `SoundManager` / `DayNightSwitcher` 의 `Q` 키 낮밤 디버그 토글은 아직 이 매니저에 연결 안 됨 (단계 → 시각 매핑은 후속 작업).

## 관련
[PhaseCondition](PhaseCondition.md) · [UIInteractionMode](UIInteractionMode.md) · [`doc/0078`](../doc/0078-interaction-system-redesign.md)
