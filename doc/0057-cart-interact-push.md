# 0057 - 쇼핑카트 상호작용 시 플레이어 반대 방향으로 밀기

## 요청 내용
> 쇼핑카트를 상호작용하면 상호작용하는 플레이어 방향으로 밀도록 하면 좋겠는데 해당하는 코드 만들어줘

## 조사 내용
- `InteractionOutline.cs`: 화면 중앙 레이캐스트로 `Interactable`을 찾고 E키 입력 시 `Interactable.Interact()`를 인자 없이 호출. 플레이어 참조를 넘기지 않음 → `Interactable` 쪽에서 `GameObject.FindGameObjectWithTag("Player")`로 직접 찾아야 함 (`ActivatePlayerFlashlight()`에서 이미 쓰는 패턴)
- `Interactable.cs`: `InteractionType` enum(`Pickup`, `TidyBed`, `Generic`, `Flashlight`)에 따라 분기하는 구조. 새 타입 `Push` 추가해서 같은 패턴으로 확장
- Carro_su(씬의 카트 오브젝트, `Assets/My/Prefabs/Vehicles/Cars/Carro_su.prefab` 인스턴스)는 씬에서 `Rigidbody`+`BoxCollider`가 인스턴스 오버라이드로 추가되어 있음(0055 관련 대화에서 확인). 아직 `Interactable` 컴포넌트는 없음 — 이번 작업은 스크립트만 추가, 씬에 컴포넌트 붙이는 건 사용자가 직접

## 계획된 변경

**`Interactable.cs`**
```diff
 public enum InteractionType
 {
     Pickup,
     TidyBed,
     Generic,
     Flashlight,
+    Push,
 }
```
```diff
     [Header("Generic (버튼 등 자유 연출)")]
     [SerializeField] private UnityEvent onInteract;
+
+    [Header("Push (상호작용 시 플레이어 반대 방향으로 밀기)")]
+    [SerializeField] private float pushForce = 3f;

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
             case InteractionType.Flashlight:
                 ActivatePlayerFlashlight();
                 break;
+            case InteractionType.Push:
+                PushAwayFromPlayer();
+                break;
         }

         onInteract?.Invoke();
     }
```
```diff
     private void ActivatePlayerFlashlight()
     {
         ...
     }
+
+    private void PushAwayFromPlayer()
+    {
+        Rigidbody body = GetComponentInParent<Rigidbody>();
+        GameObject player = GameObject.FindGameObjectWithTag("Player");
+        if (body == null || body.isKinematic || player == null)
+            return;
+
+        Vector3 dir = transform.position - player.transform.position;
+        dir.y = 0f;
+        if (dir.sqrMagnitude < 0.0001f)
+            return;
+
+        body.AddForce(dir.normalized * pushForce, ForceMode.Impulse);
+    }
 }
```

## 동작 요약
- E키로 상호작용하면 플레이어 → 카트 방향(수평 성분만)으로 `pushForce` 크기의 순간적인 힘(`ForceMode.Impulse`)을 가함 → 플레이어가 보는 쪽으로 카트가 쭉 밀려나감
- `Rigidbody`는 `GetComponentInParent<Rigidbody>()`로 찾음 — `Interactable`이 카트 루트(Rigidbody가 붙은 오브젝트)에 있어도, 자식에 있어도 동작

## 사용자가 씬/에셋에서 직접 해야 하는 일
1. Carro_su(카트) 오브젝트에 **Interactable** 컴포넌트 추가
2. Type을 **Push**로 설정, Push Force 값 조절(기본 3)
3. (`InteractionOutline`이 레이캐스트로 찾으므로) 카트에 콜라이더가 상호작용 가능한 레이어에 있는지 확인

## 적용 결과
계획대로 적용함.

## 후속 요청 (승인 대기)
> push할때 미는 힘을 직접 조절할수 있게 할수 있나?
> 상호작용 하는 방향에 따라 살짝 회전에도 힘을 받았으면 좋겠어 현재 카트의 x,y의 회전값을 고정시켜놨는데 z가 좌우로 도는건데 방향에 따라 회전도 좀 일어 났으면 좋겠네

