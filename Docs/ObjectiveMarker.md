# ObjectiveMarker

`Assets/My/Scripts/UI/ObjectiveMarker.cs` · `Assets/My/Scripts/Interaction/OutlineWhileInteractable.cs` · `Assets/My/Scripts/UI/ExitHintGauge.cs`

"여기로 가라" 유도 표식 3종 ([`doc/0144`](../doc/0144-morning-todo-list-and-bed-tidy-reset.md)). 하루 진행 트리거(게시판/접객 테이블/주인방 침대)가 상호작용 가능해지는 순간을 플레이어에게 알린다.

## ObjectiveMarker

`Interactable.CanInteract` 인 동안 오브젝트 위에 색 다이아몬드 마커를 **HUD Canvas(ScreenSpaceOverlay)** 에 그린다 — 3D 외곽선과 달리 PxlCrush(RawImage) 팔레트 크러시를 안 타서 색이 그대로 나온다 (`doc/0144` 에서 이 이유로 벽너머 외곽선 비콘 대신 채택).

`[RequireComponent(Interactable)]`. `Start` 에서 마커 `GameObject` 를 코드로 생성, `OnDestroy` 에서 파괴.

| 필드 | 기본 | 설명 |
|---|---|---|
| `color` (`Color`) | yellow | 마커 색 (오브젝트별) |
| `hudParent` (`RectTransform`) | 자동 | 마커가 붙을 ScreenSpaceOverlay Canvas. 비우면 루트 Overlay Canvas 자동 탐색 |
| `worldAnchor` (`Transform`) | — | 마커가 가리킬 지점. 비우면 오브젝트 원점 + `worldYOffset` |
| `worldYOffset` (`float`) | 0.5 | |
| `size` / `edgePadding` (`float`) | 22 / 40 | 마커 크기 / 화면 밖 클램프 여백 |
| `messageEn` / `messageKo` | "" | `CanInteract` false→true 엣지에서 [`ScreenMessage`](ScreenMessage.md) 1회 ("게시판으로 가자" 등). 비우면 안 띄움 |

- 화면 안: 대상 위치에 45° 다이아몬드. 화면 밖/뒤: 가장자리로 클램프하고 꼭짓점이 방향을 가리킴.
- `Camera.main` 을 매 프레임 캐시.

## OutlineWhileInteractable

`Interactable.CanInteract` 인 동안 `Outline.enabled = true` 를 `LateUpdate` 에서 강제 (Interactor 가 시선 떼며 끈 걸 다시 켬). [`OutlineWhenOff`](OutlineWhenOff.md) 의 조건 버전. `[RequireComponent(Interactable, Outline)]`.

- 벽 너머 표시 = `Outline` 컴포넌트 설정: `Outline Mode = OutlineAll` (ZTest Always). 색은 **흰색**이면 PxlCrush 무관(흰→흰). `Outline.enabled` 초기 false.
- `ObjectiveMarker`(방향, 색) + 이 외곽선(대상, 벽 너머) 병행 사용.

## ExitHintGauge

화면고정(접객·새벽 대화) 나가기 홀드 게이지. `Image.fillAmount = UIInteractionMode.Instance.ExitProgress` (0~1). `UIInteractionMode.exitHint` 오브젝트(또는 자식)의 Filled `Image` 에 붙인다. `escExits` 뷰(모니터)에선 `exitHint` 자체가 숨겨진다.

## 관련

[Interactable](InteractionSystem.md) · [OutlineWhenOff](OutlineWhenOff.md) · [ScreenMessage](ScreenMessage.md) · [UIInteractionMode](UIInteractionMode.md) · [MorningTasks](MorningTasks.md) · [`doc/0144`](../doc/0144-morning-todo-list-and-bed-tidy-reset.md)
