# PushEffect

`Assets/My/Scripts/Interaction/Effects/PushEffect.cs`

상호작용 시 부모 `Rigidbody` 를 주체(플레이어) 반대 방향으로 밀기. 구 `Push` 케이스 대체.

## 필드

| 필드 | 설명 |
|---|---|
| `pushForce` (기본 6) | 주체 반대 방향 임펄스 크기 |
| `torqueForce` (기본 2) | 히트 지점 기준 회전 토크 크기 |
| `useSteerAxis` (기본 on) | 토크를 로컬 Z(조향) 축으로만 제한 — 쇼핑카트용. off면 자유 회전 |

## 동작

- `GetComponentInParent<Rigidbody>()` 를 찾음. 없거나 kinematic 이면 무시.
- 주체 = `ctx.Source` (없으면 `Player` 태그 오브젝트).
- 방향 = `body.position - source.position`, 수평(`y=0`)으로 정규화.
- `AddForce(dir * pushForce, Impulse)`.
- 토크 = `Cross(ctx.Point - worldCenterOfMass, dir) * torqueForce`. `useSteerAxis` 면 `body.forward` 성분만.

## 배치
카트 등 밀리는 오브젝트 루트에 `Rigidbody`, `Interactable`(밀기)+`PushEffect`+`SfxEffect`+`ItemImpactSound`(벽 부딪힘). 지면 정렬은 [CartGroundAlign](CartGroundAlign.md) 별도.

## 관련
[Interactable](Interactable.md) · [ItemImpactSound](ItemImpactSound.md) · [CartGroundAlign](CartGroundAlign.md)
