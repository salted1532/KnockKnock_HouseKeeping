# UIInteractionMode

`Assets/My/Scripts/Interaction/Modes/UIInteractionMode.cs`

책상 접객·모니터 등 "UI 모드" 매니저 (싱글턴). 플레이어를 `Player_Anchor` 위치/정면으로 이동시켜 화면을 고정, 마우스 커서 표시, [`GazeInteractor`](GazeInteractor.md) 대신 [`CursorInteractor`](CursorInteractor.md) 로 전환. `ESC` 로 한 겹씩 해제.

**앵커 스택** — 접객 모드(하위) 안에서 모니터 `화면고정`(상위)을 눌러 들어가고, ESC 로 상위만 닫으면 하위(접객)로 복귀. 스택이 비면 완전 종료(플레이어 원위치).

## 필드 (인스펙터 연결)

| 필드 | 설명 |
|---|---|
| `playerRoot` (`Transform`) | PlayerCapsule — yaw + 위치 이동 대상 |
| `cameraPitchPivot` (`Transform`) | PlayerCameraRoot — pitch |
| `firstPersonController` (`MonoBehaviour`) | 진입 시 비활성화 (StarterAssets `FirstPersonController`) |
| `characterController` | 진입 시 비활성화 (끈 뒤에야 transform 이동 가능) |
| `gazeInteractor` / `cursorInteractor` | 진입 시 Gaze `Suspended=true` + Cursor `enabled=true`, 해제 시 반대 |
| `exitHint` (`GameObject`) | "ESC 나가기" UI (선택) |
| `crosshair` (`GameObject`) | 조준점 UI — UI/오버레이 모드 동안 숨김 (선택) |
| `moveTime` (기본 0.3) | 앵커 이동 `SmoothStep` lerp 시간 |
| `edgeLook` (bool, 기본 off) | 켜면 커서를 화면 가장자리로 옮겨 앵커 정면 기준 살짝 둘러보기. 끄면 완전 고정 |
| `yawRange` / `pitchRange` (40 / 25) | `edgeLook` 클램프 각도 |
| `edgeDeadZone` (0.25) | 화면 중앙 이 비율 안에서는 시야 안 움직임 |
| `lookLerp` (4) | 목표 각도 수렴 속도 |

## 프로퍼티 / 이벤트

| 이름 | 설명 |
|---|---|
| `Instance` (static) | 싱글턴 |
| `Active` (bool) | UI 모드 진행 중 |
| `FrozenForOverlay` (bool) | `FreezeForOverlay(true)` 상태 (노트·페이드 전환) |
| `MovementLocked` (bool) | `Active \|\| FrozenForOverlay` — 발소리 등 이동 연출 게이트용 (`doc/0142`) |
| `Depth` (int) | 쌓인 앵커 수 (0=비활성, 1=접객만, 2=접객+모니터 …) |
| `Entered` (`event Action`) | 첫 진입 시 |
| `Exited` (`event Action`) | 스택 비고 완전 종료(`Teardown`) 시 — `ReceptionManager` 가 세션 정리에 구독 |

## 메소드

- `Enter(Transform anchor)` — anchor 뷰로 진입. 첫 진입이면 플레이어 상태 저장 + FPC/CC 끔 + Gaze 정지 + 커서 표시 + 크로스헤어 숨김. 이미 Active 면 스택 위에 쌓음. 같은 앵커 재진입은 무시. anchor/`playerRoot` null 이면 경고 후 중단.
- `Enter(Transform anchor, float lookScale)` — 위와 같되 이 앵커의 `edgeLook` 각도에 `lookScale`(0~1) 을 곱한다. 앵커별로 스택에 쌓임 (`0` = 이 뷰에선 완전 고정, `1` = 기본). `KnockEffect` 가 `0.25` 로 노크 화면을 거의 고정 (`doc/0118`).
- `Exit()` — 한 단계 pop. 하위 뷰 남으면 그리로 복귀(모드 유지), 스택 비면 `Teardown`(플레이어 원위치 복귀 + 커서 락).
- `ExitAll()` — 전부 닫고 완전 종료 (접객 `EndSession` 등).
- `FreezeForOverlay(bool)` — **이동 없이** FPC 정지 + Gaze suspend + 커서 표시 (노트 `ShowPanelEffect`, 시간대 전환 중). `Active` 면 무시 — 그쪽이 이미 관리 중.

## ESC 처리

`Update` 에서 ESC → `Exit()` (한 겹). 단 `ShowPanelEffect.ConsumesEsc` 면(노트 열림/방금 닫힘) 소비 안 함 → 노트 먼저 닫히고, 다음 ESC 에 모드 탈출.

## 관련

[EnterUIModeEffect](EnterUIModeEffect.md) · [ShowPanelEffect](ShowPanelEffect.md) · [ReceptionManager](ReceptionManager.md) · [CursorInteractor](CursorInteractor.md) · [`doc/0097`](../doc/0097-reception-ui-mode-anchor.md) · [`doc/0100`](../doc/0100-note-and-monitor-interactions-design.md)
