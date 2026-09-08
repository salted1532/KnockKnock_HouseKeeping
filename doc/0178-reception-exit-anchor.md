# 0178 – 접객 종료 시 정해진 앵커로 나오기

## 요청
- 인게임→메인화면 시 마우스 잠금 해제 + 커서 표시 (별건, 아래 "보류" 참고)
- 접객모드에서 빠져나올 때 진입 직전 위치가 아니라 **정해진 앵커 위치/정면**으로 나오도록
- 앵커는 인스펙터에서 직접 연결할 수 있게

## 구현

### `UIInteractionMode.cs`
- `Enter(anchor, lookScale, escExits, Transform exitAnchor = null)` — 선택 파라미터 추가.
  첫 진입(`!Active`)에서만 `teardownExitAnchor` 필드에 저장 (위에 쌓이는 뷰의 값은 무시).
- `Teardown()` — `teardownExitAnchor` 있으면 `savedPlayerPos/Rot` 대신 그 위치로 트랜지션
  (Y 회전만, pitch 0). 종료 후 필드 클리어.
- 모니터(`MonitorViewEffect`)·노크(`KnockEffect`)·`EnterUIModeEffect` 는 `exitAnchor` 미전달
  → 기존대로 진입 직전 위치로 복귀 (무회귀).

### `ReceptionManager.cs`
- `[SerializeField] Transform receptionExitAnchor` 추가 (`접객 자리` 헤더).
  비우면 종전 동작(시작 직전 위치 복귀).
- `BeginSession()` → `UIInteractionMode.Instance.Enter(receptionAnchor, 1f, false, receptionExitAnchor)`

## 씬 배선 (사용자)
- `ReceptionManager` 인스펙터의 **Reception Exit Anchor** 슬롯에 빈 Transform 하나 만들어 연결.
  위치 = 접객 끝나고 서 있을 자리, Y 회전 = 바라볼 방향.

## 검증
- `uloop compile` → Success, 0 error / 0 warning.

## 인게임→메인화면 커서 잠금 해제
- `MainMenu.cs` 에 `Awake()` 추가 — `Cursor.lockState = None; Cursor.visible = true;`.
  MainScene 진입 경로 전부(일시정지 나가기·첫 실행) 한 군데서 커버. `uloop compile` Success.

## 변경 파일
- `Assets/My/Scripts/Interaction/Modes/UIInteractionMode.cs`
- `Assets/My/Scripts/Game/ReceptionManager.cs`
- `Assets/My/Scripts/Game/MainMenu.cs`
