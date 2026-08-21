# 0052 - 아이템 버릴 때 슬롯 포커스는 유지

## 요청 내용
> 아이템을 버렸을때 인벤토리 활성화까지 비활성화 해주진 말고
> 같은 슬롯을 포커스 하고 있는데 해당 슬롯에 있는 아이템을 버렸기 때문에 아무것도 안들고 있는거로 해줘

## 조사 내용
`InventorySystem.cs`의 `ThrowActiveItem()` 확인:
- 아이템/아이콘/장비 정보는 올바르게 비움 (`equipTargets`, `itemIcons`, `pickupSources`, `isFlashlightSlot`, `slotIcons`)
- 그런데 마지막에 `activateIcons[activeSlot].SetActive(false)`를 호출해서, "이 슬롯이 선택되어 있음"을 나타내는 포커스 표시(`activateIcons`)까지 꺼버림
- 슬롯은 여전히 활성 슬롯(`activeSlot`)인 채로 남아있으므로, 포커스 표시는 계속 켜져 있어야 하고 단지 들고 있는 아이템만 없어진(빈 슬롯) 상태가 되어야 함

## 계획된 변경
**`Assets/My/Scripts/Inventory/InventorySystem.cs`** — `ThrowActiveItem()`에서 포커스 아이콘을 끄는 부분 제거

```diff
         if (slotIcons[activeSlot] != null)
         {
             slotIcons[activeSlot].sprite = null;
             slotIcons[activeSlot].color = EmptySlotColor;
         }
-        if (activateIcons[activeSlot] != null)
-            activateIcons[activeSlot].SetActive(false);

         UpdateFlashlightHint();
```

## 적용 결과
계획대로 적용함.

## 요약 / 남은 작업
- 변경 파일: `Assets/My/Scripts/Inventory/InventorySystem.cs`
