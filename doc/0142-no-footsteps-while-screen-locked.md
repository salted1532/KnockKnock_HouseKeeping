# 0142 - 화면고정/오버레이 중 발소리 차단

날짜: 2026-09-02
관련: `doc/0027`(FootstepSystem), `doc/0038`(발소리 겹침), `doc/0118`(새벽 노크), `Assets/My/Scripts/Player/FootstepSystem.cs`, `Assets/My/Scripts/Interaction/Modes/UIInteractionMode.cs`

## 요청 (원문)

> 노크 할때 플레이어가 움직이는 와중에 발소리가 발생하는 와중에 노크로 화면고정이 되면 발소리가 자꾸 발생하는데 화면고정시 발소리 발생하는걸 막아줘

## 원인

`UIInteractionMode.Enter` (노크·접객·모니터)는 `characterController.enabled = false` 로 끄고
플레이어를 transform 으로 앵커까지 옮긴다. CC 가 꺼지면 `controller.velocity` 는 **마지막 값에서 멈춘다** —
노크 순간 달리고 있었으면 그 속도가 그대로 남아, `FootstepSystem.Update` 가 계속 거리 누적 →
`stepDistance` 마다 발소리를 무한 트리거.

## 수정

### `UIInteractionMode` — 이동 잠금 상태 노출

```csharp
public bool FrozenForOverlay { get; private set; }         // FreezeForOverlay 가 토글 (노트 읽기·페이드 전환)
public bool MovementLocked => Active || FrozenForOverlay;   // 화면고정이든 오버레이든 이동 잠김
```
`FreezeForOverlay(on)` 첫 줄에 `FrozenForOverlay = on;` 추가.

### `FootstepSystem.Update` — 게이트 추가

```csharp
if (!controller.enabled ||
    (UIInteractionMode.Instance != null && UIInteractionMode.Instance.MovementLocked))
{
    distanceAccumulator = 0f;
    return;
}
```

- `!controller.enabled` = 화면고정으로 CC 꺼진 상태 (Teardown 복귀 트랜지션 포함) — 근본 조건.
- `MovementLocked` = 위 + `FreezeForOverlay`(노트·페이드) 도 커버.
- 둘 다 만족 시 누적 초기화 후 리턴 → 발소리 없음.

`FootstepSystem` 이 유일한 발소리 소스 (`PlayerCapsule` 에 1개, 애니 이벤트 없음).

## 검증 (플레이)

- 게임플레이: `cc.enabled=True` `Active=False` `MovementLocked=False` → 통과 (발소리 O).
- `UIInteractionMode.Enter` 후: `cc.enabled=False` `Active=True` `MovementLocked=True` → **게이트 차단 (발소리 X)**.
- `uloop compile` Error 0.

## 영향 파일

```
Interaction/Modes/UIInteractionMode.cs   수정  FrozenForOverlay / MovementLocked
Player/FootstepSystem.cs                  수정  Update 게이트
Docs/UIInteractionMode.md · Docs/FootstepSystem.md  갱신
```

## 상태

2026-09-02 코드 + 컴파일 + 상태 검증 완료.
