# 0090 - HookEffect: 부모로 넣지 않고 위치/회전만 맞추기

## 요청
> 이거 key_hook에서 socket에 열쇠가 걸리면 key_hook 크기에 맞춰서 키도 같이 줄어드는거 같네
> 그냥 소켓 위치랑 회전값에 맞춰서 위치하도록만 해줘 그 밑으로 들어가는게 아니라

## 조사
현재 `HookEffect.Play()`: `item.transform.SetParent(Socket, false)` 로 **부모를 Socket으로 바꿔서** 붙이는 방식. `Key_hook` 루트 스케일이 `{0.05, 0.02, 0.02}`라 doc/0087에서 `Socket.localScale`을 역수(`{20,50,50}`)로 보정해뒀지만, 실제로 걸어보니 키가 찌그러져 보임 — 부모-자식 스케일 체인은 계산이 어긋나기 쉬워서(회전 있으면 shear까지 겹침) 근본적으로 깨지기 쉬운 방식.

요청대로 **아예 부모를 바꾸지 않고** 위치/회전만 `Socket`에 맞추면 스케일 상속 문제 자체가 없어짐(item 은 원래 자기 스케일 그대로 유지).

## 계획

### `HookEffect.cs`
```csharp
using UnityEngine;

// 빈 고리에, 지금 손에 든 열쇠(HandItem.IsKey)를 걸어 고정한다. (Key_hook 등)
// socket 의 위치/회전에 맞춰 배치만 함 — 부모로 넣지 않음(부모 스케일 영향 안 받게).
// 다시 떼어가는 동작은 걸린 오브젝트 자신의 Interactable(줍기)+PickupEffect 를 그대로 재사용.
public class HookEffect : InteractionEffect
{
    [Tooltip("걸린 아이템이 위치할 지점. 비우면 이 오브젝트 자신 사용")]
    [SerializeField] private Transform socket;
    [Tooltip("씬에 미리 걸어둔 아이템(선택) — 시작부터 걸려있는 상태를 표현할 때 지정")]
    [SerializeField] private GameObject initialHungItem;

    private GameObject hungItem;

    private Transform Socket => socket != null ? socket : transform;
    private bool IsOccupied => hungItem != null && hungItem.activeSelf;

    private void Awake()
    {
        if (initialHungItem != null && initialHungItem.activeSelf)
            hungItem = initialHungItem;
    }

    public override void Play(in InteractionContext ctx)
    {
        if (IsOccupied || InventorySystem.Instance == null) return;

        var held = InventorySystem.Instance.ActiveHandItem;
        if (held == null || !held.IsKey) return;

        GameObject item = InventorySystem.Instance.RemoveActiveItem();
        if (item == null) return;

        item.transform.SetPositionAndRotation(Socket.position, Socket.rotation);
        item.SetActive(true);

        var rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        hungItem = item;
    }
}
```

- 자식 스캔(`Socket.childCount` 순회) 대신 **직접 참조**(`hungItem` 필드)로 점유 판정. 걸린 오브젝트가 자기 `PickupEffect`로 다시 집히면(`SetActive(false)`) `IsOccupied`가 자동으로 `false`가 됨 — 이전과 동일한 자동 해제 동작 유지.
- `initialHungItem` 필드 추가: "처음부터 걸려있는" 씬 배치를 지원하려면 원래 자식-스캔 방식은 씬 시작 시점에도 자동 인식됐는데, 부모를 안 바꾸면 그 자동 인식 수단이 없어짐 → 인스펙터에서 미리 걸어둔 오브젝트를 직접 지정하는 필드로 대체. 지난 턴에 안내한 "Socket 자식으로 넣고 isKinematic 켜기" 레시피는 이제 **"Socket 위치/회전 맞추고 isKinematic 켜고, HookEffect의 initialHungItem 에 연결"**로 바뀜(부모는 원래대로 둠).

### `Key_hook.prefab`
- `Socket`의 보정 스케일(`{20, 50, 50}`)은 더 이상 쓸 데가 없어짐(부모로 안 쓰니 스케일 무의미) → `{1, 1, 1}`로 되돌림(정리, 안 해도 동작엔 지장 없지만 헷갈림 방지).
- `HookEffect`에 `initialHungItem` 필드가 새로 생기지만 지금은 빈 고리라 비워둠.

### 문서
`Docs/HookEffect.md`의 "부모 스케일 보정" 설명을 "부모로 넣지 않고 위치/회전만 맞춤"으로 수정, `initialHungItem` 필드 추가.

## 리스크
- 낮음. 동작 방식만 단순화, 다른 시스템과의 연결점(`InventorySystem.ActiveHandItem`/`RemoveActiveItem`)은 그대로.
- 걸린 아이템이 이제 `Socket`의 자식이 아니라 원래 있던 계층 그대로 유지됨 — Hierarchy 창에서 "고리 밑에 열쇠가 보인다"는 시각적 정리는 없어짐(요청대로).

## 결과 (2026-08-28, 승인 후 적용)
계획대로:
- `HookEffect.cs`: 부모 변경 없이 `SetPositionAndRotation(Socket.position, Socket.rotation)`으로 배치, 점유 판정을 `hungItem` 필드 직접 참조로 교체, `initialHungItem` 필드 추가(`Awake`에서 반영).
- `Key_hook.prefab`: `Socket`의 보정 스케일(`{20,50,50}` — 사용자가 그 사이 위치/회전은 인스펙터에서 이미 조정해둔 상태였음)을 `{1,1,1}`로 되돌림, `HookEffect`에 `initialHungItem: {fileID: 0}` 필드 추가.
- `Docs/HookEffect.md` 갱신(부모 스케일 보정 설명 삭제, 미리 걸어두는 새 레시피 추가).

## 검증
- 정적 확인만 완료. Unity에서 열쇠를 걸었을 때 더 이상 찌그러지지 않는지, 다시 떼어갈 때 정상 동작하는지 확인 필요.

## 상태
2026-08-28 완료.
