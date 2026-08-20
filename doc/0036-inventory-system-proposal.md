# 0036 - 인벤토리 시스템 구현

## 날짜
2026-08-20

## 요청 내용 (원문)
> 이제 간단한 인벤토리 시스템을 구현할건데 현재 canvas에는 inventory라는 horizontal layout Group이 있고 거기엔 slot이 1부터 5까지 있어
> 만약 손전등을 획득하게 되면 손전등 아이템 아이콘이 슬롯1에 이미지로 출력되도록 해줘
> 그리고 키보드 1,2,3,4,5로 해당 인벤토리를 선택하여 활성화 할수 있도록 해줘
> 만약 1번에 손전등이 들어가있는데 1번 누르면 플레이어 손의 손전등이 활성화 되고
> 나머지 2,3,4,5를 누르면 빈 인벤토리기 떄문에 비활성화 되도록 해줘
> 인벤토리는 5칸이 최대로 할거니깐 배열을 만들어서 각 획득형 아이템들을 저장할수 있도록 하고
> 플레이어가 1,2,3,4,5 숫자 키를 눌러서 해당하는 칸을 활성화하면 아이템을 불러와 해당하는 아이템이 활성화 되도록 하는거야 이건 이 게임 내내 사용될 거니깐 제대로된 시스템을 구현해줘

## 조사 내용
- 씬(`Assets/Scenes/InGame.unity`)에서 `inventory`(HorizontalLayoutGroup) 밑에 `inventory_slot1`~`inventory_slot5` 확인. 각 슬롯 GameObject에 **`Image` 컴포넌트가 직접 붙어있고** (`m_Sprite: {fileID: 0}`, 현재 빈 아이콘), 별도 자식 아이콘 오브젝트는 없음 → 슬롯 자체의 `Image.sprite`를 갈아끼우는 방식으로 아이콘 표시 가능.
- `Assets/My/Scripts/Interaction/Interactable.cs`(사용자가 직전 세션에서 추가한 스크립트)의 `Pickup()`이 현재는 `equipTarget.SetActive(true)`로 **즉시 손에 장착**시키고 원본을 비활성화하는 구조 → 이제는 즉시 장착이 아니라 **인벤토리에 등록만 하고, 숫자키로 선택해야 장착**되도록 바뀌어야 함.
- `Interactable`에는 아이콘 스프라이트 필드가 없어서 하나 추가 필요 (`itemIcon`).
- 씬에 손전등을 실제로 들고 다닐 "손 장착용" 오브젝트(`flashlight.prefab`, `Flashlight.cs` 사용)는 아직 배치되어 있지 않음(습득용 `FlashLight_low-Poly` 프롭만 있음) → 이번 작업은 범용 인벤토리 시스템 코드까지만 구현하고, 손전등을 손 장착용으로 씬에 배치해서 `equipTarget`에 연결하는 건 사용자가 진행.
- 입력은 기존 `InteractionOutline.cs`와 동일하게 새 Input System(`Keyboard.current`) 사용.

## 계획

### 1. `Assets/My/Scripts/Inventory/InventorySystem.cs` (신규)
```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    private const int SlotCount = 5;

    [SerializeField] private Image[] slotIcons = new Image[SlotCount];

    private readonly Sprite[] itemIcons = new Sprite[SlotCount];
    private readonly GameObject[] equipTargets = new GameObject[SlotCount];
    private int activeSlot = -1;

    private void Awake()
    {
        Instance = this;

        foreach (var icon in slotIcons)
            if (icon != null)
                icon.enabled = false;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
        else if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
    }

    public bool AddItem(Sprite icon, GameObject equipTarget)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (equipTargets[i] != null)
                continue;

            itemIcons[i] = icon;
            equipTargets[i] = equipTarget;
            equipTarget.SetActive(false);

            if (slotIcons[i] != null)
            {
                slotIcons[i].sprite = icon;
                slotIcons[i].enabled = icon != null;
            }
            return true;
        }
        return false;
    }

    private void SelectSlot(int index)
    {
        if (activeSlot >= 0 && equipTargets[activeSlot] != null)
            equipTargets[activeSlot].SetActive(false);

        if (equipTargets[index] != null)
        {
            equipTargets[index].SetActive(true);
            activeSlot = index;
        }
        else
        {
            activeSlot = -1;
        }
    }
}
```
- `itemIcons`/`equipTargets` 배열(고정 5칸)에 획득한 아이템을 저장. `AddItem`은 빈 칸을 찾아 채우고 해당 슬롯 UI의 `Image.sprite`를 갱신.
- 숫자키 1~5 → `SelectSlot(0~4)`: 기존에 활성화되어 있던 손 오브젝트를 끄고, 눌린 슬롯에 아이템이 있으면 그 손 오브젝트를 켬. 빈 슬롯이면 아무것도 켜지지 않음(요청하신 "빈 인벤토리면 비활성화").