첫 질문(힘 조절)은 이미 `pushForce`가 `[SerializeField]`라 Inspector에서 바로 조절 가능 — 코드 변경 없음.

두 번째(방향에 따른 회전)는 씬에서 Carro_su Rigidbody 확인해보니 `m_Constraints: 48` = Freeze Rotation X + Y (로컬 축 기준), Z만 자유 — 사용자 설명과 일치. 카트 prefab의 로컬 회전(X축 -90°) 때문에 로컬 Z축이 대략 월드 업(수직) 축에 해당해서, 자유로운 Z 회전 = 좌우 조향(요) 회전.

**핵심 아이디어**: 토크를 직접 계산해서 넣는 대신, 힘을 카트 중심이 아니라 **플레이어 위치에서** 가하면(`AddForceAtPosition`), 무게중심과 플레이어 위치 사이의 수평 오프셋만큼 물리 엔진이 자동으로 토크를 만들어줌 — 정면에서 밀면 그대로 직진, 옆에서/비스듬히 밀면 자연스럽게 회전. X/Y 회전은 이미 고정돼 있어서 필요없는 축 토크는 물리 엔진이 알아서 무시함. 별도 회전량 계산/새 필드 불필요.

```diff
-        body.AddForce(dir.normalized * pushForce, ForceMode.Impulse);
+        body.AddForceAtPosition(dir.normalized * pushForce, player.transform.position, ForceMode.Impulse);
```

적용함.

## 후속 요청 2 (승인 대기)
> 상호작용을 모퉁이에다가 하면 해당하는 방향으로 회전값도 들어가게끔 해줘 현재는 너무 힘이 약하네

## 조사 내용
- 방금 적용한 `AddForceAtPosition`은 힘을 **플레이어 위치**에서 가하고 있음 → 무게중심-플레이어 오프셋은 항상 "플레이어 서 있는 쪽"으로만 정해지지, 실제로 화면 크로스헤어가 카트의 어느 지점(모퉁이 vs 중앙)을 겨누는지는 전혀 반영 안 됨. "모퉁이를 겨누면 그 방향으로 회전"하려면 **레이캐스트가 맞은 지점**(`RaycastHit.point`)을 힘 작용점으로 써야 함
- `InteractionOutline.cs`가 화면 중앙 레이캐스트로 `hit`을 이미 갖고 있는데, `Interact()`를 인자 없이 호출 중이라 그 지점 정보가 `Interactable`로 전달이 안 됨 → `hit.point`를 넘기도록 시그니처 확장 필요
- 힘 세기(`pushForce`)는 Inspector에서 조절 가능하지만 기본값 3은 약하다는 피드백 → 기본값을 올림 (인스턴스별로 이미 다르게 설정했다면 그 값은 영향 없음, 새로 붙이는 것부터 기본값 적용)

## 계획된 변경

**`Interactable.cs`**
```diff
     [Header("Push (상호작용 시 플레이어 반대 방향으로 밀기)")]
-    [SerializeField] private float pushForce = 3f;
+    [SerializeField] private float pushForce = 6f;

-    public void Interact()
+    public void Interact(Vector3? hitPoint = null)
     {
         switch (type)
         {
             ...
             case InteractionType.Push:
-                PushAwayFromPlayer();
+                PushAwayFromPlayer(hitPoint ?? transform.position);
                 break;
         }

         onInteract?.Invoke();
     }
```
```diff
-    private void PushAwayFromPlayer()
+    private void PushAwayFromPlayer(Vector3 hitPoint)
     {
         Rigidbody body = GetComponentInParent<Rigidbody>();
         GameObject player = GameObject.FindGameObjectWithTag("Player");
         if (body == null || body.isKinematic || player == null)
             return;

         Vector3 dir = transform.position - player.transform.position;
         dir.y = 0f;
         if (dir.sqrMagnitude < 0.0001f)
             return;

-        body.AddForceAtPosition(dir.normalized * pushForce, player.transform.position, ForceMode.Impulse);
+        body.AddForceAtPosition(dir.normalized * pushForce, hitPoint, ForceMode.Impulse);
     }
```

