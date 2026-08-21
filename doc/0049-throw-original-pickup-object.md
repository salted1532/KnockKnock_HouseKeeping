# 0049 - F키로 던지는 대상을 손 모델이 아니라 원본 pickup 오브젝트로 변경

## 요청 내용
> 플레이어 손에 들고 있는 아이템을 버리는게 아니라
> 플레이어가 상호작용 했던 pickup 아이템 있잖아 그걸 가져와서 던지라는거야
> 그리고 플레이어의 아이템은 비활성화 하고

0048에서 만든 `ThrowActiveItem()`은 `equipTargets[activeSlot]`(손에 들린 모델)을 그대로 던지고 있었음 — 요청은 그게 아니라, 원래 월드에 있다가 `Interactable.Pickup()`/`ActivatePlayerFlashlight()`에서 `gameObject.SetActive(false)`로 숨겨진 **원본 오브젝트**를 다시 꺼내서 던지고, 손 모델(`equipTarget`)은 비활성화만 하라는 것.

## 조사 내용
현재(다른 세션에서 이미 진행된) `InventorySystem.cs`/`Interactable.cs` 상태 확인함 — 손전등 힌트 UI가 `Interactable`에서 `InventorySystem.UpdateFlashlightHint()`로 옮겨졌고, `isFlashlightSlot` 배열이 추가되어 있음. 이번 변경은 이 구조 위에 그대로 얹음.

`Interactable.Pickup()`/`ActivatePlayerFlashlight()`는 `AddItem()`에 손 모델(`equipTarget`)만 넘기고, 자기 자신(`gameObject`, 즉 원본 pickup 오브젝트)은 넘기지 않아서 `InventorySystem`이 그 참조를 모름. → `AddItem`에 원본 오브젝트를 넘기는 파라미터 추가 필요.

## 계획된 변경

**`InventorySystem.cs`**
```diff
     private readonly Sprite[] itemIcons = new Sprite[SlotCount];
     private readonly GameObject[] equipTargets = new GameObject[SlotCount];
+    private readonly GameObject[] pickupSources = new GameObject[SlotCount];
     private readonly bool[] isFlashlightSlot = new bool[SlotCount];
```
```diff
-    public bool AddItem(Sprite icon, GameObject equipTarget, bool isFlashlight = false)
+    public bool AddItem(Sprite icon, GameObject equipTarget, GameObject pickupSource, bool isFlashlight = false)
     {
         for (int i = 0; i < SlotCount; i++)
         {
             if (equipTargets[i] != null)
                 continue;

             itemIcons[i] = icon;
             equipTargets[i] = equipTarget;
+            pickupSources[i] = pickupSource;
             isFlashlightSlot[i] = isFlashlight;
```
```diff
     private void ThrowActiveItem()
     {
         if (activeSlot < 0 || equipTargets[activeSlot] == null || throwPos == null)
             return;

-        GameObject item = equipTargets[activeSlot];
+        GameObject heldItem = equipTargets[activeSlot];
+        GameObject thrownItem = pickupSources[activeSlot];

-        item.transform.SetParent(null);
-        item.transform.SetPositionAndRotation(throwPos.position, throwPos.rotation);
+        heldItem.SetActive(false);
 
-        Rigidbody rb = item.GetComponent<Rigidbody>();
-        if (rb == null)
-            rb = item.AddComponent<Rigidbody>();
-        rb.AddForce(throwPos.forward * throwForce, ForceMode.Impulse);
+        if (thrownItem != null)
+        {
+            thrownItem.SetActive(true);
+            thrownItem.transform.SetParent(null);
+            thrownItem.transform.SetPositionAndRotation(throwPos.position, throwPos.rotation);
+
+            Rigidbody rb = thrownItem.GetComponent<Rigidbody>();
+            if (rb == null)
+                rb = thrownItem.AddComponent<Rigidbody>();
+            rb.AddForce(throwPos.forward * throwForce, ForceMode.Impulse);
+        }

         equipTargets[activeSlot] = null;
         itemIcons[activeSlot] = null;
+        pickupSources[activeSlot] = null;
         isFlashlightSlot[activeSlot] = false;
```

**`Interactable.cs`** (두 호출부에 자기 자신을 pickupSource로 전달)
```diff
-        if (InventorySystem.Instance != null && InventorySystem.Instance.AddItem(itemIcon, equipTarget))
+        if (InventorySystem.Instance != null && InventorySystem.Instance.AddItem(itemIcon, equipTarget, gameObject))
             gameObject.SetActive(false);
```
```diff
-        if (flashlight == null || InventorySystem.Instance == null || !InventorySystem.Instance.AddItem(itemIcon, flashlight.gameObject, isFlashlight: true))
+        if (flashlight == null || InventorySystem.Instance == null || !InventorySystem.Instance.AddItem(itemIcon, flashlight.gameObject, gameObject, isFlashlight: true))
             return;
```

## 참고
- 원본 오브젝트는 `Interactable`/`Outline`/`Collider`가 그대로 붙어있는 채로 다시 활성화되므로, 던진 뒤 바닥에 떨어지면 다시 주울 수 있음 (별도 처리 없이 자연스럽게 따라오는 부수 효과)
- Rigidbody 자동 추가 로직은 대상만 바뀌었을 뿐 0048과 동일

## 적용 결과
계획대로 적용함.

## 요약 / 남은 작업
- 변경 파일: `Assets/My/Scripts/Inventory/InventorySystem.cs`, `Assets/My/Scripts/Interaction/Interactable.cs`
- 씬 변경 없음 (ThrowPos는 0048에서 이미 배치됨)
- Unity 에디터 미실행 테스트 — 실제 동작 확인 필요
