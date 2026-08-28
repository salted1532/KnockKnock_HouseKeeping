# 0095 - 소리 재생 중 던지면 나중에 다시 꺼져버림 (남은 코루틴)

## 요청
> 뭔가 소리가 나던중 던져버리면 버그가 나는거 같은데 버린 아이템이 addforce를 받지 않고 바로 바닥으로 떨어지거나
> 아니면 공중에서 갑자기 안보이거나 그러네

## 조사
doc/0093에서 `PickupEffect.Hide()`가 소리 재생 중이면 `FinishAfterSound()` 코루틴을 걸어서 **소리가 끝나면 그제서야 `SetActive(false)`** 하도록 만들었음. 이 코루틴은 `PickupEffect` 컴포넌트에 붙어 계속 살아있음(오브젝트 자체가 아직 안 꺼졌으니까).

문제 시나리오: 줍기 → 소리 재생 중(코루틴 대기 중) → 바로 인벤토리에서 그 아이템을 **F로 던짐** → `ThrowActiveItem()`이 `PickupEffect.Reactivate()`로 렌더러/콜라이더를 되살리고 물리(`AddForce`)를 줌 → **그런데 아까 걸어둔 `FinishAfterSound` 코루틴은 취소가 안 돼서 그대로 살아있음** → 잠시 후 소리가 자연히 끝나면 그 코루틴이 `Finish(false)` → `gameObject.SetActive(false)`를 호출해서, 이미 던져져 날아가는 중인 오브젝트를 **또 꺼버림**.

타이밍에 따라:
- 던지자마자(같은 프레임 근처) 소리가 거의 다 끝난 상태였다면 → `AddForce`가 물리 엔진에 반영되기도 전에 오브젝트가 비활성화돼서 힘이 무시된 것처럼(그냥 바닥으로) 보임.
- 좀 더 날아간 뒤에 소리가 끝나면 → 날아가던 중 갑자기 사라짐.

## 계획
`PickupEffect.Reactivate()`가 "이 오브젝트를 다시 세상에 내놓는다"는 뜻이므로, 그 시점에 남아있는 `PickupEffect`의 예약된 코루틴(옛날 줍기의 뒷정리)을 같이 취소한다.

```csharp
public static void Reactivate(GameObject go)
{
    foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
    foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = true;

    var pe = go.GetComponent<PickupEffect>();
    if (pe != null) pe.StopAllCoroutines();   // 이전 줍기의 "소리 끝나면 끄기" 예약 취소
}
```

`ThrowActiveItem()`/`HookEffect`는 이미 `Reactivate()`를 호출하고 있어서 별도 수정 불필요 — 이 한 군데만 고치면 양쪽 다 해결됨.

## 리스크
- 낮음. `StopAllCoroutines()`는 그 `PickupEffect` 컴포넌트가 시작한 코루틴만 멈춤(다른 컴포넌트엔 영향 없음). 코루틴이 없는 상태(=소리가 이미 끝난 뒤 던진 경우)에도 호출 자체는 안전(아무 효과 없음).
- 코루틴이 취소돼도 이미 재생 중이던 소리 자체는 안 끊기고 자연스럽게 끝까지 남 — 그냥 "소리 끝나면 숨기기" 예약만 없어짐.

## 결과 (2026-08-28, 승인 후 적용)
`PickupEffect.Reactivate()`에 `pe.StopAllCoroutines()` 한 줄 추가. `Docs/PickupEffect.md` 갱신.

## 검증
- 정적 확인만 완료. Unity Play 모드에서 줍자마자(소리 재생 중) F로 던져서 정상적으로 힘 받아 날아가고, 도중에 안 사라지는지 확인 필요.

## 상태
2026-08-28 완료.
