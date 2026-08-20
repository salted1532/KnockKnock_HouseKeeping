# 0043 - 인벤토리 초기 슬롯1 활성화 + 빈 슬롯도 포커스 표시

## 날짜
2026-08-20

## 요청 내용 (원문)
> 처음엔 1번 슬롯에 활성화 되어있도록 해주고 빈 인벤토리라도 활성화 된건 표시되도록해줘
> (이어서) 현재 포커스가 어디있는 알기 편하도록

[[0042-inventory-activate-icons-wiring]]의 후속 — 별도 제안서 없이 바로 진행.

## 조사 내용
- 기존 `SelectSlot(int)`은 `equipTargets[index]`(해당 슬롯에 아이템이 있는지)가 `null`이면 `activeSlot = -1`로 되돌리고 `activateIcons`도 켜지 않았음 → 빈 슬롯을 선택하면 "포커스"가 아예 사라져 어느 슬롯을 보고 있는지 알 수 없었음.
- 요청은 "포커스(현재 선택된 슬롯) 표시"와 "아이템 장착 여부"를 분리해서, 빈 슬롯이어도 포커스 표시는 항상 나오게 해달라는 것.

## 변경 내용

### `Assets/My/Scripts/Inventory/InventorySystem.cs`
```diff
+    private void Start()
+    {
+        SelectSlot(0);
+    }
+
     private void Update()
     ...
     private void SelectSlot(int index)
     {
         if (activeSlot >= 0 && equipTargets[activeSlot] != null)
             equipTargets[activeSlot].SetActive(false);

         if (activeSlot >= 0 && activateIcons[activeSlot] != null)
             activateIcons[activeSlot].SetActive(false);

-        if (equipTargets[index] != null)
-        {
-            equipTargets[index].SetActive(true);
-            activeSlot = index;
-
-            if (activateIcons[index] != null)
-                activateIcons[index].SetActive(true);
-        }
-        else
-        {
-            activeSlot = -1;
-        }
+        activeSlot = index;
+
+        if (equipTargets[index] != null)
+            equipTargets[index].SetActive(true);
+
+        if (activateIcons[index] != null)
+            activateIcons[index].SetActive(true);
     }
```
- `activeSlot`/포커스 표시(`activateIcons`)는 아이템 유무와 무관하게 항상 선택한 슬롯을 따라감. 실제 장비(`equipTargets`) 활성화는 아이템이 있을 때만 동작(기존과 동일).
- 게임 시작 시 `Start()`에서 `SelectSlot(0)`을 호출해 1번 슬롯이 기본으로 포커스 표시되도록 함.

## 결과
계획대로 적용 완료.

## 변경된 파일
- `Assets/My/Scripts/Inventory/InventorySystem.cs`
