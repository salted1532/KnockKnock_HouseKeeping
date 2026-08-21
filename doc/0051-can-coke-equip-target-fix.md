# 0051 - 자판기에서 뽑힌 Can_Coke를 손 아이템과 연결하기

## 요청 내용
> Can_Coke라는 프리팹을 생성해서 자판기랑도 연결했는데 플레이어 손에 있는 아이템이랑은 연결을 못하겠네 어떻게 하면 좋을까

## 조사 내용
`Assets/My/InGame/Prefabs/Can_Coke.prefab` 확인:
- `Interactable`: `type: 0`(Pickup), `itemIcon` 연결됨, **`equipTarget: {fileID: 0}`(비어있음)**
- `Rigidbody`, `CapsuleCollider`, `Outline` 이미 구성됨

`equipTarget`은 플레이어 손 밑에 미리 배치된 씬 오브젝트를 가리켜야 하는데, `Can_Coke`는 프리팹 에셋이라 씬 오브젝트를 참조할 수 없음 (Unity가 프리팹 에셋 편집 중엔 씬 전용 참조를 거부함). 게다가 `ItemDispenser.Dispense()`는 매번 `Instantiate(itemPrefab, ...)`으로 새로 복제하기 때문에, 씬에 직접 놓인 인스턴스에 거는 것 같은 오버라이드 트릭도 못 씀 — 뽑힐 때마다 새로 생기는 사본이라 미리 연결해둘 대상이 없음.

## 계획된 변경
`ItemDispenser`(씬 오브젝트, `VendingMachine_DispensePoint`)는 씬에 있으니 손 아이템을 자유롭게 참조 가능 → 배출 직후 스크립트로 주입.

**`Interactable.cs`**: `equipTarget`을 외부에서 설정할 수 있는 메서드 추가
```diff
     [SerializeField] private GameObject equipTarget;
+
+    public void SetEquipTarget(GameObject target) => equipTarget = target;
```

**`ItemDispenser.cs`**
```diff
     [SerializeField] private GameObject itemPrefab;
+    [SerializeField] private GameObject equipTarget;

     public void Dispense()
     {
-        if (itemPrefab != null)
-            Instantiate(itemPrefab, transform.position, transform.rotation);
+        if (itemPrefab == null)
+            return;
+
+        GameObject spawned = Instantiate(itemPrefab, transform.position, transform.rotation);
+        spawned.GetComponent<Interactable>()?.SetEquipTarget(equipTarget);
     }
```

## 사용자가 씬에서 직접 해야 하는 일 (코드로 대신할 수 없는 부분)
1. **손에 들 Can_Coke 배치**: flashlight와 같은 방식으로, `Can_Coke` 프리팹을 Hierarchy에서 `Player > PlayerCameraRoot` 밑으로 드래그 → 손에 보일 위치/회전/크기로 맞춘 뒤 **비활성화(체크 해제)**. (지금 프리팹 스케일이 17.8로 커서 손에 맞게 축소 필요)
2. `VendingMachine_DispensePoint`의 `ItemDispenser` 컴포넌트에서 새로 생긴 **Equip Target** 필드에 방금 만든 손 Can_Coke를 연결

이 두 가지는 손 위치/크기를 눈으로 보면서 맞춰야 하는 작업이라 직접 하는 게 맞다고 판단, 코드/씬 자동화는 하지 않음. 원하면 초기 임시 배치(대략적 위치)까지는 대신 해줄 수 있음.

## 적용 결과
코드 부분 계획대로 적용함.

## 요약 / 남은 작업
- 변경 파일: `Assets/My/Scripts/Interaction/Interactable.cs`, `Assets/My/Scripts/Interaction/ItemDispenser.cs`
- 씬 작업은 사용자가 직접 (위 1, 2번) — 아직 미완료
