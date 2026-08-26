# 0064 - CartGroundAlign 회전축을 바퀴 축(axle)으로 고정

## 요청 내용
> 경사로에 진입하니깐 카트가 앞바퀴 쪽으로 기울어져버리는데

## 조사 내용
- [[0063-cart-ground-align-pitch]]에서 만든 `CartGroundAlign.cs`가 `Quaternion.FromToRotation(transform.up, targetForward)`로 정렬 회전을 계산하는데, 이 함수는 회전축을 바퀴 축(`transform.right`, local X)으로 고정하지 않고 "가장 짧은 회전"이 되는 임의의 축을 스스로 고름
- 코드 작성 시 이미 `ponytail:` 주석으로 이 위험을 남겨뒀음: 카트가 완벽히 수평이 아닌 상태(조향 중 등)면 그 축이 바퀴 축에서 살짝 벗어날 수 있고, `FixedUpdate`마다 이 회전을 반복 누적(`delta * rb.rotation`)하니까 경사로 위에 머무는 동안 매 프레임 미세한 롤/요가 같은 방향으로 계속 더해져서 점점 앞바퀴 쪽으로 기울어지는 것으로 관찰됨
- 해결: 회전축을 계산에 맡기지 않고 바퀴 축(`transform.right`)으로 못박음 — `Vector3.SignedAngle(from, to, axis)`로 그 축 기준 각도만 뽑고 `Quaternion.AngleAxis(각도, transform.right)`로 델타를 만들면 항상 피치(바퀴 축 회전)만 발생, 롤/요 드리프트 자체가 불가능해짐

## 계획된 변경

**`CartGroundAlign.cs`**
```diff
         Vector3 targetForward = (fHit.point - rHit.point).normalized;
-        // ponytail: FromToRotation은 axle(local X)이 아닌 최단 회전축을 쓰므로 아주 미세한 요/롤 드리프트가 섞일 수 있음 - 눈에 띄면 axle 축으로 프로젝션해서 보정
-        Quaternion delta = Quaternion.FromToRotation(transform.up, targetForward);
+        float pitchAngle = Vector3.SignedAngle(transform.up, targetForward, transform.right);
+        Quaternion delta = Quaternion.AngleAxis(pitchAngle, transform.right);
         Quaternion targetRot = delta * rb.rotation;
```

## 동작 요약
- 정렬 회전이 항상 바퀴 축(local X) 기준으로만 일어남 → 경사로에 오래 머물러도 롤/요가 누적되지 않아 옆이나 앞으로 계속 더 기울어지는 현상 없음

## 적용 결과
계획대로 적용함.
