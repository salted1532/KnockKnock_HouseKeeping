# 0162 – 화면 비율 깨짐 원인 + Road_Image 클릭 탈출

## 요청
1. CRTMonitor Canvas/버튼/Road_Image 편집 후 "게임 시작 시 비율이 깨진다" — 원인 확인
2. `Road_Image` 클릭도 `Background` 클릭처럼 화면고정(UIInteractionMode)에서 빠져나오게

## 이슈 1 — 원인 (전체 렌더 파이프라인)
모니터가 아니라 화면 전체 문제.
- `MainCamera` → 고정 **1280×720(16:9)** RenderTexture `Posterize` 로 렌더 (targetTexture 인스펙터 고정, 리사이즈 스크립트 없음)
- `Canvas/RawImage` 가 그 RT 표시. 앵커 stretch(0~1), **`AspectRatioFitter` 없음** → 창 비율로 강제 스트레치
- 창/Game뷰가 16:9가 아니면 월드·HUD·모니터 전부 비균등 왜곡. 격자·글자 많은 모니터 UI에서 제일 티남
- `RenderTextureGraphicRaycaster` / `CursorInteractor` 는 뷰포트 정규화라 클릭은 정상, 렌더 왜곡만 문제

### 수정 (사용자 선택: A 레터박스) — `Assets/Scenes/InGame.unity`
- `Canvas/RawImage`: 앵커·pivot 중앙, `AspectRatioFitter(FitInParent, 1280/720=1.7778)` 추가
- `Canvas/Letterbox_BG` 신규(검정 Image, stretch, raycast off) = sibling 0, RawImage = sibling 1
- 결과: RawImage rect 가 항상 16:9 유지, 창이 16:9 아니면 검정 레터/필러박스. 왜곡 0.
- 한계: HUD(돈/시계 등)는 여전히 창 전체 기준 → 창이 16:9 아니면 검은 띠 위에 걸침(별개, 필요 시 후속)

## 이슈 2 — Road_Image 클릭 탈출
`Background` 는 `InteractableProxyClick`(target 비움 → 부모 `CRTMonitor` Interactable = `MonitorViewEffect` 토글)로 재클릭 시 `UIInteractionMode.Exit()`. `Road_Image` 는 `Image` 만 있어 클릭을 먹고 무동작.

### 수정 — `Assets/My/InGame/Prefabs/Item/CRTMonitor.prefab`
- `Road_Image` 에 `InteractableProxyClick` 추가 (target 비움 → Background 와 동일 경로)
- `Road_Image/Text (TMP)` `Raycast Target` off (라벨, 클릭 통과)

## 검증 (플레이모드)
```
[var] RawImage rect = 1919.6 x 1079.8 (aspect 1.7778)  fitter=FitInParent
[var] Letterbox_BG siblingIndex=0  RawImage siblingIndex=1
[var] scene Road_Image InteractableProxyClick = True
[var] scene Road_Image Text raycastTarget = False
```
`uloop compile` 클린. (Road_Image 클릭→탈출은 배선상 Background 와 동일 경로라 동작 확실, 인게임 클릭 재현은 미실시)
