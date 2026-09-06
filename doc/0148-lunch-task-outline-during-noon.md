# 0148 – 점심 할일 오브젝트 외곽선 (Noon 시간대)

## 요청
점심 할일 오브젝트들이 점심(Noon) 시간대에 외곽선이 켜지도록 — 전등스위치처럼. 프리팹에 추가해서 적용.

## 현황
점심 할일 오브젝트 4개 (`Fence`, `Fence (1)`, `illegal_parking`, `illegal_parking (1)`)는
`Interactable` + `PhaseCondition[Noon]` + `LunchTaskTarget` + `Outline`(꺼짐, mode=1 OutlineVisible, 흰색, width 2)를
이미 갖고 있었으나 `Outline.enabled`를 켜주는 구동 스크립트가 없었음.

`Interactable.CanInteract`가 이미 Noon 시간대 + 미완료(`LunchTaskTarget.IsMet`) 둘 다 판정.

## 작업 (신규 코드 0줄)
기존 컴포넌트 `OutlineWhileInteractable`(`CanInteract`인 동안 `Outline.enabled` 유지)를 프리팹 2개에 추가:
- `Assets/My/InGame/Prefabs/Item/Fence.prefab`
- `Assets/My/InGame/Prefabs/Item/illegal_parking.prefab`

`PrefabUtility.LoadPrefabContents` → `AddComponent<OutlineWhileInteractable>()` → `SaveAsPrefabAsset`.
`Outline` 설정은 그대로 (전등스위치와 동일한 OutlineVisible / width 2). 씬 인스턴스 4개 모두 상속 확인.

## 결과
- 수정: `Fence.prefab`, `illegal_parking.prefab`, `InGame.unity`
- 완료 시 `CanInteract` false → 외곽선 자동 꺼짐. 다음 날 아침 `ResetForNewDay`로 부활.
