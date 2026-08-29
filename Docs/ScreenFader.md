# ScreenFader

`Assets/My/Scripts/Environment/ScreenFader.cs`

전체 화면 검정 페이드. 싱글턴. `[RequireComponent(typeof(CanvasGroup))]`.
"암전 → 콜백 → 밝아짐" 연출 — 시간대 전환에서 조명/설정 스왑을 가려준다.

## 필드

| 필드 | 기본 | 설명 |
|---|---|---|
| `outDuration` | 0.4 | 밝음 → 암전 |
| `holdDuration` | 0.1 | 암전 유지 (이 동안 `atBlack` 처리) |
| `inDuration` | 0.6 | 암전 → 밝음 |

## API

- `FadeThrough(Action atBlack, Action done = null)` — 암전 → `atBlack()` → 유지 → 밝아짐 → `done()`.
  **이미 진행 중이면 무시** (코루틴 1개만 유지).
- `IsFading` (bool) / `Instance` (static).

페이드 동안 `CanvasGroup.blocksRaycasts = true` 로 클릭 차단.
씬에 `ScreenFader` 가 없으면 호출측(`DayPhaseManager` 등)이 `atBlack`/`done` 을 즉시 실행 (null-safe).

## 씬 작업

게임 화면 RawImage 위(Overlay 캔버스)에 풀스크린 검정 `Image` + `CanvasGroup` + 이 컴포넌트.
에디터에서 꺼둔 채 두고 [`ActivateOnAwake`](ActivateOnAwake.md) 로 런타임에 켜는 패턴 사용.

## 관련

[DayPhaseManager](DayPhaseManager.md) · [PhaseVisuals](PhaseVisuals.md) · [ActivateOnAwake](ActivateOnAwake.md) · [`doc/0100`](../doc/0100-note-and-monitor-interactions-design.md)
