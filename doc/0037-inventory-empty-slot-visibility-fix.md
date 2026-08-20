# 0037 - 빈 인벤토리 슬롯이 안 보이던 문제 수정

## 날짜
2026-08-20

## 요청 내용 (원문)
> 나머지 빈 슬롯은 그대로 보이도록 해줘

## 조사 내용
- `inventory_slot1`~`5`는 별도 배경 이미지 없이 슬롯 GameObject 자체에 `Image` 컴포넌트 하나만 있음(0036에서 확인). 스프라이트가 없어도(`None`) 기본 흰색 사각형으로 렌더링되어 "빈 슬롯 박스"처럼 보이는 게 원래 모습.
- `InventorySystem.Awake()`에서 모든 슬롯의 `Image.enabled`를 `false`로 꺼버리고, `AddItem()`에서 아이템이 채워진 슬롯만 `enabled = true`로 켜는 방식이었음 → 결과적으로 아이템이 없는 나머지 슬롯은 컴포넌트 자체가 꺼져서 아예 안 보였음.

## 계획

### `Assets/My/Scripts/Inventory/InventorySystem.cs` 수정
```diff
     private void Awake()
     {
         Instance = this;
-
-        foreach (var icon in slotIcons)
-            if (icon != null)
-                icon.enabled = false;
     }
@@
             if (slotIcons[i] != null)
-            {
                 slotIcons[i].sprite = icon;
-                slotIcons[i].enabled = icon != null;
-            }
```
- 슬롯 `Image`는 항상 켜진 채로 두고, 스프라이트만 갈아끼우는 방식으로 변경 → 빈 슬롯은 원래 모습(스프라이트 없는 사각형)대로 계속 보임, 아이템이 들어간 슬롯만 아이콘 스프라이트가 표시됨.

## 결과
승인 후 계획대로 적용 완료.

## 변경된 파일
- `Assets/My/Scripts/Inventory/InventorySystem.cs` — 슬롯 `Image`를 더 이상 껐다 켜지 않고 스프라이트만 갱신하도록 수정
