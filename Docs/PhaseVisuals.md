# PhaseVisuals

`Assets/My/Scripts/Environment/PhaseVisuals.cs`

시간대별 조명/스카이박스/볼륨/fog 스왑. **구 `DayNightSwitcher` 대체** (밤↔아침 2상태 → 4단계).
`DayPhaseManager.OnPhaseChanged`(암전 시점) 구독 → 즉시 적용, 페이드에 가려짐.

## 필드

| 필드 | 설명 |
|---|---|
| `globalVolume` (`Volume`) | URP 글로벌 볼륨 — `sharedProfile` 을 교체 |
| `looks` (`PhaseLook[4]`) | **Morning, Noon, Evening, Dawn 순서**. 아침/점심이 같은 값을 가리켜도 됨 |

### `PhaseLook` (struct)

| 필드 | 설명 |
|---|---|
| `skybox` (`Material`) | `RenderSettings.skybox` |
| `lightRoot` (`GameObject`) | 이 단계에서 켤 라이트 묶음. 다른 단계 것은 전부 꺼짐 |
| `volume` (`VolumeProfile`) | `globalVolume.sharedProfile` |
| `fog` (bool) | `RenderSettings.fog` |

## 동작

`Start` 에서 구독 + 현재 단계 즉시 적용. `Apply(phase)`:
1. `skybox` / `fog` / `volume` 적용 (null 이면 스킵).
2. 모든 `looks[].lightRoot` 를 끄고 현재 단계 것만 켬 (중복 참조 시 마지막 승자).

`DayPhaseManager` 없으면 경고 로그 + 아무것도 안 함.

## 씬 작업

구 `DayNightSwitcher` 오브젝트는 스크립트 빠진 상태 → `PhaseVisuals` 붙이고 `globalVolume` + `looks[4]` 채우기.

## 관련

[DayPhaseManager](DayPhaseManager.md) · [ScreenFader](ScreenFader.md) · [SoundManager](SoundManager.md) · [`doc/0100`](../doc/0100-note-and-monitor-interactions-design.md)
