# 0087 - 열쇠고리(Key_hook) 걸기 기능

## 요청
> Key 아이템 추가했는데 줍기 아이템이고 아직 사용은 없는데 아이템이라서 rigidbody가 있는데
> key_Hook라고 열쇠고리도 추가했는데 Key아이템이 고리에 걸려있는 동안엔 rigidbody가 작동 하지 않고
> 빈 Key_hook에다가 열쇠를 든 상태에서 상호작용하면 열쇠가 해당 고리 위치에 고정되도록 하는식으로 구현하고 싶어

## 조사
- `Assets/My/Scripts/Inventory/ItemId.cs`: `Key101`~`Key110` 이미 추가돼 있음(모텔 객실 키 10개로 추정).
- `Assets/My/InGame/Prefabs/Item/Key1.prefab`: `Interactable(promptType=줍기)` + `PickupEffect(itemId=Key101, consumeOnUse=0, useClip=없음)` + `Rigidbody(isKinematic=0)` + `BoxCollider` + `Outline`(off) + `SfxEffect` + `ItemImpactSound` + `AudioSource`. [[project_interaction-system-redesign]]의 줍기 표준 구성 그대로.
- `Assets/My/InGame/Prefabs/Item/Key_hook.prefab`: 지금은 Capsule 메시 + `BoxCollider`만 있음. `Interactable`/효과 없음, 레이어 0(Interaction 레이어 아님). 스케일이 `{0.05, 0.02, 0.02}`로 매우 찌그러져 있음(핀/못처럼 보이려고) — **이 트랜스폼에 열쇠를 그대로 자식으로 붙이면 열쇠 메시가 같이 찌그러짐**.
- `InventorySystem.cs`: 슬롯마다 `equipTargets`(손 오브젝트)·`pickupSources`(원래 월드 오브젝트) 배열은 있지만 **`ItemId` 를 저장하지 않음** — 슬롯 인덱스만으로 아이템 종류를 알 수 없음. 다만 `equipTargets[i]`가 곧 `HandItemRegistry.Resolve(itemId)`가 반환한, `HandItem` 컴포넌트를 가진 그 오브젝트이므로 `equipTargets[i].GetComponent<HandItem>().Id`로 역추적 가능.
- `ThrowActiveItem()`은 다시 꺼낸 `pickupSource`에 Rigidbody 없으면 추가하고 `AddForce`만 함 — **기존에 `isKinematic=true`로 남아있으면 힘이 안 먹어서 공중에 멈춘 것처럼 보임**. 지금은 발생 안 하지만 이번에 고리에서 kinematic 을 켜는 기능을 넣으면 "고리에 걸었다 다시 주워서 던지기" 경로에서 재현되는 잠재 버그라 같이 고침.

## 설계
줍기 아이템을 다시 집어드는 동작은 **이미 있는 걸 그대로 재사용**한다 — 걸린 열쇠도 원래 `Interactable(줍기)+PickupEffect`를 가진 그 오브젝트라서, 다시 보고 E 누르면 기존 `PickupEffect.Play()`가 그대로 다시 실행되어 인벤토리로 들어가고 `SetActive(false)` 됨 (= "다시 떼어감"). 새 코드 필요 없음.

### 1. `InventorySystem.TryRemoveItem` 추가
```csharp
// itemId 와 일치하는 손 오브젝트를 든 슬롯을 비우고 원래 월드 오브젝트(pickupSource)를 반환.
// HookEffect 가 "들고 있는 열쇠를 고리에 건다" 용도로 사용. 없으면 false.
public bool TryRemoveItem(ItemId id, out GameObject pickupSource)
{
    for (int i = 0; i < SlotCount; i++)
    {
        if (equipTargets[i] == null) continue;
        var hi = equipTargets[i].GetComponentInChildren<HandItem>(true);
        if (hi == null || hi.Id != id) continue;

        pickupSource = pickupSources[i];
        equipTargets[i].SetActive(false);
        if (activeSlot == i) UpdateFlashlightHint();
        ClearSlot(i);
        return true;
    }
    pickupSource = null;
    return false;
}
```
(`ClearSlot`이 아이콘 UI 정리까지 처리하므로 그대로 재사용.)

### 2. 새 효과 `HookEffect` (`Assets/My/Scripts/Interaction/Effects/HookEffect.cs`)
특정 프롬프트 카테고리에 안 맞는 특수 동작이라 [[project_interaction-system-redesign]] 관례대로 별도 서브클래스로 추가(`Interactable`의 큰 스위치는 안 건드림).

