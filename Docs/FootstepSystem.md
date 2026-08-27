# FootstepSystem

`Assets/My/Scripts/Player/FootstepSystem.cs`

플레이어 이동 거리 누적으로 발소리 타이밍 계산. `[RequireComponent(typeof(CharacterController))]`.

## 필드

| 필드 | 설명 |
|---|---|
| `stepDistance` (기본 2) | 이 거리마다 발소리 1회 |
| `rayDistance` (기본 1.5) | 지면 판정 레이 길이 |
| `groundMask` (`LayerMask`) | 지면 레이어 |
| `sprintPitch` (기본 2) | 달릴 때 피치 배수 |

## 동작 (`Update`)

- 접지 안 됐거나 수평 속도 < 0.1 이면 누적 초기화.
- 수평 속도 × deltaTime 을 누적, `stepDistance` 넘으면 `PlayFootstep`.
- `PlayFootstep`: 발밑으로 레이캐스트 → 맞은 콜라이더의 **레이어**와 스프린트 피치를 `SoundManager.PlayFootstep` 에 전달.

지면 재질 구분은 지면 오브젝트의 레이어(Wood/Concrete/Metal/Grass)로 함.

## 관련
[SoundManager](SoundManager.md)
