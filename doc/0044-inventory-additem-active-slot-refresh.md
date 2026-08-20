# 0044 - 아이템 획득 시 현재 활성 슬롯이면 즉시 장착 갱신

## 날짜
2026-08-20

## 요청 내용 (원문)
> 1번으로 활성화 되어있다가 손전등 획득시 바로 갱신되어 플레이어의 손전등이 활성화 되도록 해줘
> 이건 다른 아이템이 추가되었을때도 바로바로 갱신이 되도록 해야줘야해

[[0043-inventory-default-slot-and-empty-indicator]]의 후속 — 별도 제안서 없이 바로 진행.

## 조사 내용
- `InventorySystem.AddItem()`이 새 아이템을 빈 슬롯에 넣을 때마다 항상 `equipTarget.SetActive(false)`로 무조건 꺼버리고 있었음 → 그 슬롯이 마침 지금 활성화(포커스)된 슬롯이어도 장비가 안 보이고, 사용자가 해당 슬롯 숫자키를 다시 눌러야만 `SelectSlot()`을 통해 켜졌음.
- `AddItem`은 손전등 전용이 아니라 모든 줍기형 아이템이 공통으로 거치는 지점(`Interactable.Pickup()`/`ActivatePlayerFlashlight()` 둘 다 호출)이라, 여기서 고치면 요청대로 "다른 아이템 추가 시에도" 동일하게 적용됨.

## 변경 내용

### `Assets/My/Scripts/Inventory/InventorySystem.cs`
```diff
             itemIcons[i] = icon;
             equipTargets[i] = equipTarget;
-            equipTarget.SetActive(false);
+            equipTarget.SetActive(i == activeSlot);
```
- 아이템이 들어간 슬롯이 현재 활성 슬롯(`activeSlot`)과 같으면 즉시 활성화, 아니면 기존대로 꺼둔 채 보관.

## 결과
계획대로 적용 완료.

## 변경된 파일
- `Assets/My/Scripts/Inventory/InventorySystem.cs`
