# 0151 – 시간대 전환 중 마우스 커서 노출 방지

## 요청
각 시간대 넘어갈 때 마우스 커서가 보이는 것 막기.

## 원인
`DayPhaseManager.TransitionTo(fade:true)` 가 전환 시작 시 `UIInteractionMode.FreezeForOverlay(true)` 호출 →
`FreezeForOverlay` 는 노트 읽기용으로 설계돼 `on` 이면 무조건 `Cursor.visible = true` + `lockState = None`.
그래서 페이드 암전~복귀(~1.1s) 동안 커서가 화면에 뜸.

## 수정 (코드 2곳)

### `UIInteractionMode.cs`
`FreezeForOverlay(bool on, Transform anchor, bool showCursor)` 오버로드 추가.
- 기존 2개 시그니처는 `showCursor: true` 로 위임 → 노트/뉴스 브리핑 동작 불변.
- `on && showCursor` 일 때만 커서 표시, 아니면 `Locked` + 숨김.

### `DayPhaseManager.cs`
전환 페이드용 호출을 `FreezeForOverlay(true, null, false)` 로 변경 → 전환 중 커서 숨김 유지.
해제(`FreezeForOverlay(false)`)는 원래대로 `Locked` + 숨김.

## 확인
- 컴파일 에러 0.
- Play + `Advance()` (페이드 전환) 중 80ms 간격 20회 샘플 → `Cursor.visible` 계속 False, `lockState` 계속 Locked. 전환 후 Morning 에서도 False/Locked.
- 새벽 뉴스 브리핑 경로는 `TransitionTo(fade:false)` 라 이 호출 자체를 안 타므로 영향 없음.
