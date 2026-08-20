# 0035 - 상호작용 후 Interaction_Text가 계속 켜져있는 문제 수정

## 날짜
2026-08-20

## 요청 내용 (원문)
> 손전등을 E로 상호작용하고 나서 Interaction_Text가 계속 활성화되어 남아있는거좀 고쳐줘

## 조사 내용
- 손전등의 `Interactable` 타입은 `Pickup`으로 보임. `Interactable.Pickup()`(`Assets/My/Scripts/Interaction/Interactable.cs`)은 `equipTarget`이 없으면 `Destroy(gameObject)`로 오브젝트 자체를 파괴함.
- `Destroy()` 직후에도 C# 변수 `currentInteractable`은 여전히 그 파괴된 오브젝트를 가리키는 참조를 들고 있음. Unity의 `UnityEngine.Object`는 `==`/`!=` 연산자를 오버로딩해서 "파괴된 오브젝트"를 `null`과 같다고 취급하기 때문에, 다음 프레임에 레이캐스트가 진짜 `null`을 반환해도 `hitInteractable(null) != currentInteractable(파괴된 참조)` 비교가 **false**로 나옴 (둘 다 "null"로 판정되어 같다고 인식).
- 그 결과 `InteractionOutline.Update()`의 "대상이 바뀌었을 때만" 블록이 다시는 실행되지 않아서, `interactionText.SetActive(false)`가 영원히 호출되지 않고 텍스트가 켜진 채로 남음.
- 근본 원인은 "다음 프레임에 레이캐스트가 알아서 감지해줄 것"이라는 가정이 오브젝트 파괴/비활성화 케이스에서 깨진다는 것 → E키로 상호작용을 실행한 그 순간, 결과를 기다리지 말고 즉시 상태를 정리하는 방식으로 수정.

## 계획

### `Assets/My/Scripts/Player/InteractionOutline.cs` 수정
```diff
         if (currentInteractable != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
-            currentInteractable.Interact();
+        {
+            currentInteractable.Interact();
+
+            if (currentOutline != null)
+                currentOutline.enabled = false;
+            currentOutline = null;
+            currentInteractable = null;
+
+            if (interactionText != null)
+                interactionText.SetActive(false);
+        }
```
- `Interact()` 호출 직후 아웃라인/텍스트/추적 변수를 즉시 초기화 → 대상이 파괴되든, 비활성화되든, 그대로 남아있든 상관없이 항상 정리됨.
- 대상이 그대로 남아있는 타입(TidyBed, Generic 등)이어도 다음 프레임에 레이캐스트가 다시 감지해서 자연스럽게 켜지므로 눈에 띄는 문제 없음(1프레임 이하 깜빡임, 사실상 무해).

## 결과
승인 후 계획대로 적용 완료.

## 변경된 파일
- `Assets/My/Scripts/Player/InteractionOutline.cs` — `Interact()` 호출 직후 아웃라인/텍스트/추적 상태를 즉시 초기화하도록 수정
