# 0069 - CartGroundAlign 레이 시각화

## 요청 내용
> 현재 카드가 발사하는 레이가 눈에 보이도록 해봐

## 계획된 변경
`Debug.DrawLine`으로 각 바퀴의 레이를 그림 — 바닥에 맞으면 초록, 허공에 떠서 폴백 지점을 쓰면 빨강. Scene 뷰에서 Play 중에 보임(Gizmos 켜져 있으면 Game 뷰에서도).

**`CartGroundAlign.cs`**
```diff
     private Vector3 GetGroundPoint(Transform wheel)
     {
-        if (Physics.Raycast(wheel.position, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
+        if (Physics.Raycast(wheel.position, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
+        {
+            Debug.DrawLine(wheel.position, hit.point, Color.green);
             return hit.point;
+        }
+        Vector3 fallback = wheel.position + Vector3.down * rayDistance;
+        Debug.DrawLine(wheel.position, fallback, Color.red);
-        return wheel.position + Vector3.down * rayDistance;
+        return fallback;
     }
```

## 적용 결과
계획대로 적용함.
