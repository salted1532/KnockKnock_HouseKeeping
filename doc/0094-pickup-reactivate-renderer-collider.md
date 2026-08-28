# 0094 - 던지기/고리 걸기 시 렌더러·콜라이더 복원 안 됨

## 요청
> 이제 줍는 소리는 잘 나오는데 다시 아이템 버리니깐 제대로 메쉬나 콜라이더가 켜지지 않아서 보이지도 않고 맵밖으로 날라가버리네

## 조사
doc/0093에서 `PickupEffect.Hide()`가 즉시 `SetActive(false)` 하는 대신 **렌더러/콜라이더의 `enabled`를 직접 끄는 방식**으로 바꿈(소리가 끝날 때까지 오브젝트 자체는 살려두기 위해). 문제는 `Renderer.enabled`/`Collider.enabled`가 `GameObject.SetActive`와 **완전히 별개의 상태**라서, 나중에 `SetActive(true)`로 되살려도 그 `enabled=false`는 그대로 남음.

이 값을 안 되돌리고 그냥 `SetActive(true)`만 하는 곳이 2군데:
- `InventorySystem.ThrowActiveItem()` — `F`로 버릴 때 `thrownItem.SetActive(true)` (렌더러 꺼진 채로 안 보임, 콜라이더도 꺼진 채라 물리 충돌 없이 그대로 날아가서 맵 밖으로 나감 — 신고된 증상과 정확히 일치)
- `HookEffect.Play()` — 고리에 걸 때 `item.SetActive(true)` (같은 문제, 걸어도 안 보임)

## 계획
`PickupEffect`가 "숨김" 개념을 정의한 주체이므로, 그 반대(되살리기)도 같이 정의해서 두 호출부가 재사용하게 한다.

### `PickupEffect.cs`
```csharp
// 이 아이템을 다시 세상에 내놓을 때(던지기/고리 걸기 등) 호출 — Hide() 로 꺼둔 렌더러/콜라이더를 되살림.
public static void Reactivate(GameObject go)
{
    foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
    foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = true;
}
```

### `InventorySystem.ThrowActiveItem()`
```csharp
thrownItem.SetActive(true);
PickupEffect.Reactivate(thrownItem);
thrownItem.transform.SetParent(null);
```

### `HookEffect.Play()`
```csharp
item.transform.SetPositionAndRotation(Socket.position, Socket.rotation);
item.SetActive(true);
PickupEffect.Reactivate(item);
```

## 리스크
- 낮음. 렌더러/콜라이더를 원래 안 꺼둔 적 없는 아이템(=한 번도 `Hide()`를 안 거친 오브젝트)에 `Reactivate`를 호출해도 이미 `enabled=true`라 아무 변화 없음 — 안전.

## 결과 (2026-08-28, 승인 후 적용)
계획대로 `PickupEffect.Reactivate(GameObject)` 정적 메서드 추가, `InventorySystem.ThrowActiveItem()`과 `HookEffect.Play()` 양쪽의 `SetActive(true)` 직후에 호출 추가. `Docs/PickupEffect.md`에 설명 추가.

## 검증
- 정적 확인만 완료. Unity Play 모드에서 아이템 주웠다 F로 던졌을 때 보이는지/벽에 막히는지, 열쇠 주웠다 고리에 다시 걸었을 때 보이는지 확인 필요.

## 상태
2026-08-28 완료.
