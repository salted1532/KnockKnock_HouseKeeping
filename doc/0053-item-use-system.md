# 0053 - 손에 든 아이템 사용(좌클릭) 시스템 - 소다 마시기부터 적용

## 요청 내용
> 소다 아이템에 경우 들고 있을떄 좌클릭 누르면 마시는거로 하자 그러면 인벤토리에서 사라지고 마시는 효과음 나오도록 할까
> 아이템 사용에 관한걸 만들고 싶은거야 나중에도 사용하게 될거야

소다 하나만을 위한 임시 코드가 아니라, 앞으로 다른 아이템(음식, 물약 등)에도 재사용할 수 있는 "손에 든 아이템 사용" 시스템을 원함.

## 조사 내용
- `InventorySystem.cs`: F키로 `ThrowActiveItem()` 호출, 슬롯별 `equipTargets`/`itemIcons`/`pickupSources`/`isFlashlightSlot` 배열로 상태 관리. `Update()`에서 키보드 입력만 처리 중 (마우스 입력 없음)
- `Interactable.cs`: `Pickup()`에서 `InventorySystem.AddItem(icon, equipTarget, pickupSource)` 호출해 손 아이템 등록. 손전등은 별도 `isFlashlight` 플래그로 표시
- 손에 들고 있는 상태의 "사용" 개념이 아직 없음 → 소다뿐 아니라 향후 아이템에도 쓸 수 있게 `AddItem`에 사용 관련 정보(효과음, 사용 시 소모 여부)를 함께 등록하는 방식으로 확장

## 계획된 변경

**`Interactable.cs`**: Pickup 아이템에 "사용" 관련 필드 추가, 등록 시 같이 넘김
```diff
     [Header("Pickup")]
     [SerializeField] private string itemName;
     [SerializeField] private Sprite itemIcon;
     [SerializeField] private GameObject equipTarget;
+    [SerializeField] private AudioClip useClip;
+    [SerializeField] private bool consumeOnUse;

     public void SetEquipTarget(GameObject target) => equipTarget = target;
...
     private void Pickup()
     {
         if (equipTarget == null)
         {
             Destroy(gameObject);
             return;
         }

-        if (InventorySystem.Instance != null && InventorySystem.Instance.AddItem(itemIcon, equipTarget, gameObject))
+        if (InventorySystem.Instance != null && InventorySystem.Instance.AddItem(itemIcon, equipTarget, gameObject, useClip: useClip, consumeOnUse: consumeOnUse))
             gameObject.SetActive(false);
     }
```

**`InventorySystem.cs`**: 좌클릭으로 든 아이템 사용, 소모 아이템이면 효과음 재생 후 인벤토리에서 제거
```diff
     [SerializeField] private Transform throwPos;
     [SerializeField] private float throwForce = 10f;
+    [SerializeField] private AudioSource audioSource;

     private readonly Sprite[] itemIcons = new Sprite[SlotCount];
     private readonly GameObject[] equipTargets = new GameObject[SlotCount];
     private readonly GameObject[] pickupSources = new GameObject[SlotCount];
     private readonly bool[] isFlashlightSlot = new bool[SlotCount];
+    private readonly AudioClip[] useClips = new AudioClip[SlotCount];
+    private readonly bool[] consumeOnUseSlot = new bool[SlotCount];
     private int activeSlot = -1;
...
         if (Keyboard.current.fKey.wasPressedThisFrame) ThrowActiveItem();
+        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) UseActiveItem();
     }

-    public bool AddItem(Sprite icon, GameObject equipTarget, GameObject pickupSource, bool isFlashlight = false)
+    public bool AddItem(Sprite icon, GameObject equipTarget, GameObject pickupSource, bool isFlashlight = false, AudioClip useClip = null, bool consumeOnUse = false)
     {
         for (int i = 0; i < SlotCount; i++)
         {
             if (equipTargets[i] != null)
                 continue;

             itemIcons[i] = icon;
             equipTargets[i] = equipTarget;
             pickupSources[i] = pickupSource;
             isFlashlightSlot[i] = isFlashlight;
+            useClips[i] = useClip;
+            consumeOnUseSlot[i] = consumeOnUse;
             equipTarget.SetActive(i == activeSlot);
             ...
         }
     }

+    private void UseActiveItem()
+    {
+        if (activeSlot < 0 || equipTargets[activeSlot] == null || useClips[activeSlot] == null)
+            return;
+
+        if (audioSource != null)
+            audioSource.PlayOneShot(useClips[activeSlot]);
+
+        if (consumeOnUseSlot[activeSlot])
+        {
+            if (pickupSources[activeSlot] != null)
+                Destroy(pickupSources[activeSlot]);
+            equipTargets[activeSlot].SetActive(false);
+            ClearSlot(activeSlot);
+        }
+    }

     private void ThrowActiveItem()
     {
         ...
-        equipTargets[activeSlot] = null;
-        itemIcons[activeSlot] = null;
-        pickupSources[activeSlot] = null;
-        isFlashlightSlot[activeSlot] = false;
-        if (slotIcons[activeSlot] != null)
-        {
-            slotIcons[activeSlot].sprite = null;
-            slotIcons[activeSlot].color = EmptySlotColor;
-        }
-        UpdateFlashlightHint();
+        ClearSlot(activeSlot);
     }
+
+    private void ClearSlot(int index)
+    {
+        equipTargets[index] = null;
+        itemIcons[index] = null;
+        pickupSources[index] = null;
+        isFlashlightSlot[index] = false;
+        useClips[index] = null;
+        consumeOnUseSlot[index] = false;
+        if (slotIcons[index] != null)
+        {
+            slotIcons[index].sprite = null;
+            slotIcons[index].color = EmptySlotColor;
+        }
+        UpdateFlashlightHint();
+    }
```

`ThrowActiveItem()`과 `UseActiveItem()`이 공통으로 쓰는 "슬롯 비우기" 로직은 `ClearSlot()`으로 묶음 (0052에서 뺀 `activateIcons` 끄기는 그대로 포함 안 함 — 포커스 유지).

`useClip`이 비어있으면(`null`) 좌클릭은 그냥 무시됨 — 손전등처럼 사용 개념이 없는 아이템은 아무 설정도 안 해도 됨.

## 사용자가 씬/에셋에서 직접 해야 하는 일
1. `InventorySystem`(인벤토리 UI 오브젝트)에 새로 생긴 **Audio Source** 필드에 소리 낼 `AudioSource` 컴포넌트 연결 (없으면 하나 추가해서 연결)
2. 소다 아이템(`Interactable`, Pickup 타입) 프리팹에서 새로 생긴 **Use Clip**에 마시는 효과음 연결, **Consume On Use** 체크

## 적용 결과
계획대로 적용함.

## 요약 / 남은 작업
- 변경 파일: `Assets/My/Scripts/Interaction/Interactable.cs`, `Assets/My/Scripts/Inventory/InventorySystem.cs`
- 씬/에셋 작업은 사용자가 직접 (위 1, 2번)
