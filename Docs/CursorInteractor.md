# CursorInteractor

`Assets/My/Scripts/Interaction/Drivers/CursorInteractor.cs`

마우스 커서 레이 → 호버 아웃라인, 좌클릭 상호작용. **UI 모드(`UIInteractionMode`)에서만 활성화**된다 (평소 `enabled = false`).

## 필드

| 필드 | 설명 |
|---|---|
| `interactDistance` (기본 5) | 사거리 |
| `interactMask` (`LayerMask`) | 레이어 마스크 |
| `cam` (`Camera`) | 레이 기준 카메라 (`Reset` 에서 `Camera.main` 자동) |

## 동작 (`Update`)

1. 마우스 위치 → `cam.ScreenPointToRay` → `Physics.Raycast(interactMask)`.
2. 호버한 `Interactable` 의 `Outline` 켜기.
3. 좌클릭 → `hovered.Interact(this, hitPoint)`.

`GazeInteractor` 와 달리 가림 체크·프롬프트 텍스트 없음 (책상 위 근거리 오브젝트 대상이라 불필요).

## 관련
[GazeInteractor](GazeInteractor.md) · [UIInteractionMode](UIInteractionMode.md)
