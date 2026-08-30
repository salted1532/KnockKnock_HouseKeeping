# SpriteOutline

`Assets/My/Scripts/Interaction/SpriteOutline.cs`

2D 스프라이트 오브젝트(현재 Guest)용 hover 하이라이트. QuickOutline([Outline](Interactable.md)) 은
3D 메쉬 실루엣용이라 `SpriteRenderer` 에 붙으면 fill 머티리얼이 스프라이트 쿼드를 덧그려 깨진다 (doc/0108).

## 동작

`Awake` 에서 `source` 밑에 자식 GameObject `SpriteOutline` + `SpriteRenderer` 를 만든다:
- 머티리얼 = `material` (비우면 `source.sharedMaterial` 복사 — 이 경우 단색 안 되고 초상화가 그대로 비침). **`My/SpriteSilhouette`** (`Assets/My/InGame/Material/SpriteSilhouette.mat`) 을 지정하면 텍스처 알파만 써서 `color` 단색 실루엣으로 그림
- 색 = `color` (SpriteRenderer 정점 색), `localScale` = `scale` (원본보다 그만큼 큼)
- 시작은 꺼짐

`SetHighlighted(bool)` — Interactor 가 hover 진입/이탈 시 호출. 켤 때 `sprite`/`flipX/Y`/`sortingOrder`(원본 + `sortingOffset`) 를 즉시 미러하고, 켜져 있는 동안 `LateUpdate` 가 계속 미러. 꺼져 있으면 비용 0.

중심 기준 균일 확대라 알파 모양 외곽선이 아니라 실루엣이 살짝 두꺼워지는 방식.

## 필드

| 필드 | 기본 | 설명 |
|---|---|---|
| `source` (`SpriteRenderer`) | 비면 자식에서 탐색 | 따라갈 원본 (Guest = `GuestView.body` = `Square`) |
| `material` (`Material`) | 비면 원본 복사 | 외곽선 렌더러 머티리얼. `My/SpriteSilhouette` = 단색 실루엣 |
| `color` (`Color`) | `(1, 0.85, 0.2, 1)` | 실루엣 색 |
| `scale` (float) | `1.06` | 원본 대비 확대 배율 (외곽선 두께) |
| `sortingOffset` (int) | `-1` | 원본보다 이만큼 뒤 sortingOrder |

## Interactor 연결

`CursorInteractor` / `GazeInteractor` 가 레이 히트에서 `GetComponentInParent<SpriteOutline>()` 를
`Outline` 과 나란히 잡아 `SetHighlighted` 를 토글한다. 한 오브젝트는 `Outline` **또는** `SpriteOutline`
중 하나만 쓴다 — `Interactable.EnsureOutline()` 는 `SpriteOutline` 이 있으면 QuickOutline 을 안 붙인다.

## 관련

[Interactable](Interactable.md) · [InteractionSystem](InteractionSystem.md) · [[project_quickoutline-local-patch]]
