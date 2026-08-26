# 0068 - 바퀴 일부가 공중에 뜬 경우에도 정렬되도록 수정

## 요청 내용
> 레이가 아직 나머지 바퀴가 공중에 뜨게 되면 그걸 고쳐주지 않는거 같아 rigidbody x,y값이 고정이니깐 뭐 바퀴2개가 땅에 붙어있어도 나머지 바퀴2개가 공중에 뜨면 그걸 고쳐줘야하는데 안고쳐주네

## 조사 내용
- `CartGroundAlign.FixedUpdate()`(`CartGroundAlign.cs`)의 `TryGetGroundPoint`가 레이캐스트 실패(바닥 없음 = 바퀴가 허공에 뜬 상태)하면 `false`를 반환하고, 4개 중 **하나라도 실패하면 `FixedUpdate`가 그 즉시 `return`** — 정렬 계산 자체를 아예 안 함
- 그래서 "2개는 붙어있고 2개는 떠있는" 흔한 상황(모서리/턱에 걸친 경우)에서 아무 반응도 안 한 것
- 수정: 레이가 안 맞으면 그냥 스킵하지 말고, **`wheel.position`에서 `rayDistance`만큼 아래로 처진 지점**을 그 바퀴의 값으로 대신 사용 → 뜬 바퀴 쪽은 "그만큼 아래로 꺼진 것"으로 취급돼서 자연스럽게 그쪽으로 기울어짐 (실제 바퀴가 허공에 걸리면 축 처지는 느낌과 비슷)

## 계획된 변경

**`CartGroundAlign.cs`**
```diff
     private void FixedUpdate()
     {
-        if (!TryGetGroundPoint(frontLeft, out Vector3 fl) || !TryGetGroundPoint(frontRight, out Vector3 fr) ||
-            !TryGetGroundPoint(rearLeft, out Vector3 rl) || !TryGetGroundPoint(rearRight, out Vector3 rr))
+        if (frontLeft == null || frontRight == null || rearLeft == null || rearRight == null)
             return;

+        Vector3 fl = GetGroundPoint(frontLeft);
+        Vector3 fr = GetGroundPoint(frontRight);
+        Vector3 rl = GetGroundPoint(rearLeft);
+        Vector3 rr = GetGroundPoint(rearRight);
+
         Vector3 rightEdge = (fr + rr) * 0.5f - (fl + rl) * 0.5f;
         Vector3 forwardEdge = (fl + fr) * 0.5f - (rl + rr) * 0.5f;
         Vector3 targetUp = Vector3.Cross(forwardEdge, rightEdge).normalized;

         Quaternion delta = Quaternion.FromToRotation(transform.forward, targetUp);
         Quaternion targetRot = delta * rb.rotation;
         rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, alignSpeed * Time.fixedDeltaTime));
     }

-    private bool TryGetGroundPoint(Transform wheel, out Vector3 point)
+    private Vector3 GetGroundPoint(Transform wheel)
     {
-        point = default;
-        if (wheel == null)
-            return false;
-        if (!Physics.Raycast(wheel.position, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
-            return false;
-        point = hit.point;
-        return true;
+        if (Physics.Raycast(wheel.position, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
+            return hit.point;
+        return wheel.position + Vector3.down * rayDistance;
     }
```

## 동작 요약
- 4개 다 붙어있으면 기존과 동일
- 일부만 붙어있어도(1~3개) 나머지는 "rayDistance만큼 아래로 처진 것"으로 취급해서 계산이 계속 돌아감 → 뜬 바퀴 쪽으로 자연스럽게 기울어짐
- 완전히 공중에 떠서 4개 다 안 붙어도 최소한 크래시 없이 처짐 방향으로 기울어진 채 유지됨

## 적용 결과
계획대로 적용함.