```csharp
using UnityEngine;

// 빈 고리에, 들고 있는 열쇠(acceptedItemId)를 걸어 고정한다.
// socket 이 비어있는 동안(활성 자식 없음)만 동작 — 이미 걸려있으면 무시.
public class HookEffect : InteractionEffect
{
    [Tooltip("이 고리가 받는 열쇠 (예: Key101)")]
    [SerializeField] private ItemId acceptedItemId;
    [Tooltip("걸린 열쇠가 위치할 지점. 비우면 이 오브젝트 자신 사용")]
    [SerializeField] private Transform socket;

    private Transform Socket => socket != null ? socket : transform;

    public override void Play(in InteractionContext ctx)
    {
        if (IsOccupied || InventorySystem.Instance == null) return;
        if (!InventorySystem.Instance.TryRemoveItem(acceptedItemId, out GameObject item) || item == null) return;

        item.transform.SetParent(Socket, false);
        item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        item.SetActive(true);

        var rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    private bool IsOccupied
    {
        get
        {
            for (int i = 0; i < Socket.childCount; i++)
                if (Socket.GetChild(i).gameObject.activeSelf) return true;
            return false;
        }
    }
}
```
- 점유 여부를 별도 bool 필드로 관리 안 하고 `Socket`의 활성 자식 유무로 판단 — 열쇠가 다시 뽑혀서(자기 `PickupEffect`가 `SetActive(false)`) 비활성화되면 자동으로 "빈 고리" 상태가 됨.
- `acceptedItemId` 하나만 받음 — 고리 10개(Key101~110) 각각 인스펙터에서 다른 값 지정.

### 3. `Key_hook.prefab` 구성
- 스케일 왜곡 문제 때문에 **자식으로 빈 `Socket` 오브젝트(스케일 1,1,1) 추가**, 열쇠는 이 자식에 붙임(고리 메시 자체 트랜스폼 말고).
- 루트에 `Interactable(promptType=상호작용)` + `HookEffect(acceptedItemId=Key101, socket=위 자식)` 추가.
- 우클릭 "Prompt Type에 맞게 효과 재설정" 실행 → `SfxEffect`/콜라이더/레이어(Interaction)/`Outline` 자동 정리. `HookEffect`는 managed 목록에 없어서 재설정에도 안 지워짐.
- 나머지 9개 고리(Key102~110)는 이 프리팹을 복제해 `acceptedItemId`만 바꿔 배치.

### 4. `InventorySystem.ThrowActiveItem` 안전장치
```csharp
if (rb == null)
    rb = thrownItem.AddComponent<Rigidbody>();
rb.isKinematic = false;   // 고리에 걸렸다 다시 주워 던지는 경로 대비
rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
```

## 리스크
- `Key_hook` 은 지금 씬에 1개만 배치돼 있다고 알고 있음 — 10개로 복제/배치는 이 작업 범위 밖(요청받으면 별도 진행).
- "고리에 걸려있던 걸 다시 뗀다"는 기존 `PickupEffect` 재사용이라 별도 검증 필요(고리 위치에서 Outline 하이라이트/레이캐스트 정상 동작하는지 에디터에서 확인 권장).
- `Key_hook` 콜라이더가 지금 `{1,1,1}` 크기 그대로라 스케일 왜곡된 트랜스폼에 얹혀 있음 — "재설정" 돌리면 메시 bounds 기준으로 안 바뀌고(콜라이더가 이미 있어서 크기 갱신 로직을 안 탐, `EnsureColliderAndLayer`는 없을 때만 추가) 그대로 유지됨. 필요하면 수동 조정.

## 결과 (2026-08-28, 승인 후 적용)
계획대로 구현:
- `InventorySystem.TryRemoveItem` 추가, `ThrowActiveItem`에 `rb.isKinematic = false` 안전장치 추가.
- `Assets/My/Scripts/Interaction/Effects/HookEffect.cs`(+`.meta`, guid `39790e2f0bbb37cf1512b0689eddb0cc`) 신규 작성.
- `Key_hook.prefab`: 레이어 0→11(Interaction), `Interactable(promptType=상호작용)` + `HookEffect(acceptedItemId=Key101, socket=Socket)` + `SfxEffect`(clip 비움) + `Outline`(off) + `AudioSource` 추가. 자식 `Socket` 오브젝트 신규 추가.
- **설계 문서에 없던 추가 발견**: `Key_hook` 루트 트랜스폼 스케일이 `{0.05, 0.02, 0.02}`라서 `Socket`을 단순히 로컬 스케일 1로 자식으로 넣어도 부모 스케일이 곱해져 그대로 찌그러짐(자식은 부모 스케일을 상속). `Socket.localScale`을 부모 스케일의 역수 `{20, 50, 50}`로 설정해 상쇄, 최종 월드 스케일 1이 되도록 수정.
- `Docs/HookEffect.md` 신규 작성, `Docs/InventorySystem.md`·`Docs/InteractionSystem.md`·`Docs/PickupEffect.md`에 상호 링크 추가.

## 검증
- 코드/YAML 정적 확인만 완료. **Unity 에디터에서 직접 확인 필요**:
  - `Key_hook` 프리팹을 씬에 배치 후, `Key101`(Key1) 아이템을 들고 상호작용(E) → 열쇠가 `Socket` 위치에 정상 크기로 붙는지, 자유낙하 안 하는지.
  - 붙은 열쇠를 다시 보고 상호작용 → 손으로 돌아오고 고리가 다시 빈 상태(Outline 하이라이트 다시 뜨는지)로 인식되는지.
  - 나머지 9개 고리(Key102~110)는 `Key_hook.prefab` 복제 후 `acceptedItemId`만 변경해서 배치(이번 작업 범위 밖).

## 상태
2026-08-28 코드/프리팹 반영 완료, 에디터 실동작 확인은 사용자 몫.