### 2. `Assets/My/Scripts/Interaction/Interactable.cs` 수정
```diff
     [Header("Pickup")]
     [SerializeField] private string itemName;
+    [SerializeField] private Sprite itemIcon;
     [SerializeField] private GameObject equipTarget;
@@
     private void Pickup()
     {
-        Debug.Log($"Picked up {itemName}");
-
-        if (equipTarget != null)
+        if (equipTarget == null)
         {
-            equipTarget.SetActive(true);
-            gameObject.SetActive(false);
+            Destroy(gameObject);
+            return;
         }
-        else
-        {
-            Destroy(gameObject);
-        }
+
+        if (InventorySystem.Instance != null && InventorySystem.Instance.AddItem(itemIcon, equipTarget))
+            gameObject.SetActive(false);
     }
```
- `equipTarget`이 없는 아이템(단순 수집용)은 기존처럼 그냥 파괴.
- `equipTarget`이 있는 아이템은 즉시 장착하지 않고 인벤토리에 등록 → 인벤토리가 꽉 차서 등록 실패하면 픽업되지 않고(오브젝트가 그대로 남음) 재시도 가능.

## 결과
승인 후 적용. 단, 구현 시점에 `Interactable.cs`가 계획 작성 이후 사용자가 직접 추가한 내용으로 변경되어 있어 계획과 다르게 적용됨:
- `InteractionType.Flashlight` + `ActivatePlayerFlashlight()`가 이미 추가되어 있었음 (`PlayerCameraRoot/flashlight`를 하드코딩된 경로로 찾아 `SetActive(true)`로 즉시 장착, `HowToUse_Flashlight` 힌트 텍스트 표시).
- 이 즉시-장착 로직이 "숫자키로 선택해야 장착" 요구사항과 충돌하여, `ActivatePlayerFlashlight()`도 `equipTarget.SetActive(true)` 대신 `InventorySystem.Instance.AddItem(itemIcon, flashlight.gameObject)`를 호출하도록 수정. 힌트 텍스트 표시 로직은 그대로 유지(등록 성공 시에만 표시).
- `Pickup()`은 계획대로 적용됨.

```diff
     private void ActivatePlayerFlashlight()
     {
         GameObject player = GameObject.FindGameObjectWithTag("Player");
         Transform flashlight = player != null ? player.transform.Find("PlayerCameraRoot/flashlight") : null;
-        if (flashlight != null)
-            flashlight.gameObject.SetActive(true);
+
+        if (flashlight == null || InventorySystem.Instance == null || !InventorySystem.Instance.AddItem(itemIcon, flashlight.gameObject))
+            return;

         GameObject canvas = GameObject.Find("Canvas");
         Transform hint = canvas != null ? canvas.transform.Find("HowToUse_Flashlight") : null;
         if (hint != null)
             hint.gameObject.SetActive(true);

         gameObject.SetActive(false);
     }
```

## 남은 작업 (씬 작업, 사용자 진행)
1. Canvas 밑 아무 곳에 빈 오브젝트를 만들어 `InventorySystem` 컴포넌트 부착, `Slot Icons` 배열(크기 5)에 `inventory_slot1`~`inventory_slot5`의 `Image` 컴포넌트를 순서대로 연결.
2. 손전등을 손에 드는 용도의 오브젝트(`Assets/AssetsFolder/Flashlight/Flashlight/Prefab/flashlight.prefab`)를 플레이어 카메라/손 위치 밑에 배치 (평소엔 꺼둬도 됨 — `AddItem`이 자동으로 꺼줌).
3. `FlashLight_low-Poly`(습득용 프롭)의 `Interactable` 컴포넌트에서 `Item Icon`(손전등 아이콘 스프라이트)과 `Equip Target`(위에서 배치한 손전등 오브젝트)을 연결.

## 변경된 파일
- `Assets/My/Scripts/Inventory/InventorySystem.cs` (신규) — 5칸 배열 기반 인벤토리, 숫자키 1~5로 슬롯 선택/장착
- `Assets/My/Scripts/Interaction/Interactable.cs` — Pickup 시 즉시 장착 대신 인벤토리에 등록하도록 수정, `itemIcon` 필드 추가
