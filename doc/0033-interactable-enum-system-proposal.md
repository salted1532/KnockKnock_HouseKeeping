# 0033 - enum 기반 상호작용(Interactable) 시스템 제안

## 날짜
2026-08-20

## 요청 내용 (원문)
> 이 게임에서 여러 오브젝트와 상호작용이 될거 같은데 해당하는 오브젝트별 상호작용 내용이 다 다를탠데 이를 어떻게 나누고 각각 작동하는 내용을 어떻게 코드적으로 분리 시키는게 좋을까?
> (줍기, 정리된 침대로 변경, 버튼 클릭 등)
>
> → enum 방식으로 각 물체를 추가하는 방식으로 하고 각 물건별 상호작용을 정의하는식으로 하자

## 조사 내용
- 기존에 이미 상호작용 감지/하이라이트 시스템이 구축되어 있음([[0030-outline-interaction-proposal]], [[0031-interaction-outline-camera-fix]], [[0032-quickoutline-switch]] 참고):
  - `Assets/My/Scripts/Player/InteractionOutline.cs` (PlayerCapsule에 부착) — 매 프레임 화면 중앙에서 레이캐스트, 맞은 오브젝트의 QuickOutline `Outline` 컴포넌트를 켜고 끔.
  - 상호작용 가능한 오브젝트는 `Interaction` 레이어(11번, `interactMask`)로 필터링되고 `Outline` 컴포넌트가 미리 붙어있음.
  - 아직 "키를 눌렀을 때 실제로 무언가를 실행하는" 부분은 없음 — 지금까지는 하이라이트만 구현된 상태.
- 즉 이번 작업은 그 위에 "E키를 누르면 실제 상호작용 동작을 실행"하는 레이어를 얹는 것.
- 입력 처리 패턴: 이 프로젝트는 새 Input System 사용 중이며, `SoundManager.cs`가 이미 `Keyboard.current.digit1Key.wasPressedThisFrame` 같은 저수준 폴링 방식을 쓰고 있음 → `.inputactions` 에셋을 새로 건드리지 않고 동일한 패턴으로 E키를 폴링하는 게 가장 간단하고 기존 스타일과 일치.

## 계획

### 1. `Assets/My/Scripts/Interaction/Interactable.cs` (신규)
물건마다 붙이는 컴포넌트. enum으로 상호작용 종류를 고르고, 종류별로 필요한 데이터만 인스펙터에 채우는 방식:
```csharp
using UnityEngine;
using UnityEngine.Events;

public enum InteractionType
{
    Pickup,
    TidyBed,
    Generic,
}

public class Interactable : MonoBehaviour
{
    [SerializeField] private InteractionType type;

    [Header("Pickup")]
    [SerializeField] private string itemName;

    [Header("TidyBed")]
    [SerializeField] private GameObject messyVisual;
    [SerializeField] private GameObject tidyVisual;

    [Header("Generic (버튼 등 자유 연출)")]
    [SerializeField] private UnityEvent onInteract;

    public void Interact()
    {
        switch (type)
        {
            case InteractionType.Pickup:
                Pickup();
                break;
            case InteractionType.TidyBed:
                TidyBed();
                break;
            case InteractionType.Generic:
                onInteract?.Invoke();
                break;
        }
    }

    private void Pickup()
    {
        Debug.Log($"Picked up {itemName}");
        Destroy(gameObject);
    }

    private void TidyBed()
    {
        if (messyVisual != null) messyVisual.SetActive(false);
        if (tidyVisual != null) tidyVisual.SetActive(true);
    }
}
```
- `Pickup`/`TidyBed`는 요청에 나온 예시라 전용 코드를 둠. 그 외(버튼 클릭 등 "그냥 상호작용")는 `Generic` 하나로 묶어서 인스펙터의 `UnityEvent`에 원하는 동작(문 열기, 조명 켜기 등)을 자유롭게 연결 — 매번 새 enum 값과 C# 코드를 추가하지 않아도 됨.
- 새로운 "코드로 특별히 처리해야 하는" 상호작용이 생기면 그때 enum 값과 `switch` case, private 메서드를 하나씩 추가하면 됨.
- `Pickup`은 실제 인벤토리 시스템이 아직 없어서 로그+삭제로 최소 구현. 인벤토리 붙일 때 `Pickup()` 내부만 교체하면 됨.

### 2. `Assets/My/Scripts/Player/InteractionOutline.cs` 수정
레이캐스트로 맞은 오브젝트에서 `Interactable`도 같이 찾아뒀다가, E키를 누르면 실행:
```diff
+using UnityEngine.InputSystem;
 
 public class InteractionOutline : MonoBehaviour
 {
     ...
     private Outline currentOutline;
+    private Interactable currentInteractable;
 
     private void Update()
     {
         Outline hitOutline = null;
+        Interactable hitInteractable = null;
 
         Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
         if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
         {
             hitOutline = hit.collider.GetComponentInParent<Outline>();
+            hitInteractable = hit.collider.GetComponentInParent<Interactable>();
         }
 
-        if (hitOutline == currentOutline)
-            return;
-
-        if (currentOutline != null)
-            currentOutline.enabled = false;
-        if (hitOutline != null)
-            hitOutline.enabled = true;
-
-        currentOutline = hitOutline;
+        if (hitOutline != currentOutline)
+        {
+            if (currentOutline != null)
+                currentOutline.enabled = false;
+            if (hitOutline != null)
+                hitOutline.enabled = true;
+            currentOutline = hitOutline;
+        }
+
+        currentInteractable = hitInteractable;
+
+        if (currentInteractable != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
+            currentInteractable.Interact();
     }
 }
```
- `Outline`과 `Interactable`은 보통 같은 오브젝트에 같이 붙임(하이라이트되는 물건 = 상호작용 가능한 물건).
- E키는 `SoundManager.cs`와 동일하게 `Keyboard.current` 폴링 방식 사용, `.inputactions` 에셋은 건드리지 않음.

## 남은 작업 (승인 후 씬 작업, 사용자 진행)
1. 상호작용시킬 각 오브젝트(손전등, 침대 등)에 `Interactable` 컴포넌트 추가하고 `type` 지정
   - 손전등 → `Pickup`, `itemName`에 이름 입력
   - 어질러진 침대 → `TidyBed`, `messyVisual`/`tidyVisual`에 각각의 비주얼(자식 오브젝트 등) 연결
   - 버튼/스위치 등 → `Generic`, `onInteract`에 원하는 동작 연결
2. 이미 `Interaction` 레이어 + `Outline` 컴포넌트가 붙어있는 오브젝트라면 `Interactable`만 추가로 붙이면 됨

## 변경 파일 (승인 시)
- `Assets/My/Scripts/Interaction/Interactable.cs` (신규)
- `Assets/My/Scripts/Player/InteractionOutline.cs`
