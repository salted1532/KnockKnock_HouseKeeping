# 0060 - ThrowPos 위치를 눈높이 정면으로 조정

## 요청 내용
> 아이템 던지는거 Throwpos에서 잘 던져지도록 할래

## 후속 확인
> 바로 눈앞에서 스폰해서 던져져야하는데 바라보는 방향보다 좀더 위에서 던져짐

## 조사 내용
- `InventorySystem.ThrowActiveItem()` (`Assets/My/Scripts/Inventory/InventorySystem.cs:92`)는 `throwPos.position`에 아이템을 스폰하고 `throwPos.forward * throwForce`로 Impulse를 가함 — 방향/힘 로직 자체는 문제없음, 스폰 위치가 원인
- 씬에서 `ThrowPos`(`Assets/Scenes/InGame.unity:34373`)는 `MainCamera`(`Assets/AssetsFolder/StarterAssets/FirstPersonController/Prefabs/MainCamera.prefab`)의 바로 아래 자식
  - Local Rotation: identity (카메라 회전을 그대로 따라감 → 조준 방향 자체는 정확)
  - Local Position: `(0, -0.6, 0.6)` → 카메라 기준 아래로 0.6, 앞으로 0.6 떨어진 지점
- "눈앞"(조준선 상)이 아니라 카메라보다 0.6만큼 아래쪽에서 스폰되고 있었음 → 카메라가 위/아래를 볼 때 이 오프셋이 함께 회전하면서 조준선과 어긋나 보이는 게 "위에서 던져지는" 것처럼 느껴지는 원인으로 보임
- 조준선(카메라 forward) 위에서 바로 스폰되게 하려면 수직 오프셋을 없애고, 카메라와 겹치지 않을 정도로만 앞으로 살짝 띄우면 됨

## 계획된 변경

**`Assets/Scenes/InGame.unity`** (ThrowPos, fileID 1600965761)
```diff
-  m_LocalPosition: {x: -0.000000044703484, y: -0.5999999, z: 0.5999994}
+  m_LocalPosition: {x: 0, y: 0, z: 0.5}
```

## 동작 요약
- ThrowPos가 카메라 조준선(정면) 바로 위에 위치 → 아이템이 눈앞/조준 방향 그대로에서 스폰되어 던져짐
- 벽 체크(SphereCastAll)·힘 방향 로직은 기존 그대로 유지

## 적용 결과
계획대로 적용함.
