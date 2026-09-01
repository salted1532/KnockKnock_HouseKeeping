# 0123 - 열쇠 고리: 재걸기 밀림 + 바닥 낙하 버그 수정

날짜: 2026-08-31
관련: `doc/0087`(HookEffect), `doc/0088`(아무 열쇠 걸기), `Assets/My/InGame/Prefabs/MotelRoom/Owner's_Motel_Room.prefab`

## 증상 (사용자)

1. 열쇠를 고리에 **걸 때마다 뒤로 밀리거나 작아지는 것 같음**
2. 처음부터 `initialHungItems` 에 연결해뒀는데 **걸리지 않고 바닥에 떨어지는 열쇠**가 있음

## 원인

### 버그 1 — `hung` 리스트 중복 누적 → `stackOffset` 만큼 매번 밀림

`HookEffect.Hang()`:
```csharp
hung.RemoveAll(g => g == null || !g.activeSelf);   // activeSelf 로 "떼어감" 판단
...
item.transform.position = Socket.position + Socket.rotation * (stackOffset * hung.Count);
```

재걸기 흐름:
1. `PickupEffect.Hide()` 는 열쇠를 즉시 `SetActive(false)` 안 함 — 줍기 소리가 끝날 때까지 기다리는 코루틴(`FinishAfterSound`)을 예약
2. 소리 끝나기 전에 다시 걸면 `RemoveAll(!activeSelf)` 가 그 열쇠(아직 active)를 못 지움
3. `PickupEffect.Reactivate()` → `StopAllCoroutines()` 로 코루틴 취소 → 열쇠 영영 active → `hung` 에 **중복 누적**
4. `hung.Count` 증가 → `stackOffset(0,0,0.02)` × count 만큼 매 재걸기마다 이동 (Socket 이 world -X 향이라 "뒤로"·"작아짐"으로 보임)

### 버그 2 — 자식에 컴포넌트가 있는 열쇠(`Key2` 계열) 낙하

`Key2`, `Key2 (1~4)` 는 `Rigidbody`/`PickupEffect` 가 **자식 `Simple_03`** 에 있는데 `initialHungItems` 는 `Key2` 부모를 가리킴.
`Hang()` 의 `item.GetComponent<Rigidbody>()` 는 루트만 봄 → `Key2` 부모엔 없음 → `isKinematic=true` 못 검 → 자식 Rigidbody 낙하. (`Key1` 계열은 루트에 있어 정상)

## 수정

### `Interaction/Effects/HookEffect.cs` — `Hang()`

```csharp
// 컴포넌트가 자식에 있는 열쇠(Key2 등) → 실제 PickupEffect 오브젝트로 정규화
var pe = item.GetComponentInChildren<PickupEffect>();
if (pe != null) item = pe.gameObject;

hung.Remove(item);                                 // ← 재걸기 시 중복 제거 (버그1)
hung.RemoveAll(g => g == null || !g.activeSelf);
...
var rb = item.GetComponentInChildren<Rigidbody>(); // ← 루트에 없으면 자식 (버그2)
if (rb != null) rb.isKinematic = true;
```

### `Owner's_Motel_Room.prefab`

`Key1`, `Key2/Simple_03` 의 `Rigidbody.isKinematic` `false → true` (에디터·시작 프레임에도 안 떨어지게). 던지기 경로는 `InventorySystem.ThrowActiveItem` 이 `isKinematic=false` 로 되돌리므로 무관.

## 검증 (플레이모드)

- 초기 10개 열쇠 전부 `active=True`, `kinematic=True`, `socket거리 = 0.000m` (Key2 계열 포함 — 낙하 없음)
- 같은 열쇠 5회 재걸기 → `hung.Count` 1 유지, `socket거리` 0.0000m 유지 (밀림 없음)
- `uloop compile` Error 0

## 추가 — 고리 정원 제한 (겹쳐 걸기 금지)

증상: 처음부터 열쇠가 걸린 고리에도 다른 열쇠를 겹쳐 걸 수 있음.

`HookEffect` 에 `[SerializeField] int capacity = 1` 추가. `Play` 에서 떼어간 것 정리 후 `hung.Count >= Mathf.Max(1, capacity)` 면 early-return (열쇠는 인벤토리에 그대로).
- 기존 프리팹 고리 12개 전부 직렬화값 `capacity = 1` (Unity 가 필드 이니셜라이저 적용).
- `initialHungItems` 로 1개 걸린 고리(`Key_hook` 등) → `hung.Count = 1` → 꽉 참 → 겹쳐 걸기 거부. 빈 고리(`Key_hook (10)`, `(11)`) → 걸림.
- 여러 개 스택하려면 그 고리만 `capacity` 를 2+ 로.
- `Mathf.Max(1, capacity)` 라 직렬화값이 0 이어도 최소 1.

검증: 플레이모드 — 초기 열쇠 있는 고리 `꽉참? True`, 빈 고리 `False`. `uloop compile` Error 0.

## 상태

2026-08-31 수정 + 검증 완료 (재걸기 밀림 / Key2 낙하 / 겹쳐 걸기 금지).
