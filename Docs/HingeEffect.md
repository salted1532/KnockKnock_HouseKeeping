# HingeEffect

`Assets/My/Scripts/Interaction/Effects/HingeEffect.cs`

경첩 회전으로 여닫기. 구 `Door.cs` 대체. `Interactable.isToggle` 을 켜서 쓴다.

## 필드

| 필드 | 설명 |
|---|---|
| `hinge` (`Transform`) | 회전시킬 Transform. **비우면 이 오브젝트 자신** |
| `axis` (`Vector3`, 기본 `(0,1,0)`) | `hinge` 로컬 기준 회전축. 문=위쪽 Y, 쓰레기통 뚜껑=옆쪽 X 등. 0이면 Y로 폴백 |
| `openAngle` (기본 90) | 열림 각도. 음수면 반대 방향 |
| `openTime` (기본 0.6) | 여닫는 시간(초) |
| `ease` (`AnimationCurve`) | 회전 보간 커브 (기본 EaseInOut) |

## 동작

- `Awake`: `hinge` 현재 로컬 회전을 "닫힘"으로 기억, `openRot = 닫힘 * Quaternion.AngleAxis(openAngle, axis)`.
- `Start`: `Interactable.IsOn` 에 맞춰 초기 회전 적용.
- `Play`: 목표 회전으로 코루틴 `Slerp`. 스윙 도중 재상호작용하면 코루틴 중단 후 반대 방향으로 (인터럽트 안전).
- `SwingTo(float angleDeg, float time)` (public) — 닫힘 기준 **임의 각도**로 스윙. `Interactable.IsOn` 안 건드림. `RoomController.PeekDoor` 가 새벽 노크 시 문 살짝 열기(`peekAngle`)/닫기(0)에 사용 (`doc/0118`).

## 배치

- `hinge` 미지정: 이 컴포넌트 붙은 오브젝트가 피벗 → 문/뚜껑 메시를 자식으로.
- `hinge` 지정: `Interactable`+`HingeEffect` 는 본체(쓰레기통 몸통 등)에, 뚜껑 피벗 Transform 만 연결. 콜라이더는 본체에, Interaction 레이어.

## 관련
[Interactable](Interactable.md) · [SfxEffect](SfxEffect.md) · [`doc/0074`](../doc/0074-door-open-interaction.md)
