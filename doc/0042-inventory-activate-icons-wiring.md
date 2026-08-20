# 0042 - 인벤토리 슬롯별 inventory_activate 오브젝트 연결

## 날짜
2026-08-20

## 요청 내용 (원문)
> 각 슬롯 별로 inventory_activate가 있으니깐 너가 해당하는 곳에 연결해줘

[[0041-inventory-slot-activate-indicator]]의 후속 씬 연결 작업.

## 조사 내용
- `InventorySystem`과 슬롯/인디케이터 오브젝트들이 모두 일반 씬 오브젝트(프리팹 인스턴스 아님)라서, 이전에 겪었던 프리팹 인스턴스 간 참조가 저장 시 깨지는 문제([[0036-flashlight-runtime-lookup-fix]] 참고)와 무관 — 그냥 값 연결이라 안전하게 직접 편집 가능.
- `InventorySystem`의 기존 `slotIcons` 배열이 이미 슬롯1~5 순서로 연결되어 있어서(`inventory_slot1`~`inventory_slot5`의 Image 컴포넌트), 각 슬롯 오브젝트의 자식 중 `Inventory_number`가 아닌 `inventory_activate`를 찾아 같은 순서로 매칭:
  - 슬롯1(`inventory_slot1`) → `inventory_activate` fileID 1294399035
  - 슬롯2(`inventory_slot2`) → 2003192390
  - 슬롯3(`inventory_slot3`) → 227806465
  - 슬롯4(`inventory_slot4`) → 280345850
  - 슬롯5(`inventory_slot5`) → 1684730304

## 변경 내용

### `Assets/Scenes/InGame.unity`
`InventorySystem` 컴포넌트에 `activateIcons` 배열 추가:
```yaml
  activateIcons:
  - {fileID: 1294399035}
  - {fileID: 2003192390}
  - {fileID: 227806465}
  - {fileID: 280345850}
  - {fileID: 1684730304}
```

## 결과
계획대로 적용 완료. 1~5 숫자키로 슬롯 전환 시 해당 슬롯의 `inventory_activate`만 켜지고 나머지는 꺼짐.

## 변경된 파일
- `Assets/Scenes/InGame.unity`
