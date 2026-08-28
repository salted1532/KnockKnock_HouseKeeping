# 0088 - HookEffect: 정해진 키 대신 "현재 손에 든 열쇠"를 걸도록 변경

## 요청
> hookeffect에서 정해진 키를 거는게 아니라 플레이어의 인벤에서 들고있는 열쇠 아이템을 거는식으로 하는게 좋을거 같아

[[doc/0087]]에서는 `HookEffect.acceptedItemId`로 고리마다 특정 `ItemId`(예: Key101)만 받게 했음. 고리 10개마다 각각 다른 itemId를 지정해야 했는데, 대신 "지금 손에 들고 있는 게 열쇠 종류면 그게 뭐든 그 고리에 건다"로 바꾸고 싶다는 요청.

## 조사
- 씬(`Assets/Scenes/InGame.unity`)의 `HandItem` 인스턴스는 현재 4개뿐: `id:1`(Flashlight), `id:2`(Soda), `id:3`(Key101), `id:4`(Key102). 앞으로 Key103~110도 같은 방식으로 손 오브젝트가 추가될 예정으로 보임.
- `HandItem`엔 `id`만 있고 "이게 열쇠 종류인지" 표시하는 필드가 없음 → 종류 판별용 플래그 필요.
- 기존 `InventorySystem.TryRemoveItem(ItemId id, ...)`(0087에서 추가, 아직 `HookEffect` 외 사용처 없음)은 "지정한 id를 5슬롯 아무데서나 찾기" 방식 — 이번 요청은 "지금 손에 쥐고 있는(활성 슬롯) 그 아이템"이 기준이라 의미가 다름. 그대로 두면 죽은 코드가 되므로 교체.

## 계획

### 1. `HandItem`에 `isKey` 플래그 추가
```csharp
public class HandItem : MonoBehaviour
{
    [SerializeField] private ItemId id;
    [Tooltip("열쇠 종류 아이템인가 (Key_hook 등에 걸 수 있음)")]
    [SerializeField] private bool isKey;
    public ItemId Id => id;
    public bool IsKey => isKey;
}
```
씬의 `id:3`(Key101), `id:4`(Key102) 두 `HandItem`에 `isKey: 1` 설정. Flashlight/Soda는 그대로 0.

### 2. `InventorySystem`: `TryRemoveItem(ItemId)` → 활성 슬롯 기준 API로 교체
```csharp
// 현재 활성 슬롯(손에 든 것)의 HandItem. 없으면 null.
public HandItem ActiveHandItem =>
    activeSlot >= 0 && equipTargets[activeSlot] != null
        ? equipTargets[activeSlot].GetComponentInChildren<HandItem>(true)
        : null;

// 활성 슬롯을 비우고 원래 월드 오브젝트(pickupSource)를 반환. 빈 손이면 null.
public GameObject RemoveActiveItem()
{
    if (activeSlot < 0 || equipTargets[activeSlot] == null) return null;
    GameObject pickupSource = pickupSources[activeSlot];
    equipTargets[activeSlot].SetActive(false);
    ClearSlot(activeSlot);
    return pickupSource;
}
```
(0087에서 추가한 `TryRemoveItem(ItemId, out GameObject)`는 삭제 — 사용처가 없어짐.)

### 3. `HookEffect`: `acceptedItemId` 제거, 활성 아이템이 열쇠인지로 판단
```csharp
public class HookEffect : InteractionEffect
{
    [Tooltip("걸린 아이템이 위치할 지점. 비우면 이 오브젝트 자신 사용")]
    [SerializeField] private Transform socket;

    private Transform Socket => socket != null ? socket : transform;

    public override void Play(in InteractionContext ctx)
    {
        if (IsOccupied || InventorySystem.Instance == null) return;

        var held = InventorySystem.Instance.ActiveHandItem;
        if (held == null || !held.IsKey) return;

        GameObject item = InventorySystem.Instance.RemoveActiveItem();
        if (item == null) return;

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

### 4. `Key_hook.prefab`
`HookEffect` 컴포넌트의 `acceptedItemId: 3` 줄 제거(필드 자체가 없어짐). 나머지(Socket, Interactable, 콜라이더/레이어 등)는 그대로.

### 5. 문서
`Docs/HookEffect.md`에서 `acceptedItemId` 설명 제거, "열쇠 종류(`HandItem.IsKey`) 판정" 방식으로 갱신. `Docs/HandItemRegistry.md`에 `isKey` 필드 언급 추가.

## 효과
- 어떤 고리든 **아무 열쇠나** 걸 수 있음(방 번호 매칭 없음) — 나중에 "이 고리는 101호 전용" 같은 제약이 필요해지면 그때 `HandItem.Id` 비교를 얹으면 됨(지금은 요청 범위 밖이라 안 만듦).
- 새 열쇠(Key103~110) 추가할 때 손 오브젝트의 `HandItem.isKey`만 체크하면 끝 — 고리마다 설정할 값 없음.

## 리스크
- 낮음. `TryRemoveItem(ItemId)`를 쓰는 다른 코드 없음(0087에서 막 추가돼 이번에 바로 교체되는 것) — 확인 완료.
- 씬 파일(`InGame.unity`) 직접 수정 포함 — 텍스트 YAML 편집이라 Unity에서 열었을 때 정상 로드되는지 확인 필요.

## 결과 (2026-08-28, 승인 후 적용)
계획대로:
- `HandItem`에 `isKey`(bool) 필드 + `IsKey` 프로퍼티 추가.
- `InventorySystem.TryRemoveItem(ItemId, ...)` 삭제 → `ActiveHandItem`(프로퍼티) + `RemoveActiveItem()`으로 교체.
- `HookEffect`: `acceptedItemId` 필드 제거, `ActiveHandItem?.IsKey` 판정으로 변경.
- `Key_hook.prefab`의 `HookEffect` 컴포넌트에서 `acceptedItemId: 3` 줄 제거.
- `Assets/Scenes/InGame.unity`: `HandItem id:3`(Key101), `id:4`(Key102) 두 곳에 `isKey: 1` 추가.
- `Docs/HookEffect.md`·`Docs/InventorySystem.md`·`Docs/HandItemRegistry.md` 갱신.

## 검증
- 정적 확인만 완료. Unity에서 씬 정상 로드되는지, Key101/102 들고 고리 상호작용 시 걸리는지 확인 필요.
- 앞으로 Key103~110 손 오브젝트 추가 시 `HandItem.isKey`만 체크하면 됨(고리 쪽 설정 불필요).

## 상태
2026-08-28 완료.
