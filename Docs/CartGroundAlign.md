# CartGroundAlign

`Assets/My/Scripts/Interaction/CartGroundAlign.cs`

쇼핑카트가 4개 바퀴 지점의 지면을 레이캐스트해 바닥 기울기에 맞춰 눕도록 회전 정렬. `[RequireComponent(typeof(Rigidbody))]`.

## 필드

| 필드 | 설명 |
|---|---|
| `frontLeft` / `frontRight` / `rearLeft` / `rearRight` (`Transform`) | 4개 바퀴 위치 |
| `rayDistance` (기본 1) | 지면 판정 레이 길이 |
| `groundMask` (`LayerMask`) | 지면 레이어 |
| `alignSpeed` (기본 8) | 정렬 Slerp 속도 |

## 동작 (`FixedUpdate`)

- 4개 바퀴 아래로 레이캐스트해 지면 접점 4개 획득 (못 맞으면 `rayDistance` 아래로 폴백).
- 좌우/앞뒤 접점 평균으로 지면 평면의 up 벡터 계산 → `Quaternion.FromToRotation` 으로 목표 회전.
- `rb.MoveRotation(Slerp(현재, 목표, alignSpeed * dt))`.

`OnDrawGizmos` 없이 `Debug.DrawLine` 으로 레이 시각화 (씬 뷰).

> 진행 중 기능 (쇼핑카트 디테일). 밀기는 [PushEffect](PushEffect.md) 가 별도 담당.

## 관련
[PushEffect](PushEffect.md)
