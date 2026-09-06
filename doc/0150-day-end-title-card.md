# 0150 – 일차 종료 "N일차" 타이틀 카드

## 요청
각 일차가 끝날 때 화면 중앙에 "N일차" 를 크게 1회 알려주기.

## 구현

### `Assets/My/Scripts/Game/DayEndTitle.cs` (신규)
- `DayPhaseManager.OnPhaseChangeFinished` 구독. `phase == Morning` 이고 `DayCount - 1 >= 1` 이면 표시.
  - `DayCount` 는 아침 진입(AtBlack)에서 이미 ++ 됨 → 방금 끝난 일차 = `DayCount - 1`.
  - 게임 시작 아침(전환 없음)에는 안 뜸.
- 텍스트: 한국어 `"{n}일차"`, 영어 `"Day {n}"` (`LocalizationManager.Korean`).
- 페이드: `CanvasGroup` alpha 코루틴 (fadeIn 0.5 / hold 1.8 / fadeOut 0.9). `ScreenMessage` 와 동일 패턴.
- 비차단(blocksRaycasts=false). 새벽 TV 뉴스 브리핑 경로(`TransitionTo(Morning, false)`)도 `OnPhaseChangeFinished` 를 발생시키므로 그 뒤에도 정상 표시.

### 씬 배선 (`InGame.unity`, uloop 다이나믹 코드)
- HUD `Canvas` 아래 `DayEndTitle` 오브젝트 생성: 풀스크린 RectTransform + `CanvasGroup` + `DayEndTitle`.
  - 자식 `Text` (`TextMeshProUGUI`): Galmuri11 SDF, fontSize 110, Bold, Center, 흰색 + 검정 아웃라인 0.15, raycastTarget off.
  - `group` / `label` 인스펙터 연결 완료. HUD 내 마지막 형제(맨 위 렌더).
- 씬 저장됨.

## 런타임 확인
- Play → `Current` 를 Dawn 으로 세팅 후 `TransitionTo(Morning)` → `DayCount 1→2`, 카드 텍스트 `"1일차"`, `CanvasGroup.alpha` peak 1.0 확인.
- 스크린샷: 중앙에 큰 "1일차" 표시됨. (동시에 Morning `PhaseMessage` 나레이션도 떠서 살짝 겹침 — 필요하면 나레이션 문구/타이밍 조정)

## 참고
- 상단 HUD `PhaseLabel` 은 현재 일차("2일차 · 아침")를 계속 표시 → 카드의 "1일차"(끝난 일차)와 숫자가 다름. 의도대로.
