# DayEndTitle

`Assets/My/Scripts/Game/DayEndTitle.cs`

하루가 끝나 다음 날 아침으로 넘어간 직후 화면 중앙에 **"N일차"** 를 크게 1회 페이드. 설계 [`doc/0150`](../doc/0150-day-end-title-card.md), off-by-one 수정 [`doc/0157`](../doc/0157-dayendtitle-off-by-one.md).

## 배치

HUD Canvas 아래(풀스크린 권장) 오브젝트에 `CanvasGroup` + 큰 중앙 `TMP_Text` 를 두고 붙인다 (`[RequireComponent(CanvasGroup)]`).

| 필드 | 기본 | 설명 |
|---|---|---|
| `group` (`CanvasGroup`) | 자동 | 없으면 `GetComponent` |
| `label` (`TMP_Text`) | 자동 | 없으면 `GetComponentInChildren(true)` |
| `fadeIn` / `hold` / `fadeOut` | 0.5 / 1.8 / 0.9 | 초 |

## 동작

- `DayPhaseManager.OnPhaseChangeFinished`(페이드 인 완료) 구독 → `phase == Morning` 이면 발동.
- `N = DayPhaseManager.DayCount` (= 이제 시작하는 일차, HUD [`PhaseLabel`](PhaseLabel.md) 과 일치).
- **`day < 2` 면 표시 안 함** — 게임 시작 아침(1일차)엔 안 뜬다.
- `label.text` = `LocalizationManager.Korean ? "{day}일차" : "Day {day}"`.
- 페이드: alpha 0→1 (`fadeIn`) → `hold` 유지 → 1→0 (`fadeOut`). `blocksRaycasts`/`interactable` 항상 off.

뉴스 브리핑이 있는 날은 [`NightNewsBriefing`](NightNewsBriefing.md) 이 `TransitionTo(Morning, false)` 로 상태만 넘기고 자체 페이드로 밝히므로, 이 컴포넌트는 그 뒤 `OnPhaseChangeFinished` 에서 정상 발동한다 (브리핑이 HUD 를 복원한 뒤).

## 관련

[DayPhaseManager](DayPhaseManager.md) · [PhaseLabel](PhaseLabel.md) · [LocalizationManager](LocalizationManager.md) · [PhaseMessage](PhaseMessage.md) · [`doc/0150`](../doc/0150-day-end-title-card.md)
