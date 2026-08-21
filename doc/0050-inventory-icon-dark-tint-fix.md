# 0050 - 인벤토리 아이콘이 원본보다 어둡게 보이는 문제 수정

## 요청 내용
> 인벤토리로 들어간 이미지가 원본이미지보다 어둡게 나오는거 같은데 왜그런거야?
> (원인 설명 후) 고쳐줘

## 조사 내용
`Assets/Scenes/InGame.unity`의 슬롯 5개(`inventory_slot1`~`5`, Image 컴포넌트: `346387133`, `212619698`, `1122948444`, `1676934086`, `2060213167`)가 전부 동일하게
```
m_Color: {r: 0.32156864, g: 0.32156864, b: 0.32156864, a: 1}
```
빈 슬롯을 어둡게 보여주려는 기본 틴트로 세팅돼있음. `InventorySystem.AddItem()`이 `.sprite`만 바꾸고 `.color`는 그대로 둬서, 아이템이 들어와도 이 회색 틴트가 곱해져 어둡게 보임.

## 적용한 변경
`Assets/My/Scripts/Inventory/InventorySystem.cs`
- `EmptySlotColor` 상수 추가 (씬에 이미 박혀있던 값과 동일: `0.32156864, 0.32156864, 0.32156864, 1`)
- `AddItem()`: 아이콘 넣을 때 `slotIcons[i].color = Color.white`로 리셋
- `ThrowActiveItem()`: 슬롯 비울 때 `slotIcons[activeSlot].color = EmptySlotColor`로 복원 (빈 슬롯 어둡게 표시하는 기존 룩 유지)

## 요약 / 남은 작업
- 변경 파일: `Assets/My/Scripts/Inventory/InventorySystem.cs`
- Unity 에디터 미실행 테스트 — 실제로 밝아지는지 확인 필요
