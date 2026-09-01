# 0130 - 모니터 화면 배경 클릭 = 모니터 상호작용, 버튼은 버튼

## 요청
모니터 화면 배경을 클릭하면 모니터(오브젝트) 상호작용이 되도록. 안의 방배정 버튼은 버튼 클릭 그대로.

## 배경
doc/0129 로 "커서가 UI 위면 월드 무시" 를 넣으니, 모니터 `ScreenUI/Background`(raycastTarget=true, 불투명)도
UI 라서 배경 클릭 시 아무것도 안 일어남 (배경엔 클릭 핸들러 없음).

## 수정
### 신규 `Interaction/InteractableProxyClick.cs`
`IPointerClickHandler` — 붙은 UI 그래픽 클릭 시 지정/상위 `Interactable.Interact(null, pos)` 실행.
`target` 비우면 `GetComponentInParent<Interactable>()`.

### `CRTMonitor.prefab` → `ScreenUI/Background` 에 `InteractableProxyClick` 추가
- `Background` raycastTarget=true 유지 → EventSystem 이 클릭 전달 → 상위 `CRTMonitor` Interactable 실행 (EnterUIModeEffect 등).
- 방배정 버튼(101~110)은 각자 raycastTarget=true + Button.onClick → 배경 핸들러 안 탐 (버튼 그래픽이 히트).
- `CursorInteractor` 는 배경/버튼 모두 IsPointerOverGameObject=true 라 월드 레이 안 쏨 → 중복 실행 없음.

## 동작
| 클릭 위치 | 결과 |
|---|---|
| 화면 빈 배경 | `CRTMonitor.Interact()` — 접객 자리에서면 모니터 앵커로 화면고정 스택 진입 |
| 방 번호 버튼 | `ReceptionManager.AssignRoom(n)` (토글) |
| 화면 밖 월드 | 평소대로 |

## 추가 (같은 세션) — 화면고정 토글

모니터를 다시 클릭(= 배경 클릭)하면 화면고정이 풀리도록:
- `UIInteractionMode.IsTopAnchor(Transform)` 추가 — 이 앵커가 현재 최상위 뷰인지.
- `EnterUIModeEffect` 에 `toggle`(기본 true): `IsTopAnchor(anchor)` 면 `Enter` 대신 `Exit()`.
  - 게임플레이에서 E → 진입 / 모니터 뷰에서 배경 클릭 → 해제(Depth 1→0 = 완전 종료 / 스택이면 하위 복귀).
- 접객 모드는 `ReceptionManager` 가 `Enter` 직접 호출 → 영향 없음.

런타임 검증: Interact #1 Active=True Depth=1 / Interact #2 Active=False Depth=0.

## 상태
2026-08-31 완료. 컴파일 0에러.
