# 0062 - 던진 아이템이 플레이어 자신의 콜라이더에 부딪히는 문제

## 요청 내용
> 아니다 높이가 문제가 아니라 바닥을 보고 던지게 되면 y값이 올라간건가 그로 인해 캡슐 콜리더에 부딫하면서 플레이어의 정수리에 부딪히면서 캔이 완전히 다른 방향으로 날라가거나 머리 위에서 떨어지는거 처럼 보이는거였어

## 조사 내용
- `MainCamera`는 씬에서 `PlayerCapsule`(플레이어 루트, `Player` 태그 + `CharacterController` 보유)과 **별개의 최상위 오브젝트**임 (Cinemachine으로 플레이어의 `PlayerCameraRoot`를 따라가는 구조 — 부모/자식 관계 아님)
- `ThrowPos`는 `MainCamera`의 자식이라, 카메라(=플레이어 머리 부근) 위치에서 살짝 앞으로 떨어진 지점에 스폰됨. 바닥을 보고 던지면 스폰 지점이 앞쪽 아래로 내려가면서 플레이어 자신의 캡슐 콜라이더(머리~발끝을 감싸는 영역) 안이나 바로 옆에서 스폰됨
- 던지는 아이템(`Can_Coke.prefab` 등)은 `CapsuleCollider`(트리거 아님) + `Rigidbody`를 가진 완전한 물리 오브젝트 → 스폰 직후 플레이어의 `CharacterController`(이것도 `Collider`의 일종)와 겹치면서 물리 엔진이 강하게 밀어냄 → 캔이 엉뚱한 방향으로 튕기거나 머리 위에서 떨어지는 것처럼 보임
- `ThrowActiveItem()`의 `SphereCastAll` 벽 체크는 `throwPos.root`(=`MainCamera` 자신, 부모 없음이라 root가 곧 자신) 기준으로 플레이어를 제외하려 했지만, 애초에 `MainCamera`엔 플레이어 하위 콜라이더가 없어서 이 제외 로직이 실질적으로 아무것도 걸러내지 못하고 있었음. 하지만 이건 스폰 위치 계산(벽 뚫림 방지)에만 관여하고, 실제 원인은 스폰 후 물리 충돌 자체이므로 이 부분은 손댈 필요 없음
- 근본 해결: 아이템이 스폰된 직후, 그 아이템의 콜라이더와 플레이어의 `CharacterController` 사이의 충돌을 `Physics.IgnoreCollision`으로 꺼주면 됨 (바닥/벽 등 다른 오브젝트와는 정상적으로 충돌하고, 오직 자신을 던진 플레이어와만 안 부딪힘)

## 계획된 변경

**`InventorySystem.cs`**
```diff
             rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
             rb.AddForce(throwPos.forward * throwForce, ForceMode.Impulse);
+
+            GameObject player = GameObject.FindGameObjectWithTag("Player");
+            CharacterController playerController = player != null ? player.GetComponent<CharacterController>() : null;
+            if (playerController != null)
+            {
+                foreach (Collider itemCollider in thrownItem.GetComponentsInChildren<Collider>())
+                    Physics.IgnoreCollision(itemCollider, playerController, true);
+            }
         }
```

## 동작 요약
- 던진 아이템은 스폰 직후부터 플레이어 자신의 콜라이더와는 절대 충돌하지 않음 (바닥을 보고 던져도 자기 몸에 맞고 튕기는 일 없음)
- 바닥/벽/다른 오브젝트와의 충돌은 그대로 정상 작동
- 한번 `IgnoreCollision`을 켜면 해당 아이템 오브젝트가 파괴되기 전까지 계속 유지됨 — 나중에 다시 주워도 문제없고, 바닥에 떨어진 뒤 플레이어가 그 위를 그냥 통과해서 지나갈 수 있음(가벼운 쓰레기 아이템이라 자연스러움)

## 적용 결과
계획대로 적용함. (0060의 ThrowPos 위치 변경은 그대로 유지, 0061의 Y 제거안은 반려)
