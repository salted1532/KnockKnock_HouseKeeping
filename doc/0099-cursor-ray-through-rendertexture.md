# 0099 - CursorInteractor 레이가 콜라이더와 안 맞음 (RenderTexture 경유)

## 요청
> ESC 로 빠져나왔을 때 마우스 락 + 커서 숨김.
> GameManager 에 다 연결했는데 제대로 됐는지 확인.
> 마우스 커서 레이가 상호작용 콜라이더랑 안 맞음. 화면에 보이는 물체 콜라이더 위치가 왜곡돼 보임.

## 1. ESC 퇴장 시 커서 (완료)
`UIInteractionMode.Exit` 가 이전엔 진입 전 커서 상태를 저장했다 복원 → UI 모드에서 나오면 항상 게임플레이라
`CursorLockMode.Locked` + `visible=false` 로 하드코딩. 저장/복원 필드 제거.

## 2. GameManager 배선 확인 — 불가 (씬 미저장)
`Assets/Scenes/InGame.unity` 에 `UIInteractionMode` / `DayPhaseManager` / `ReceptionManager` GUID 가 아직 없음
→ 컴포넌트 추가 후 **씬 저장(Ctrl+S)** 안 함. 저장하면 파일에서 필드 연결 검증 가능.

## 3. 커서 레이 왜곡 — 원인 규명

씬 렌더 파이프라인:
```
"MainCamera" (씬 루트, CinemachineBrain, FOV 40, cullingMask 4055)
   → RenderTexture "Posterize" (1280x720)
   → 풀스크린 RawImage (Overlay 캔버스, PxlCrush 픽셀 이펙트 머티리얼)
   → 화면
"UI Camera" (FOV 60, cullingMask 32=UI, 플레이어 시점에 붙어 이동)
```

`CursorInteractor` 는 `cam.ScreenPointToRay(Mouse.position)` 로 레이를 쏨. 이게 안 맞는 이유:
- 화면에 보이는 그림은 **MainCamera(FOV 40)** 가 1280x720 텍스처로 그린 것.
- 커서 좌표는 **실제 화면 픽셀**(예: 1920x1080).
- `cam` 이 UI Camera(FOV 60)면 → 레이가 표시된 화면보다 넓게 퍼짐 → 중앙은 맞고 **가장자리로 갈수록 어긋남** ("왜곡되어 보임"의 정체).
- `cam` 이 MainCamera면 → `ScreenPointToRay` 가 카메라 픽셀 크기(1280x720) 기준인데 커서는 화면 크기 기준 → 스케일 어긋남.

GazeInteractor 는 화면 정중앙(뷰포트 0.5,0.5)만 쏴서 FOV 차이 영향이 거의 없어 지금까지 문제 없었음.

## 계획 — CursorInteractor 레이를 RawImage 사각형 → 월드 카메라 뷰포트로 변환

`cam` 필드 → 3개로 교체:
- `worldCamera` : 월드를 RenderTexture 로 그리는 MainCamera
- `screen` : 그 텍스처를 표시하는 RawImage
- `canvasCamera` : RawImage 캔버스를 그리는 카메라 (Overlay 면 비워둠)

```csharp
Vector2 sp = Mouse.current.position.ReadValue();
if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
        screen.rectTransform, sp, canvasCamera, out Vector2 local)) return;   // 실패
Rect r = screen.rectTransform.rect;
float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
float v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
if (u < 0 || u > 1 || v < 0 || v > 1) { ClearOutline(); return; }             // 화면 밖
Rect uv = screen.uvRect;                                                       // 기본 0,0,1,1
Ray ray = worldCamera.ViewportPointToRay(new Vector3(uv.x + u*uv.width, uv.y + v*uv.height, 0));
```

FOV / RT 해상도 / 화면 해상도 / 종횡비 / RawImage 크기·위치 무관하게 정확.
PxlCrush 픽셀 스냅(셀 이하 오차) · 배럴 왜곡은 무시 (PxlCrush 는 기하 왜곡 없음).

### 선택: GazeInteractor 도 동일 정렬
`playerCamera.ScreenPointToRay(화면중앙)` → `worldCamera.ViewportPointToRay(0.5, 0.5)`.
중앙이라 체감 차이 거의 없지만 살짝 어긋난 조준을 없앨 수 있음. 리스크 낮음.

## 확인 필요
1. **콜라이더 자체가 메시와 어긋나 보이는지** 도 있는지? (씬에서 물체 선택 → 초록 BoxCollider 와이어프레임이 메시와 맞나) — 맞으면 순수 레이 문제, 안 맞으면 `Interactable` 자동 BoxCollider 크기 문제(별도).
2. GazeInteractor 도 같이 정렬할지.
3. `canvasCamera`: RawImage 캔버스가 Overlay 로 보임 → 비워두면 됨. 맞나?

## 적용 (2026-08-28)
- `UIInteractionMode.Exit` : 커서 저장/복원 제거 → `Locked` + `visible=false` 하드코딩.
- `CursorInteractor` : `cam` → `worldCamera` + `screen`(RawImage) + `canvasCamera` 로 교체.
  `TryCursorRay()` 가 커서 스크린 좌표를 RawImage 로컬 → 정규화 → `worldCamera.ViewportPointToRay` 로 변환.
  화면 밖이면 아웃라인 해제하고 무시.
- GazeInteractor 는 안 건드림 (중앙 조준이라 영향 미미).

## 사용자 작업 (에디터)
`CursorInteractor` (PlayerCapsule) 필드:
- `worldCamera` → "MainCamera" (RenderTexture 로 그리는 카메라, CinemachineBrain 붙은 것)
- `screen` → 게임 화면 RawImage
- `canvasCamera` → 비워둠 (Overlay 캔버스)
- `interactMask` → Interaction 레이어만 (Gaze 와 동일하게, 기본 Everything 이면 벽 먼저 맞음)

## 상태
2026-08-28 적용 완료. 에디터 필드 연결 + 검증 대기.
