# 0048 - F키로 현재 든 아이템 던지기

## 요청 내용
> F 키를 누르면 아이템을 버리는걸 구현하고 싶어 플레이어한테 ThrowPos가 있고 해당하는 아이템(상호작용으로 인해 비활성화 된 아이템)을 다시 그 위치에서 AddForce를 이용해서 던지는거야

## 조사 내용
`InventorySystem.cs`(`Assets/My/Scripts/Inventory/InventorySystem.cs`)에 이미:
- `equipTargets[SlotCount]`: 슬롯별 손에 든 오브젝트 참조
- `activeSlot`: 현재 선택된 슬롯 인덱스
- `Update()`에서 숫자키 1~5로 `SelectSlot()` 처리 중

→ "지금 손에 든 아이템"은 `equipTargets[activeSlot]`으로 이미 접근 가능. F키 처리도 같은 `Update()`에 자연스럽게 추가.

`ThrowPos`는 프로젝트에 아직 없음 → 새로 필요 (플레이어 자식으로 빈 Transform 하나, 카메라 앞쪽을 향하게 배치).

기존 hand-equip 오브젝트(예: flashlight)에는 `Rigidbody`가 없음 → 던질 때 없으면 자동으로 `AddComponent<Rigidbody>()`.

## 계획된 변경

**`InventorySystem.cs`**
```diff
     [SerializeField] private Image[] slotIcons = new Image[SlotCount];
     [SerializeField] private GameObject[] activateIcons = new GameObject[SlotCount];
+    [SerializeField] private Transform throwPos;
+    [SerializeField] private float throwForce = 10f;
```
```diff
         else if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
+
+        if (Keyboard.current.fKey.wasPressedThisFrame) ThrowActiveItem();
     }
```
```diff
+    private void ThrowActiveItem()
+    {
+        if (activeSlot < 0 || equipTargets[activeSlot] == null || throwPos == null)
+            return;
+
+        GameObject item = equipTargets[activeSlot];
+
+        item.transform.SetParent(null);
+        item.transform.SetPositionAndRotation(throwPos.position, throwPos.rotation);
+
+        Rigidbody rb = item.GetComponent<Rigidbody>();
+        if (rb == null)
+            rb = item.AddComponent<Rigidbody>();
+        rb.AddForce(throwPos.forward * throwForce, ForceMode.Impulse);
+
+        equipTargets[activeSlot] = null;
+        itemIcons[activeSlot] = null;
+        if (slotIcons[activeSlot] != null)
+            slotIcons[activeSlot].sprite = null;
+        if (activateIcons[activeSlot] != null)
+            activateIcons[activeSlot].SetActive(false);
+    }
```

**씬**: 플레이어 하위에 `ThrowPos` 빈 GameObject 생성 (카메라 앞쪽 방향), `InventorySystem`의 새 `Throw Pos` 필드에 연결. 초기 위치는 대략 카메라 위치로 배치 — 정확한 오프셋은 승인 후 Inspector에서 직접 조정.

## 참고 / 스킵한 부분
- 던진 아이템이 바닥과 충돌하려면 `Collider`가 필요한데, 지금 hand-equip 프리팹들(flashlight 등)엔 없어 보임. 콜라이더 없는 아이템은 던지면 바닥을 그냥 통과함 — 필요하면 아이템 프리팹에 직접 Collider 추가해야 함 (자동으로 안 붙임, 모양 모르는 채로 임의 콜라이더 붙이면 더 이상해질 수 있어서)
- 던진 뒤 다시 주울 수 있게(Interactable 재활성화) 하는 기능은 요청에 없어서 스킵. 필요하면 별도 요청
- `throwForce`는 임의 기본값 10 — Inspector에서 튜닝

## 적용 결과
계획대로 적용함:
- `InventorySystem.cs`에 `throwPos`/`throwForce` 필드, F키 입력, `ThrowActiveItem()` 추가
- `ThrowPos` GameObject를 `PlayerCameraRoot`(Player 프리팹 내부, fileID `1771932452`) 자식으로 생성, 로컬 위치 `(0, -0.2, 0.5)` (카메라 앞쪽 살짝 아래)로 배치
- `InventorySystem`의 `Throw Pos` 필드에 연결, `Throw Force`는 기본값 10

Unity 에디터를 직접 실행해 테스트하진 못했음 — 씬 YAML을 직접 편집한 방식이라, 에디터에서 열어서 F키 던지기가 실제로 의도대로 작동하는지, ThrowPos 위치가 적절한지 확인 필요.

## 요약 / 남은 작업
- 변경 파일: `Assets/My/Scripts/Inventory/InventorySystem.cs`, `Assets/Scenes/InGame.unity`(ThrowPos 추가 + 필드 연결)
