# ItemImpactSound

`Assets/My/Scripts/Interaction/ItemImpactSound.cs`

물리 오브젝트가 충돌할 때 임팩트 사운드 재생. `[RequireComponent(typeof(Collider))]`. `OnCollisionEnter` 이 필요하므로 대상에 `Rigidbody` 도 있어야 함.

## 필드

| 필드 | 설명 |
|---|---|
| `impactClips` (`AudioClip[]`) | 랜덤 재생할 충돌음 (직전 클립 연속 방지) |
| `minImpactVelocity` (기본 1.5) | 이 속도 미만 충돌은 무시 |
| `cooldown` (기본 0.2) | 재생 최소 간격(초) |

## 동작 (`OnCollisionEnter`)

- 쿨다운 중이거나 `collision.relativeVelocity` 크기 < `minImpactVelocity` 면 무시.
- `impactClips` 중 랜덤(직전과 다른 것) 선택 → `AudioSource.PlayClipAtPoint(clip, 접점)`.

우클릭 재설정에서 **줍기 / 밀기** 에 자동 추가됨 (제거는 수동).

## 관련
[PushEffect](PushEffect.md) · [PickupEffect](PickupEffect.md) · [InteractionSystem](InteractionSystem.md)
