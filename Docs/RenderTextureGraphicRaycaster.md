# RenderTextureGraphicRaycaster

`Assets/My/Scripts/Interaction/Drivers/RenderTextureGraphicRaycaster.cs`

`GraphicRaycaster` 서브클래스. **오브젝트 화면(CRT 모니터 등)에 얹은 World Space Canvas** 의 uGUI
(Button/Toggle/Slider…)를 정상 클릭되게 한다.

## 왜 필요한가

게임 화면 = `MainCamera`(FOV 40) → `Posterize` RenderTexture(1280×720) → 풀스크린 `RawImage`(Overlay) → 화면.
일반 `GraphicRaycaster` 는 마우스 스크린 좌표(예 1920×1080)를 Event Camera 에 그대로 넘기는데, 이 간접
렌더 때문에 좌표·FOV·종횡비가 어긋나 버튼이 엉뚱한 자리에서 눌린다. ([`CursorInteractor`](CursorInteractor.md) 와 같은 문제 — [`doc/0099`](../doc/0099-cursor-ray-through-rendertexture.md))

## 동작

`Raycast()` 오버라이드:
1. 커서를 풀스크린 `RawImage` rect 로컬 좌표로 정규화 → `(u,v)`.
2. `(u,v) * worldCamera.pixelWidth/Height` (RT 픽셀 공간) 로 `eventData.position` 을 임시 치환.
3. `base.Raycast` (Event Camera = MainCamera 로 자동 연결) → 올바른 그래픽 히트.
4. `eventData.position` 복원 (드래그/델타 계산은 실제 커서 위치 사용).

EventSystem/InputModule 은 그대로 → uGUI 이벤트 전부 정상.

## 필드

| 필드 | 설명 |
|---|---|
| `worldCamera` / `screen` / `canvasCamera` | 비우면 씬의 [`CursorInteractor`](CursorInteractor.md) 에서 자동 참조 (같은 RT 파이프라인). 프리팹이 씬 참조를 못 담아도 런타임 해결 |

## 배선

- Canvas: Render Mode = **World Space**. 이 컴포넌트를 `GraphicRaycaster` 대신 붙인다. Event Camera 는 `OnEnable` 이 자동 설정.
- 캔버스 layer 는 MainCamera cullingMask 안 & UI Camera 밖 (= layer 0/11) → MainCamera 가 RT 에 같이 그려 PxlCrush 자동 적용. 머티리얼/RenderTexture 불필요.
- **씬 필수**: 풀스크린 `RawImage.raycastTarget = false` (Overlay 가 월드 캔버스 클릭 삼킴).

## 게이트 / 한계

- `UIInteractionMode.Instance.Active` 가 false 면 즉시 return — 게임플레이 중 조준점 클릭이 화면 버튼 누르는 것 방지. 화면고정/접객 모드에서만 반응.
- 드래그 델타는 실제 스크린 픽셀 기준 (ScrollRect 감도 살짝 다름). Canvas 는 평면 (CRT 곡률 미반영).

## 관련

[CursorInteractor](CursorInteractor.md) · [EnterUIModeEffect](EnterUIModeEffect.md) · [UIInteractionMode](UIInteractionMode.md) · [`doc/0119`](../doc/0119-crt-monitor-world-canvas-ui.md)
