# DayPhaseManager

`Assets/My/Scripts/Game/DayPhaseManager.cs`

하루 진행: **아침 → 점심 → 저녁(접객) → 새벽**, 다음 날 아침으로 순환. 싱글턴.
전환은 [`ScreenFader`](ScreenFader.md) 를 거친다 — 암전 시점에 상태 갱신, 페이드 인 완료 후 후속 이벤트.

```csharp
public enum DayPhase { Morning, Noon, Evening, Dawn }
```

## 필드

| 필드 | 설명 |
|---|---|
| `startPhase` (`DayPhase`) | 시작 단계 |
| `debugAdvanceKey` (bool, 기본 on) | 디버그: `N` 또는 `Q` 로 `Advance()` |

## 프로퍼티 / 이벤트

| 이름 | 설명 |
|---|---|
| `Instance` (static) | 싱글턴 |
| `Current` (`DayPhase`) | 현재 단계 |
| `DayCount` (int) | 며칠째. 어느 단계에서든 `Morning` 으로 **진입**할 때 +1 |
| `Transitioning` (bool) | 페이드 전환 중 (재-Advance 무시됨) |
| `OnPhaseChanged` (`event Action<DayPhase>`) | **암전 중** 발동 — 게이트·비주얼용 (`Start` 최초 1회 포함) |
| `OnPhaseChangeFinished` (`event Action<DayPhase>`) | **페이드 인 완료 후** 발동 — 후속 연출용 |

## 메소드

- `Advance()` — 순환상 다음 단계로 `TransitionTo`. 디버그 N/Q, `ReceptionManager` 접객 종료에서 사용.
- `TransitionTo(DayPhase target)` — `target` 으로 페이드 전환. 이미 전환 중이거나 같은 단계면 무시.
  전환 중엔 `UIInteractionMode.FreezeForOverlay(true)` 로 플레이어 조작 정지 (UI 모드 중이면 무시됨).
  암전 시점: `target==Morning` 이면 `DayCount++`, `Current` 갱신, `OnPhaseChanged`.
  `ScreenFader` 없으면 즉시 전환.
- `TransitionTo(DayPhase target, bool fade)` — `fade=false` 면 페이드·`FreezeForOverlay` 없이 `DayCount++`·`Current`·이벤트만 (동기). 호출 연출이 자체 페이드를 제공할 때 ([`NightNewsBriefing`](NightNewsBriefing.md) 닫는 페이드). `TransitionTo(target)` = `(target, true)`.

## 소비자

| 스크립트 | 하는 일 |
|---|---|
| [PhaseVisuals](PhaseVisuals.md) | `OnPhaseChanged` → 4단계 조명/스카이박스/볼륨/fog 스왑 |
| [SoundManager](SoundManager.md) | `OnPhaseChanged` → 저녁·새벽=밤 / 아침·점심=낮 앰비언스 |
| [ReceptionManager](ReceptionManager.md) | `OnPhaseChanged` → `Evening` 시 접객 세션 시작 |
| [PhaseCondition](PhaseCondition.md) | 상호작용을 특정 단계로 게이팅 |
| [PhaseSwitchEffect](PhaseSwitchEffect.md) | 상호작용으로 `TransitionTo` 호출 (게시판/테이블) |
| [NightNewsBriefing](NightNewsBriefing.md) | 침대(새벽): 뉴스 브리핑 연출 후 `TransitionTo(Morning)`. `Playing` 이 디버그 키 가드 |
| [PhaseLabel](PhaseLabel.md) | `OnPhaseChanged` → HUD 텍스트 |
| [PhaseMessage](PhaseMessage.md) | `OnPhaseChangeFinished` → 해당 단계 진입 시 `ScreenMessage` 문구 1회 (새벽="손님은 다 온 것 같다…") |

## 관련

[`doc/0078`](../doc/0078-interaction-system-redesign.md) · [`doc/0097`](../doc/0097-reception-ui-mode-anchor.md) · [`doc/0100`](../doc/0100-note-and-monitor-interactions-design.md)
