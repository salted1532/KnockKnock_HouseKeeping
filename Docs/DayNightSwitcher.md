# DayNightSwitcher

`Assets/My/Scripts/Environment/DayNightSwitcher.cs`

라이팅/스카이박스/포스트프로세싱 볼륨 프로파일을 밤↔아침으로 전환.

## 필드

| 필드 | 설명 |
|---|---|
| `nightSkybox` / `morningSkybox` (`Material`) | 스카이박스 |
| `nightLight` / `morningLight` (`GameObject`) | 디렉셔널 라이트 |
| `globalVolume` (`Volume`) | URP 글로벌 볼륨 |
| `nightProfile` / `morningProfile` (`VolumeProfile`) | 볼륨 프로파일 |

## 동작

`Q` 키로 `isNight` 토글 → `SetNight()` / `SetMorning()`:
- 스카이박스 교체, 라이트 오브젝트 on/off, `globalVolume.sharedProfile` 교체, `RenderSettings.fog` on/off.

## 현재 상태
`Q` 키 토글은 임시 디버그 — 4단계 [DayPhaseManager](DayPhaseManager.md) 연결 시 `OnPhaseChanged` 구독으로 교체 예정 (Evening/Dawn = 밤).

## 관련
[DayPhaseManager](DayPhaseManager.md) · [SoundManager](SoundManager.md)
