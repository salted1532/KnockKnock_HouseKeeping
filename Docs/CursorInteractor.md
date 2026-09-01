# CursorInteractor

`Assets/My/Scripts/Interaction/Drivers/CursorInteractor.cs`

마우스 커서 레이 → 호버 아웃라인, 좌클릭 상호작용. **UI 모드([`UIInteractionMode`](UIInteractionMode.md))에서만 활성화**된다 (평소 `enabled = false`).

## 필드

| 필드 | 설명 |
|---|---|
| `interactDistance` (기본 5) | 사거리 |
| `interactMask` (`LayerMask`, 기본 Everything) | 대상 레이어. **`Interaction` 레이어로 좁혀야** 가림 체크가 동작 (Everything 이면 무효) |
| `worldCamera` (`Camera`) | 월드를 RenderTexture 로 그리는 카메라 (MainCamera). `Reset` 에서 `Camera.main` |
| `screen` (`RawImage`) | 그 RenderTexture 를 표시하는 풀스크린 RawImage |
| `canvasCamera` (`Camera`) | RawImage 캔버스를 그리는 카메라. Screen Space - Overlay 면 비워둠 |

## 동작 (`Update`)

0. **커서가 UI 위면(`EventSystem.IsPointerOverGameObject`)** 월드 레이는 안 쏜다 (`doc/0129`) —
   대신 그 UI 가 얹힌 `Interactable`(모니터 화면 등)의 **외곽선만** `ResolveUIOutline()` 로 켠다 (`doc/0141`).
   `EventSystem.RaycastAll` → 첫 히트의 `GetComponentInParent<Interactable>()`(CanInteract) → 그 `Outline`/`SpriteOutline`.
   클릭·프롬프트는 안 건드림 (버튼은 자기 `onClick`, 모니터 배경은 `InteractableProxyClick`). 풀스크린 RawImage 는 `raycastTarget=false`.
1. **커서 좌표 → RawImage 로컬 → 정규화 뷰포트 → `worldCamera` 레이.**
   게임 화면이 MainCamera → RenderTexture → RawImage(PxlCrush) 경유라 커서 스크린 좌표를 그대로 못 쓴다. RawImage 사각형·`uvRect` 기준으로 변환 (FOV/해상도/종횡비 무관). 화면 밖이면 아웃라인 해제.
2. `Physics.Raycast(interactMask)`.
3. **가림 체크** ([`GazeInteractor`](GazeInteractor.md) 와 동일) — 대상 앞에 막는 콜라이더(Interaction·Ignore Raycast 제외)가 있으면 무시 → 벽 너머 클릭 차단.
4. 호버한 `Interactable`(+`CanInteract`) 의 `Outline` 켜기.
5. 좌클릭 → `hovered.Interact(this, hitPoint)`. (UI 호버 경로에선 클릭 안 함 — `hovered` 가 null)

외곽선 켜고 끄기는 `if (hitOutline != currentOutline)` diff 한 곳이 담당 (월드·UI 공용) — 소유자 1개.

프롬프트 텍스트는 없음 (책상 위 근거리 오브젝트 대상).

## 관련

[GazeInteractor](GazeInteractor.md) · [UIInteractionMode](UIInteractionMode.md) · [`doc/0099`](../doc/0099-cursor-ray-through-rendertexture.md)