**`InteractionOutline.cs`**
```diff
         if (currentInteractable != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
         {
-            currentInteractable.Interact();
+            currentInteractable.Interact(hit.point);
```
(`hit`은 위쪽 `Physics.Raycast(ray, out RaycastHit hit, ...)`에서 이미 선언된 변수 — `currentInteractable`이 non-null인 프레임엔 항상 이번 프레임 레이캐스트가 성공한 상태라 `hit.point`가 유효함)

## 동작 요약
- 밀 때 크로스헤어가 카트의 어느 지점을 겨누고 있었는지(모퉁이 등)가 실제 힘 작용점이 됨 → 모퉁이를 겨누고 상호작용하면 그만큼 회전(토크)이 더 크게 걸림, 중앙을 겨누면 거의 직진
- 기본 `pushForce` 3 → 6으로 상향 (기존에 다른 값으로 바꿔둔 인스턴스는 영향 없음)

적용함.

## 후속 요청 3 (승인 대기)
> 미는 힘과 회전힘은 다르게 작동하고 다르게 수치 조절할수 있게 해줘

## 조사 내용
- 현재 `AddForceAtPosition` 한 번으로 힘/회전이 같이 결정됨 → `pushForce` 하나가 직진과 회전 세기를 동시에 좌우해서 따로 조절 불가
- 직진력(`AddForce`, 무게중심에)과 회전력(`AddTorque`, 오프셋으로 계산한 축)을 분리하면 각각 독립적인 계수로 조절 가능. `Vector3.Cross(offset, dir)`는 정규화하지 않고 그대로 두면 "모퉁이일수록(오프셋이 클수록) 회전이 커지는" 기존 특성은 유지하면서, 거기에 별도 배율(`rotationForce`)만 곱해서 세기만 독립적으로 조절

## 계획된 변경

**`Interactable.cs`**
```diff
     [Header("Push (상호작용 시 플레이어 반대 방향으로 밀기)")]
     [SerializeField] private float pushForce = 6f;
+    [SerializeField] private float rotationForce = 2f;
```
```diff
     private void PushAwayFromPlayer(Vector3 hitPoint)
     {
         Rigidbody body = GetComponentInParent<Rigidbody>();
         GameObject player = GameObject.FindGameObjectWithTag("Player");
         if (body == null || body.isKinematic || player == null)
             return;

         Vector3 dir = transform.position - player.transform.position;
         dir.y = 0f;
         if (dir.sqrMagnitude < 0.0001f)
             return;

-        body.AddForceAtPosition(dir.normalized * pushForce, hitPoint, ForceMode.Impulse);
+        dir.Normalize();
+        body.AddForce(dir * pushForce, ForceMode.Impulse);
+
+        Vector3 offset = hitPoint - body.worldCenterOfMass;
+        body.AddTorque(Vector3.Cross(offset, dir) * rotationForce, ForceMode.Impulse);
     }
```

## 동작 요약
- `Push Force`: 무게중심에 가하는 직진 힘 세기 (겨눈 지점과 무관)
- `Rotation Force`: 겨눈 지점(모퉁이일수록 큼)에 비례해 걸리는 회전 힘의 배율 — 0으로 두면 회전 없이 직진만
- 둘 다 Inspector에서 독립적으로 조절 가능

적용함.

## 요약 / 남은 작업
- 변경 파일: `Assets/My/Scripts/Interaction/Interactable.cs`, `Assets/My/Scripts/Player/InteractionOutline.cs`
- 씬 작업은 사용자가 직접 (위 "사용자가 씬/에셋에서 직접 해야 하는 일" 1~3번)
