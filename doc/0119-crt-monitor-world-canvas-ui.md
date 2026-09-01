# 0119 - CRT 모니터 World Space Canvas UI + RT 파이프라인 클릭 보정

> **doc/0118 과 연계**: 0118(모니터 방배정 + 새벽 노크 제안)은 모니터 버튼을 "물리 Quad ×10
> (월드 uGUI 는 CursorInteractor 로 안 눌림)" 으로 가정했다. 이 문서가 그 제약을 없앤다 —
> `RenderTextureGraphicRaycaster` 로 월드 캔버스 uGUI 버튼이 정상 동작하므로, 0118 의
> `RoomAssignEffect` 를 물리 Quad 대신 `ScreenUI` 아래 uGUI Button 으로 구현 가능.

## 요청

`Assets/My/InGame/Prefabs/Item/CRTMonitor.prefab` 의 `screenON`(단방향 화면 메쉬)에 메쉬 모양대로
Canvas UI 를 붙여, 플레이어가 화면고정 상태에서 uGUI 버튼을 조작할 수 있게 한다.

- 별도 머티리얼/RenderTexture 없이 **World Space Canvas 를 화면 메쉬 위에 직접** 얹는다.
  (MainCamera 가 그 캔버스를 게임 RT(Posterize)에 같이 그려 PxlCrush/Posterize 효과를 자동으로 받음)
- **uGUI 버튼 버전** 확정 (콜라이더 hit-test 버전 아님).

## 문제

게임 화면 = `MainCamera`(FOV 40) → `Posterize` RT(1280x720) → 풀스크린 `RawImage`(Overlay) → 화면.
일반 `GraphicRaycaster` 는 마우스 스크린 좌표(예 1920x1080)를 Event Camera 에 그대로 넘기는데,
이 간접 렌더 때문에 좌표·FOV·종횡비가 어긋나 버튼이 엉뚱한 위치에서 눌린다.
([[project_ingame-rendertexture-pipeline]] — `CursorInteractor.TryCursorRay` 가 같은 문제를 겪었음)

## 구현 (코드)

| 파일 | 내용 |
|---|---|
| `Interaction/Drivers/RenderTextureGraphicRaycaster.cs` | **신규.** `GraphicRaycaster` 서브클래스. `Raycast()` 오버라이드 → 커서를 풀스크린 RawImage rect 로컬 좌표로 정규화 → `(u,v) * worldCamera.pixelWidth/Height`(RT 픽셀 공간) 로 `eventData.position` 임시 치환 → `base.Raycast` → 복원. EventSystem/InputModule 은 그대로라 Button/Toggle/Slider 정상 동작 |
| `Interaction/Drivers/CursorInteractor.cs` | `WorldCamera`/`Screen`/`CanvasCamera` public getter 추가 (raycaster 가 같은 RT 참조 재사용) |

### RenderTextureGraphicRaycaster 동작
- `worldCamera`/`screen`/`canvasCamera` 를 인스펙터에서 비워두면 `OnEnable`/`Raycast` 에서
  씬의 `CursorInteractor` 를 찾아 자동 복사 → **프리팹이 씬 참조를 못 담아도 런타임 해결**.
- `OnEnable` 이 `Canvas.worldCamera`(Event Camera)를 MainCamera 로 자동 연결.
- **게이트**: `UIInteractionMode.Instance.Active` 가 false 면 `Raycast` 즉시 return.
  → 게임플레이 중(커서 잠김) 조준점 클릭이 화면 버튼을 누르는 것 방지.
  화면고정/접객 모드에서만 반응. `ponytail:` 전역 Active 게이트 — 정밀 게이트(anchor 매칭)는 필요 시.

### 알려진 한계
- 드래그/델타는 실제 스크린 픽셀 기준(복원 후) — RT 공간 대비 ~1.5배 스케일. 버튼엔 무관,
  ScrollRect 는 스크롤 감도가 살짝 다를 수 있음. `ponytail:` 주석.
- 곡률: Canvas 는 평면. CRT 볼록함은 무시(또는 풀스크린 CRT 셰이더에서 처리). Curved UI 에셋 안 씀.

## 에디터 작업 (완료 — uloop 자동)

