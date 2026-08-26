# 0066 - 카트 밀기 회전력을 Z축(조향) 성분으로만 제한

## 요청 내용
> 그 밀기 작용에서 rotation force가 카트의 z 회전축에만 적용되는 힘으로 제한할수 있나? 이제 각도 조절떄문에 뒤집히거나 그럴일은 없는데 그쪽으로 힘이 들어가는게 안좋은거 같아서 그냥 z으로만 회전시킬수 있고 미는힘만 존재하도록

## 조사 내용
- `Interactable.PushAwayFromPlayer()` (`Interactable.cs:87`)는 `Vector3.Cross(offset, dir) * rotationForce`로 토크를 계산해서 그대로 `AddTorque`에 넣음 — 이 벡터는 3축 아무 방향이나 될 수 있음(모퉁이를 비스듬히 밀면 X/Y 성분도 섞임)
- [[0065-cart-gyroscope-configurable-joint]]로 ConfigurableJoint가 X/Y 방향은 30° 원뿔로 막아주긴 하지만, 그 방향으로 토크 자체는 계속 들어가서 Joint가 계속 밀어내는 힘과 싸우는 상태가 됨 → 요청대로 아예 카트의 로컬 Z축(조향 축) 성분만 남기고 나머지는 버리는 게 맞음
- 카트의 로컬 Z축은 `body.transform.forward`로 구할 수 있음(0057에서 확인: 베이크된 -90° X 회전 때문에 로컬 Z가 조향 축). 토크 벡터를 이 축에 내적(dot)해서 그 축 방향 성분만 남기면(투영) Z축 회전만 발생하고 X/Y 성분은 완전히 0이 됨

## 계획된 변경

**`Interactable.cs`**
```diff
         Vector3 offset = hitPoint - body.worldCenterOfMass;
-        body.AddTorque(Vector3.Cross(offset, dir) * rotationForce, ForceMode.Impulse);
+        Vector3 torque = Vector3.Cross(offset, dir) * rotationForce;
+        Vector3 steerAxis = body.transform.forward; // 카트 로컬 Z(조향) 축
+        body.AddTorque(Vector3.Dot(torque, steerAxis) * steerAxis, ForceMode.Impulse);
```

## 동작 요약
- 모퉁이를 비스듬히 밀어도 실제로 가해지는 회전력은 카트의 조향 축(Z) 성분만 남고, 그 외 축으로는 전혀 토크가 안 들어감
- 미는 힘(`pushForce`, `AddForce`)은 기존 그대로 (수평 방향 전체 유지, 변경 없음)

## 적용 결과
계획대로 적용함.
