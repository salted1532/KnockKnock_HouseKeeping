# SoundManager

`Assets/My/Scripts/Audio/SoundManager.cs`

사운드 전담 싱글턴. `[RequireComponent(typeof(AudioSource))]`.

## 인스펙터

| 필드 | 설명 |
|---|---|
| `nightClip` / `morningClip` | 밤/아침 앰비언스 (`Q` 키로 토글 — 임시 디버그) |
| `footstepSource` | 발소리 전용 AudioSource |
| `woodStepClips` / `concreteStepClips` / `metalStepClips` / `grassStepClips` | 지면 레이어별 발소리 클립 배열 |

## API

- `PlayFootstep(int groundLayer, float pitch = 1f)` — [FootstepSystem](FootstepSystem.md) 이 밟은 지면 레이어로 호출. 레이어 → 클립 배열 매핑(기본 Concrete), 직전 클립 연속 방지, `footstepSource.isPlaying` 이면 스킵.

## 현재 상태
`Q` 키 밤/아침 토글은 임시 디버그 — [DayPhaseManager](DayPhaseManager.md) 연결은 후속.

## 관련
[FootstepSystem](FootstepSystem.md) · [DayNightSwitcher](DayNightSwitcher.md)
