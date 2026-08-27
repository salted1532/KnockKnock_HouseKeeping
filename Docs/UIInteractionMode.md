# UIInteractionMode

`Assets/My/Scripts/Interaction/Modes/UIInteractionMode.cs`

책상 접객 등 "UI 모드" 매니저 (싱글턴). 플레이어/카메라를 앵커에 고정, 마우스 커서 표시, `GazeInteractor` 대신 `CursorInteractor` 로 전환. `ESC` 로 해제.

## 필드 (인스펙터 연결)

| 필드 | 설명 |
|---|---|
| `cameraTransform` | 이동시킬 카메라 Transform |
| `firstPersonController` (`MonoBehaviour`) | 진입 시 비활성화할 이동 컨트롤러 (StarterAssets `FirstPersonController`) |
| `characterController` | 진입 시 비활성화 (transform 이동 충돌 방지) |
| `gazeInteractor` / `cursorInteractor` | 진입 시 Gaze `Suspended=true` + Cursor `enabled=true`, 해제 시 반대 |
| `exitHint` (`GameObject`) | "ESC 나가기" UI (선택) |
| `moveTime` (기본 0.3) | 카메라 이동 lerp 시간 |

## 메소드

- `Enter(Transform anchor)` — 이미 Active 거나 anchor 없으면 무시.
  카메라 포즈·커서 상태 저장 → FPC·CC 비활성, Gaze 정지, 커서 표시 → 카메라를 anchor 로 `SmoothStep` lerp → 도착 시 `CursorInteractor` 활성.
- `Exit()` — 역순 복구. `Active` 중 `Update` 에서 `ESC` 감지 시 자동 호출.
- `Active` (bool) — 현재 UI 모드인가.

## 관련
[EnterUIModeEffect](EnterUIModeEffect.md) · [GazeInteractor](GazeInteractor.md) · [CursorInteractor](CursorInteractor.md) · [DayPhaseManager](DayPhaseManager.md)
