# SoundManager

`Assets/My/Scripts/Audio/SoundManager.cs`

사운드 전담 싱글턴. `[RequireComponent(typeof(AudioSource))]` (앰비언스 루프 소스).

## 인스펙터

| 필드 | 설명 |
|---|---|
| `nightClip` / `morningClip` | 밤/낮 앰비언스 루프 |
| `footstepSource` | 발소리 전용 AudioSource |
| `woodStepClips` / `concreteStepClips` / `metalStepClips` / `grassStepClips` | 지면 레이어별 발소리 클립 배열 |

## 동작

- `Start` 에서 `DayPhaseManager.OnPhaseChanged` 구독 → `ApplyAmbience`:
  **저녁·새벽 = `nightClip`, 아침·점심 = `morningClip`**. 같은 클립이면 재생 유지.
  `DayPhaseManager` 없으면 `nightClip` 재생.
- `PlayFootstep(int groundLayer, float pitch = 1f)` — [FootstepSystem](FootstepSystem.md) 이 밟은 지면 레이어로 호출. 레이어 → 클립 배열 매핑(기본 Concrete), 직전 클립 연속 방지, `footstepSource.isPlaying` 이면 스킵.

## 개선 여지

게임 규모 대비 얇음 — BGM, SFX 카테고리 볼륨/뮤트, AudioSource 풀링, 3D 감쇠 등 없음 (README 스크립트 정리 분석 참고).

## 관련

[FootstepSystem](FootstepSystem.md) · [DayPhaseManager](DayPhaseManager.md) · [PhaseVisuals](PhaseVisuals.md)
