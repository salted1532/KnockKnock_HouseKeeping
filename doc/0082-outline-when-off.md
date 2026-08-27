# 0082 - OutlineWhenOff (조명 스위치 상시 외곽선)

## 요청
`light_switch`(켜고끄기, 방 조명 on/off). 불이 꺼지면 어두워서 스위치가 안 보임 → 꺼져 있을 때(off)는 Outline 이 상시 활성화되도록.

## 방식
`InteractionEffect` 가 아니라 지속 동작(매 프레임 유지)이 필요 → 별도 컴포넌트. "큰 틀 밖 특수 작동은 별도 스크립트" 원칙.

## 신규: `Assets/My/Scripts/Interaction/OutlineWhenOff.cs`
```
[RequireComponent(typeof(Interactable), typeof(Outline))]
LateUpdate: if (!interactable.IsOn && !outline.enabled) outline.enabled = true;
```
- `LateUpdate` 라 `GazeInteractor`/`CursorInteractor` 의 `Update`(시선 떼면 Outline off) 뒤에 실행 → 되살림.
- `IsOn`(켜짐)일 때는 간섭 안 함 → 호버 아웃라인은 Interactor 가 평소대로.
- 우클릭 "재설정" 은 자동 부착 안 함 (특정 오브젝트만) — 수동 추가.

## 배치
`light_switch`: `Interactable`(켜고끄기) + `ChangeObjectEffect` + `SfxEffect` + **`OutlineWhenOff`**. `startOn` = 방 초기 밝기.

## 상태
2026-08-27 완료. `Docs/OutlineWhenOff.md`, `Docs/Overview.md` 추가.
