# 0047 - TidyBed/Pickup이 Generic(onInteract)과 함께 작동하도록

## 요청 내용
> tidy bed랑 제네릭이랑 같이 작동하도록 할수 있나?
> pickup이라도 제네릭이랑 같이 작동하도록해줘

## 조사 내용
`Interactable.cs`의 `Interact()`는 `switch (type)`으로 분기하며, `onInteract?.Invoke()`는 `Generic` case에서만 호출됨. 그래서 `Pickup`/`TidyBed`/`Flashlight` 타입인 오브젝트는 `onInteract`를 채워도 절대 실행되지 않음.

씬 전체에서 현재 `onInteract`에 실제 콜이 등록된 곳은 Cube(`AudioSource.Play`)와 자판기(`ItemDispenser.Dispense`) 둘뿐이고, 둘 다 이미 `Generic` 타입이라 이번 변경으로 기존 동작은 바뀌지 않음.

## 계획된 변경
`onInteract?.Invoke()`를 `Generic` 전용 case에서 빼서, 타입에 상관없이 항상 마지막에 한 번 호출되도록 이동 (한 곳만 고치면 모든 타입에 적용되는 root-cause 수정).

`Assets/My/Scripts/Interaction/Interactable.cs`
```diff
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
-            case InteractionType.Generic:
-                onInteract?.Invoke();
-                break;
             case InteractionType.Flashlight:
                 ActivatePlayerFlashlight();
                 break;
         }
+
+        onInteract?.Invoke();
     }
```
(`InteractionType.Generic`은 그대로 유지, switch에서 처리할 고유 동작이 없을 뿐)

## 참고
- `Pickup`이 `equipTarget`을 실패(인벤토리 꽉 참 등)해도 `onInteract`는 매번 호출됨 — 성공 여부와 무관하게 항상 같이 울리는 단순한 방식. 성공했을 때만 울리게 하려면 별도 요청 필요.
- `Pickup`이 `equipTarget == null`이라 즉시 `Destroy(gameObject)`되는 경우도, Unity의 `Destroy`는 프레임 끝에 실제 파괴되므로 그 직후 `onInteract?.Invoke()`는 정상 실행됨.

## 적용 결과
계획대로 적용함 — `onInteract?.Invoke()`를 switch 밖으로 빼서 모든 타입 공통으로 실행되도록 변경.

## 요약 / 남은 작업
- 변경 파일: `Assets/My/Scripts/Interaction/Interactable.cs`
