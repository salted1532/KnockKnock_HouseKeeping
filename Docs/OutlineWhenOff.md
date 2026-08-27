# OutlineWhenOff

`Assets/My/Scripts/Interaction/OutlineWhenOff.cs`

토글 상호작용이 **off 상태(`Interactable.IsOn == false`)일 때 `Outline` 을 항상 켜둔다.**
어두운 방의 조명 스위치처럼, 꺼놓으면 잘 안 보여서 다시 찾기 힘든 오브젝트에 붙인다.

`[RequireComponent(typeof(Interactable), typeof(Outline))]`. `InteractionEffect` 가 아니라 지속 동작이 필요해 별도 스크립트 ("큰 틀 밖 특수 작동").

## 동작

- `LateUpdate`: `!IsOn && !outline.enabled` 면 `outline.enabled = true`.
  - `LateUpdate` 라 `GazeInteractor`/`CursorInteractor` 의 `Update`(시선 떼면 Outline off) 뒤에 실행되어 되살림.
- `IsOn`(켜짐) 상태에서는 아무것도 안 함 → 호버 아웃라인은 Interactor 가 평소대로 관리.

## 배치

조명 스위치: `Interactable`(켜고끄기) + `ChangeObjectEffect`(불 켜짐/꺼짐 오브젝트) + `SfxEffect` 에 더해 **`OutlineWhenOff` 추가**.
`Interactable.startOn` = 방이 처음부터 밝으면 on, 어두우면 off.

우클릭 "재설정" 은 이 컴포넌트를 자동으로 붙이지 않음 (조명 스위치 등 특정 오브젝트에만 필요) — 수동 추가.

## 관련
[Interactable](Interactable.md) · [ChangeObjectEffect](ChangeObjectEffect.md) · [GazeInteractor](GazeInteractor.md)
