# 0096 - 두 번째 던지기부터 AddForce 안 먹힘 (숨어있는 동안 바닥 뚫고 낙하)

## 요청
> 그래도 처음 던질떈 잘 던져지는데 2번째 던질때부턴 addforce가 적용안되고 바로 바닥으로 떨어지는데 원인좀 찾아줘

## 조사
`PickupEffect.Hide()`(doc/0093):
```csharp
private void Hide(bool destroy)
{
    foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
    foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
    ...
}
```
**콜라이더만 끄고 Rigidbody는 그대로 둠.** 오브젝트는 소리가 끝날 때까지(코루틴 대기) 계속 "살아있는" 상태라 물리 시뮬레이션이 계속 돎 — 콜라이더가 없으니 바닥과 충돌을 안 해서 **중력 받아 바닥을 뚫고 계속 떨어짐**(안 보이니 티도 안 남).

- **1번째 줍기**: 아이템이 레벨에 원래 정지해 있던 상태 → 물리 엔진이 이미 "재움(sleep)" 처리해둠 → 콜라이더 꺼도 잠든 리짓바디는 중력을 안 받음(멈춰있음) → 문제 없음. 이후 `ThrowActiveItem`이 `AddForce`로 깨우면서 던짐 → 정상 작동.
- **2번째 줍기**: 1번째로 던진 뒤 착지해서 튕기다 완전히 잠들기 전에(또는 막 착지한 직후) 다시 주우면, 콜라이더가 꺼지는 순간 리짓바디가 아직 "깨어있는" 상태라 계속 낙하 시작 → 소리 재생 시간(코루틴 대기) 동안 바닥을 뚫고 계속 떨어지며 **아래 방향 속도가 누적됨** → 그 상태로 다시 던지면 `SetPositionAndRotation`으로 위치는 원위치로 복귀시키지만 **속도는 안 지워짐** → `AddForce`의 던지는 힘이 이미 누적된 낙하 속도에 묻혀서 그냥 아래로 떨어지는 것처럼 보임.

## 계획
`Hide()`에서 콜라이더 끌 때 **Rigidbody도 같이 kinematic으로 얼림** — 숨어있는 동안 아예 물리 시뮬레이션을 멈춰서 낙하 자체가 안 일어나게.

```csharp
private void Hide(bool destroy)
{
    foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
    foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

    var rb = GetComponent<Rigidbody>();
    if (rb != null) rb.isKinematic = true;   // 숨어있는 동안 낙하 방지(콜라이더가 꺼져 바닥을 뚫고 떨어지는 것 막음)

    var src = GetComponent<AudioSource>();
    if (src != null && src.isPlaying)
        StartCoroutine(FinishAfterSound(src, destroy));
    else
        Finish(destroy);
}
```

별도 복구 코드 불필요 — 이미 두 호출부가 각자 원하는 최종 `isKinematic` 값을 명시적으로 세팅하고 있음:
- `InventorySystem.ThrowActiveItem()`: `Reactivate()` 다음에 `rb.isKinematic = false;` 이미 있음.
- `HookEffect.Play()`: `Reactivate()` 다음에 `rb.isKinematic = true;` 이미 있음(고리에 걸 때는 어차피 고정).

## 리스크
- 낮음. Rigidbody 없는 연출용 픽업(장식용)은 `rb == null`이라 그냥 스킵됨.
- 두 호출부 다 재활성화 시 `isKinematic`을 명시적으로 다시 세팅하므로 "숨어있는 동안 kinematic=true였다가 방치되는" 경우 없음.

## 결과 (2026-08-28, 승인 후 적용)
`PickupEffect.Hide()`에 `rb.isKinematic = true` 추가(콜라이더 끄는 부분 바로 다음). `ThrowActiveItem`/`HookEffect`는 이미 각자 최종 `isKinematic` 값을 명시적으로 세팅하고 있어서 별도 수정 불필요. `Docs/PickupEffect.md` 갱신.

## 검증
- 정적 확인만 완료. Unity Play 모드에서 던지고 → 착지 직후 바로 다시 주워서 → 또 던졌을 때 정상적으로 힘 받아 날아가는지 확인 필요.

## 상태
2026-08-28 완료.
