# 0046 - 자판기 상호작용 시 물건 배출

## 요청 내용
> 자판기와 상호작용하면 물건이 나오는거로 하고 싶은데 한번 만들어줄래
> 물건이 나오는 위치도 지정할수 있도록
> 나오는 물건 프리팹도 연결할수 있도록

## 조사 내용
`Assets/Scenes/InGame.unity`의 `Vending_machine` 프리팹 인스턴스(root fileID `1500965751`)에 이미:
- `Interactable`(`1500965752`): `type: 2` = `Generic`, `onInteract` 비어있음
- `Outline`(`1500965753`): 기본 비활성화
- `BoxCollider`(`1500965757`)

가 붙어있음 (Cube 테스트 때와 같은 패턴). 즉 자판기 자체는 이미 상호작용 가능한 상태고, `onInteract`만 비어있음.

`Interactable.cs`에는 물건을 "생성(Instantiate)"하는 기능이 없음 — `Pickup`은 이미 씬에 있는 오브젝트를 인벤토리로 옮기는 것뿐. 자판기처럼 상호작용마다 새 오브젝트를 특정 위치에 만들어내는 것은 다른 동작이라 새 기능이 필요함.

## 설계 (라이브러리/기존 구조 재사용)
`Interactable.cs`는 건드리지 않음 — 이미 있는 `Generic` + `onInteract` UnityEvent를 그대로 재사용.

새 스크립트 1개만 추가:

**`Assets/My/Scripts/Interaction/ItemDispenser.cs`**
```csharp
using UnityEngine;

public class ItemDispenser : MonoBehaviour
{
    [SerializeField] private GameObject itemPrefab;

    public void Dispense()
    {
        if (itemPrefab != null)
            Instantiate(itemPrefab, transform.position, transform.rotation);
    }
}
```
- `itemPrefab`: 나오는 물건 프리팹 (Inspector에서 연결, 지금은 비워둠 → 승인 후 프리팹 지정 필요)
- 물건이 나오는 위치 = 이 스크립트가 붙은 오브젝트의 **Transform 위치 자체** → Scene 뷰에서 자유롭게 옮겨서 지정 가능

## 계획된 변경
1. **새 스크립트** `Assets/My/Scripts/Interaction/ItemDispenser.cs` 생성 (위 코드)
2. **씬에 새 GameObject** `VendingMachine_DispensePoint` 추가 (최상위, `ItemDispenser` 컴포넌트 부착)
   - 초기 위치는 자판기와 같은 좌표 `(-23.620584, 1.7777921, -5.174995)`로 배치 — 승인 후 자판기 배출구 위치로 직접 옮기면 됨
   - `itemPrefab`은 비워둠 (승인 후 원하는 물건 프리팹을 Inspector에서 드래그)
3. **`Vending_machine`의 `Interactable`(`1500965752`) `onInteract`**에 `VendingMachine_DispensePoint`의 `ItemDispenser.Dispense()` 등록

## 적용 결과
계획대로 적용함:
- `Assets/My/Scripts/Interaction/ItemDispenser.cs`(+`.meta`) 생성
- `VendingMachine_DispensePoint` GameObject를 자판기와 같은 좌표(`-23.620584, 1.7777921, -5.174995`)에 생성, `ItemDispenser` 부착 (`itemPrefab` 비움)
- `Vending_machine`의 `onInteract`에 `ItemDispenser.Dispense()` 등록

## 요약 / 남은 작업
- 승인되면 위 3가지를 적용 (코드 1파일 + 씬 변경)
- 적용 후 사용자가 직접: (a) DispensePoint 위치를 배출구로 이동, (b) itemPrefab에 원하는 물건 프리팹 할당
- 나오는 물건이 직접 주울 수 있어야 한다면, 그 물건 프리팹 쪽에 `Interactable`(`Pickup`) + `Collider`가 이미 있어야 함 (이번 범위 밖, 필요하면 별도 요청)
- 변경 예정 파일: `Assets/My/Scripts/Interaction/ItemDispenser.cs`(신규), `Assets/Scenes/InGame.unity`
