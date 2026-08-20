# 0041 - 인벤토리 슬롯 활성화 표시(inventory_activate) 연동

## 날짜
2026-08-20

## 요청 내용 (원문)
> 인벤토리 슬롯에다가 inventory_activate라는 이미지를 넣었는데 이게 해당하는 인벤토리가 활성화 되어있다는걸 보여주는 이미지로 플레이어가 1,2,3,4,5 숫자 키 눌렀을때 활성화된 인벤토리에 이 오브젝트가 활성화 되도록 해줘 나머지 오브젝트는 비활성화되어있어야하고

## 조사 내용
- `Assets/My/Scripts/Inventory/InventorySystem.cs`가 이미 슬롯별 `slotIcons`(아이템 아이콘 `Image[]`) 배열과 `SelectSlot(int)`(숫자키 1~5로 호출)로 슬롯 선택/장착을 처리 중.
- `inventory_activate` 오브젝트는 슬롯마다 하나씩 있는 별도 표시용 오브젝트라 `slotIcons`와 같은 패턴으로 슬롯 개수(5)만큼의 배열 필드를 추가해서 인스펙터에서 슬롯별로 연결하는 방식이 기존 코드 스타일과 일치.

## 변경 내용

### `Assets/My/Scripts/Inventory/InventorySystem.cs`
```diff
     [SerializeField] private Image[] slotIcons = new Image[SlotCount];
+    [SerializeField] private GameObject[] activateIcons = new GameObject[SlotCount];
     ...
     private void Awake()
     {
         Instance = this;
+
+        foreach (GameObject activateIcon in activateIcons)
+            if (activateIcon != null)
+                activateIcon.SetActive(false);
     }
     ...
     private void SelectSlot(int index)
     {
         if (activeSlot >= 0 && equipTargets[activeSlot] != null)
             equipTargets[activeSlot].SetActive(false);
+
+        if (activeSlot >= 0 && activateIcons[activeSlot] != null)
+            activateIcons[activeSlot].SetActive(false);

         if (equipTargets[index] != null)
         {
             equipTargets[index].SetActive(true);
             activeSlot = index;
+
+            if (activateIcons[index] != null)
+                activateIcons[index].SetActive(true);
         }
         else
         {
             activeSlot = -1;
         }
     }
```
- 시작 시 모든 `activateIcons`를 비활성화, 숫자키로 슬롯을 바꿀 때마다 이전 슬롯 표시는 끄고 새 슬롯 표시만 켬 (아이템이 있는 슬롯일 때만 — 빈 슬롯 키를 누르면 기존 로직대로 선택 해제됨).

## 결과
계획대로 적용 완료.

## 남은 작업 (씬 작업, 사용자 진행)
- 인스펙터에서 `InventorySystem`의 `Activate Icons` 배열(5칸)에 각 슬롯의 `inventory_activate` 오브젝트를 순서대로(슬롯1→인덱스0 ... 슬롯5→인덱스4) 연결

## 변경된 파일
- `Assets/My/Scripts/Inventory/InventorySystem.cs`
