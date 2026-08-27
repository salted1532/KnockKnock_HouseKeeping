# GazeInteractor

`Assets/My/Scripts/Interaction/Drivers/GazeInteractor.cs` (구 `InteractionOutline.cs`, git mv 로 GUID 유지)

화면 중앙 레이 → 아웃라인 + 프롬프트, `E` 키로 상호작용. 플레이어(또는 카메라 리그)에 1개.

## 필드

| 필드 | 설명 |
|---|---|
| `interactDistance` (기본 3) | 사거리 |
| `interactMask` (`LayerMask`) | 씬에서 `Interaction`(11) 레이어만 |
| `playerCamera` (`Camera`) | 레이 기준 카메라 |
| `interactionText` (`GameObject`) | 프롬프트 UI (있으면 대상 유무에 따라 `SetActive`) |

## 동작 (`Update`)

1. `Suspended` 면 아무것도 안 함 (UI 모드에서 켜짐).
2. 화면 중앙 → `Physics.Raycast(interactMask)`.
3. **가림 체크** ([`doc/0077`](../doc/0077-interaction-raycast-layer.md)): 대상 앞에 막는 콜라이더(`Interaction`·`Ignore Raycast` 제외)가 있으면 무시 → 벽 너머 상호작용 차단.
4. 대상의 `Outline` 켜기, `interactionText` 표시.
5. `E` → `target.Interact(this, hitPoint)` 후 상태 클리어.

## 프로퍼티
- `Suspended` (bool) — true면 잠시 끔. `UIInteractionMode` 가 진입 시 켜고 해제 시 끔.

## 관련
[Interactor](InteractionSystem.md) · [Interactable](Interactable.md) · [UIInteractionMode](UIInteractionMode.md)
