# 0056 - 마우스 휠로 인벤토리 슬롯 전환

## 요청 내용
> 마우스 휠을 통해서 인벤토리 이동이 가능하도록 해줘
> 이떄 주의해야할건 손전등 아이템은 휠클릭으로 사용하게 되는데 사용중일떈 인벤토리 이동이 안되고 휠 클릭을 눌러 미사용일때 인벤토리가 전환되도록 1 ~ 5까지 가는데 5에선 다시 1로 돌아가도록

## 조사 내용
- `InventorySystem.cs`: `Update()`에서 숫자키 1~5로 `SelectSlot(index)` 직접 호출. 마우스 휠 입력은 아직 처리 안 함
- `Flashlight.cs` (`Assets/AssetsFolder/Flashlight/Flashlight/Scripts/Flashlight.cs`, `Game.PlayerHandItem` 네임스페이스): 휠 **클릭**(가운데 버튼)으로 `IsOpen` 토글. `IsOpen == true`인 동안 휠 **스크롤**은 이미 손전등 자체의 조사각/거리 줌 조절(`UpdateLightByScroll`)에 사용 중 → 이 상태에서 휠 스크롤이 인벤토리도 같이 바꾸면 충돌함
- 손전등 슬롯 판별은 `isFlashlightSlot[]` 배열로 이미 존재. 다만 그 슬롯의 `Flashlight` 컴포넌트 참조는 저장 안 하고 있어서 `IsOpen` 상태를 알 수 없음 → 참조 캐싱 배열 추가 필요
- `Interactable.ActivatePlayerFlashlight()`에서 `equipTarget`으로 넘기는 오브젝트가 `PlayerCameraRoot/flashlight`이며, `Flashlight` 컴포넌트가 붙은 오브젝트는 그 프리팹 내부(중첩 프리팹)라 `GetComponentInChildren<Flashlight>(true)`로 찾아야 함 (동일 오브젝트여도 안전)
- 두 스크립트 모두 별도 asmdef 없이 `Assembly-CSharp`에 같이 컴파일되므로 `Game.PlayerHandItem` 참조 가능

## 계획된 변경

**`InventorySystem.cs`**
```diff
 using UnityEngine;
 using UnityEngine.InputSystem;
 using UnityEngine.UI;
+using Game.PlayerHandItem;

     private readonly bool[] isFlashlightSlot = new bool[SlotCount];
     private readonly AudioClip[] useClips = new AudioClip[SlotCount];
     private readonly bool[] consumeOnUseSlot = new bool[SlotCount];
+    private readonly Flashlight[] flashlightRefs = new Flashlight[SlotCount];
     private int activeSlot = -1;
```
```diff
         if (Keyboard.current.fKey.wasPressedThisFrame) ThrowActiveItem();
         if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) UseActiveItem();
+
+        if (Mouse.current != null)
+        {
+            float scroll = Mouse.current.scroll.ReadValue().y;
+            if (scroll != 0f && !IsActiveFlashlightOn())
+                SelectSlot((activeSlot + (scroll > 0f ? 1 : SlotCount - 1)) % SlotCount);
+        }
     }
```
```diff
             isFlashlightSlot[i] = isFlashlight;
             useClips[i] = useClip;
             consumeOnUseSlot[i] = consumeOnUse;
+            flashlightRefs[i] = isFlashlight ? equipTarget.GetComponentInChildren<Flashlight>(true) : null;
             equipTarget.SetActive(i == activeSlot);
```
```diff
         isFlashlightSlot[index] = false;
         useClips[index] = null;
         consumeOnUseSlot[index] = false;
+        flashlightRefs[index] = null;
```
```diff
+    private bool IsActiveFlashlightOn() =>
+        activeSlot >= 0 && isFlashlightSlot[activeSlot] &&
+        flashlightRefs[activeSlot] != null && flashlightRefs[activeSlot].IsOpen;
```

## 동작 요약
- 휠을 위/아래로 굴리면 활성 슬롯이 1→2→3→4→5→1 (또는 반대 방향)로 순환 전환
- 단, 현재 든 아이템이 손전등이고 **켜져 있는 상태**(`IsOpen == true`)면 휠 스크롤은 무시 (손전등 자체 줌 조작에 그대로 사용됨)
- 손전등이 꺼져 있으면(가운데 클릭으로 다시 끈 상태) 휠 스크롤로 인벤토리 전환 가능

## 사용자가 씬/에셋에서 직접 해야 하는 일
없음 (기존 필드/참조만 사용)

## 적용 결과
계획대로 적용함.

## 후속 수정
> 5 -> 1로 이동하는건 빼자
> 반대로도 빼주고

순환(래핑) 없이 양끝(1, 5)에서 멈추도록 변경.
```diff
         if (Mouse.current != null)
         {
             float scroll = Mouse.current.scroll.ReadValue().y;
             if (scroll != 0f && !IsActiveFlashlightOn())
-                SelectSlot((activeSlot + (scroll > 0f ? 1 : SlotCount - 1)) % SlotCount);
+            {
+                int nextSlot = Mathf.Clamp(activeSlot + (scroll > 0f ? 1 : -1), 0, SlotCount - 1);
+                if (nextSlot != activeSlot)
+                    SelectSlot(nextSlot);
+            }
         }
```

## 후속 수정 2
> 휠 로 인벤토리 인벤토리 이동 방향을 반대로 해줘

스크롤 방향-슬롯 이동 부호를 반전.
```diff
-                int nextSlot = Mathf.Clamp(activeSlot + (scroll > 0f ? 1 : -1), 0, SlotCount - 1);
+                int nextSlot = Mathf.Clamp(activeSlot + (scroll > 0f ? -1 : 1), 0, SlotCount - 1);
```

## 후속 수정 3
> 이제 다시 5 -> 1로 이동하는거 넣어줘 반대로로도

래핑을 다시 도입 (양방향: 5→1, 1→5), 반전된 스크롤 방향 그대로 유지.
```diff
-            if (scroll != 0f && !IsActiveFlashlightOn())
-            {
-                int nextSlot = Mathf.Clamp(activeSlot + (scroll > 0f ? -1 : 1), 0, SlotCount - 1);
-                if (nextSlot != activeSlot)
-                    SelectSlot(nextSlot);
-            }
+            if (scroll != 0f && !IsActiveFlashlightOn())
+                SelectSlot((activeSlot + (scroll > 0f ? SlotCount - 1 : 1)) % SlotCount);
```

## 요약 / 남은 작업
- 변경 파일: `Assets/My/Scripts/Inventory/InventorySystem.cs`
- 씬/에셋 작업 없음
