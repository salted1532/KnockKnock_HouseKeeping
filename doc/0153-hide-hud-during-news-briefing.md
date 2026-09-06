# 0153 – 새벽 TV 뉴스 브리핑 중 대화창 외 UI 숨김

## 요청
새벽 TV 브리핑 동안 대화창(News_Panel)만 남기고 나머지 HUD(돈/할일/조준점/인벤토리 등)는 안 보이게.

## 구현 — `NightNewsBriefing.cs`

- 필드 추가: `[SerializeField] GameObject newsPanelRoot` — 뉴스 대화 패널 루트(`News_Panel`). 이것의 부모 = HUD `Canvas`.
- `HideHud(bool)`:
  - `true`: `newsPanelRoot` 의 부모(Canvas) 직속 자식 중 활성 상태인 것을 전부 `SetActive(false)`, 끈 목록을 `hudHidden` 에 저장.
    - 예외(유지): `newsPanelRoot` 본체, `ScreenFader` 가진 오브젝트(FadeOverlay), `RawImage` 가진 오브젝트(게임 화면).
  - `false`: `hudHidden` 에 담긴 것만 `SetActive(true)` 로 복원 (원래 꺼져 있던 UI 는 안 건드림).
- 호출: 여는 페이드 암전 콜백에서 `HideHud(true)`, 닫는 페이드 암전 콜백 **맨 앞**에서 `HideHud(false)` (그 뒤 `TransitionTo(Morning)` 가 `DayEndTitle` 등을 다시 트리거하므로 복원이 먼저여야 함).
- 새 HUD 위젯이 Canvas 에 추가돼도 자동으로 숨김 대상에 포함됨 (명시 리스트 아님).

## 씬 배선 (`InGame.unity`)
- `NightNewsBriefing.newsPanelRoot` = `Canvas/News_Panel` 연결, 씬 저장.

## 확인 (Play, `HideHud` 리플렉션 직접 호출)
- BEFORE → DURING: `RawImage`, `FadeOverlay` 만 유지, 나머지(CrossHair/Money/inventory/Watch/ScreenMessage/MorningTasks/LunchTasks/DayEndTitle/ObjectiveMarker 등) 전부 off.
- DURING → AFTER: 껐던 것만 정확히 복원, 원래 off 였던 것(Interaction_Text/note_image/Dialogue_Panel/ExitHint 등)은 off 유지.
