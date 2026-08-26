# 0061 - 던지기 벽 체크 시 뒤로 당기는 좌표에서 Y값 제거

## 요청 내용
> 이거 벽뚫리는거 막기위해 Y값이라 올라가는거는 빼줄수 있나?

## 확인
> "벨똸리는거 막기 위해 Y값이라 올라가는거는 빼줘"가 구체적으로 다음 중 어느 부분을 가리키는거야?
→ "벽 체크 로직에서 뒤로 당기는 좌표에서 y값은 제거해줘"

## 조사 내용
- `InventorySystem.ThrowActiveItem()`의 벽 체크 로직: `dir = offset.normalized`(카메라→ThrowPos 방향, 즉 카메라 forward)로 `SphereCastAll` 검사 후, 막혔으면 `spawnPos = origin.position + dir * (closest - 0.15f)`로 뒤로 당김
- `dir`이 카메라 forward라서 위/아래를 보고 있을 때 막히면 당겨진 좌표의 Y도 그만큼 낮아지거나 높아짐(시야 각도만큼 비례) → 이 Y 변화를 없애고 싶다는 요청
- 스폰 위치의 높이는 항상 `throwPos.position.y`(눈높이)로 고정하고, 막혔을 때 당겨지는 건 수평 방향까지만 반영되도록 수정

## 계획된 변경

**`InventorySystem.cs`**
```diff
                 if (blocked)
+                {
                     spawnPos = origin.position + dir * Mathf.Max(closest - 0.15f, 0f);
+                    spawnPos.y = throwPos.position.y;
+                }
```

## 동작 요약
- 벽에 막혀서 스폰 위치가 당겨져도 높이(Y)는 항상 눈높이 그대로 유지, 수평 거리만 줄어듦

## 반려
> 아니다 높이가 문제가 아니라 바닥을 보고 던지게 되면 y값이 올라간건가 그로 인해 캡슐 콜리더에 부딫하면서 플레이어의 정수리에 부딪히면서 캔이 완전히 다른 방향으로 날라가거나 머리 위에서 떨어지는거 처럼 보이는거였어

실제 원인이 아니라고 판단되어 이 변경은 적용하지 않음. 진짜 원인 분석/수정은 [[0062-throw-ignore-player-collision]] 참고.