1. `CRTMonitor.prefab` → `screenON` 자식 `ScreenUI` 생성:
   - `Canvas`(World Space) + `CanvasScaler`(dynamicPixelsPerUnit 3) + `RenderTextureGraphicRaycaster`
   - RectTransform: sizeDelta 800x600(4:3), localScale 0.00038, localRotation identity,
     localPosition (0, 0.0279, 0.106) — 화면 메쉬 로컬 bounds 중심 + 전면 ~1cm 앞
   - 검증: UI 평면 크기 world 0.228x0.171 ≈ 메쉬 0.229x0.170, forward 일치, 중심 화면 앞 ~2.7cm
   - 자식: `Background`(풀 rect Image, 짙은 남색 불투명), `SampleButton`(Image+Button, ColorTint)
2. `InGame.unity` 풀스크린 `RawImage.raycastTarget` → **false** (Overlay 가 월드 캔버스 클릭 삼키는 것 방지)

## 사용자 작업 (남음)

1. **실제 UI 구성**: `ScreenUI` 아래 `SampleButton` 삭제/교체, 필요한 이미지·버튼·텍스트(TMP) 배치.
2. **위치 미세조정**: `screenON/ScreenUI` localPosition.z (0.106) 를 화면 유리에 맞게 조절.
   화면이 뒤집혀 보이면 localRotation Y +180.
3. **검증 (Play)**: 모니터 상호작용(`화면고정`) → 카메라가 앵커로 이동 → 커서로 `SampleButton` 위 호버 시
   색 변함 + 클릭 시 눌림 색. ESC 복귀. 게임플레이 중엔 버튼 반응 없어야 함(게이트).
4. `screenON` MeshRenderer 는 유지(뒤 배경광) 또는 끄기 — 취향.

## 후속: 접객 → 노크 대화 전체 흐름

모니터 UI 가 들어갈 상위 게임 루프 = **doc/0118** (모니터 방배정 + `RoomController` + 새벽 노크 제안,
잔여 확인 A~G 대기 중). 이 문서(0119)는 그 중 "모니터 UI" 를 uGUI 로 만들 수 있게 하는 인프라만 담당.

## 버그 & 수정 (2026-08-31, Play 테스트 중)

**증상**: 캔버스는 화면에 나타나는데 버튼 클릭이 아예 안 됨.

**원인**: `ScreenUI` 를 `screenON` 자식으로 localRotation identity 로 두면 캔버스 forward =
화면 메쉬 법선 = **방(플레이어) 쪽**. 즉 캔버스가 카메라를 **등짐** (`dot(canvas.fwd, cam.fwd) ≈ -1`).
- 렌더: UI 셰이더가 Cull Off 라 뒷면이 그려짐 → 텍스트 **좌우 미러링** (스크린샷에서 "1 2 3" 뒤집힘 확인).
- 클릭: `GraphicRaycaster.ignoreReversedGraphics`(기본 on)가 "뒤집힌 그래픽"으로 판정 → **모든 히트 폐기**.

**수정**:
1. `CRTMonitor.prefab` → `ScreenUI` localRotation **(0, 180, 0)** — 카메라를 마주봄. 미러링 해제 + dot ≈ +1.
2. `RenderTextureGraphicRaycaster.OnEnable` 에서 `ignoreReversedGraphics = false` 강제 — 화면 메쉬 방향에
   따라 캔버스를 어느 쪽으로 돌리든 클릭이 되게 (등지면 미러링이 눈에 띄니 방향 실수는 바로 발견됨).
3. `CursorInteractor` 자동참조용 `FindObjectOfType` → `FindAnyObjectByType(FindObjectsInactive.Include)` (deprecation).

런타임 raycast 테스트로 4개 버튼 전부 히트 확인 (`ignoreReversedGraphics` 기본값에서도, flip 후).

## 상태
2026-08-31 코드 + 프리팹/씬 배선 + 클릭 버그 수정 완료 (uloop). 컴파일 0 에러.
※ 병행 세션이 `doc/0118`(MonitorRoomBoard = 이 캔버스의 방배정 버튼) 구현 중 — 이 문서 인프라 위에 얹힘.
