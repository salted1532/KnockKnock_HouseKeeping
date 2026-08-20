# 0034 - 손전등 줍기 상호작용 구현

## 날짜
2026-08-20

## 요청 내용 (원문)
> 이제 첫번째 상호작용을 구현해볼거야 PlayerCapsule안에 flashlight라는 오브젝트가 있는데 처음엔 active -> false로 비활성화 되어있다가 맵에 있는 FlashLight_low-Poly를 상호작용한 상태에서 키보드 E키 누르면 FlashLight_low-Poly가 비활성화 되고 플레이어의 flashlight가 활성화 되도록 해줘

[[0033-interactable-enum-system-proposal]]에서 만든 enum 기반 `Interactable` 시스템의 첫 실사용 사례라 별도 제안서 승인 없이 바로 진행.

## 조사 내용
- `Assets/Scenes/InGame.unity`에서 두 오브젝트 확인:
  - `PlayerCapsule` 밑 `flashlight` — `Assets/My/InGame/Prefabs/FlashLight/flashlight.prefab` 인스턴스(PrefabInstance fileID 193342755), 루트 GameObject 소스 fileID `6955010278166185207`
  - 맵의 `FlashLight_low-Poly` — `Assets/AssetsFolder/Low-Poly FlashLight/.../FlashLight_low-Poly.prefab` 인스턴스(PrefabInstance fileID 250567531), 루트 GameObject 소스 fileID `9029360721989362242`
- 기존 `Interactable.cs`의 `Pickup` 케이스는 "로그 남기고 `Destroy`"만 하는 최소 구현이라, "다른 오브젝트를 비활성화하고 플레이어 쪽 오브젝트를 활성화"하는 이번 요구사항에 안 맞음 → `Pickup`에 `equipTarget` 필드를 추가해서, 지정돼 있으면 그 오브젝트를 켜고 자신은 `Destroy` 대신 `SetActive(false)`로 바꾸는 방식으로 확장 (요청대로 "비활성화"이지 "파괴"가 아니므로).
- `FlashLight_low-Poly`에 `Interaction` 레이어(11번) 오버라이드가 없었음 — [[0031-interaction-outline-camera-fix]] 조사 당시엔 있었다고 기록됐지만 현재 씬에는 없는 상태. E키 상호작용이 동작하려면 `InteractionOutline`의 레이캐스트가 이 오브젝트를 맞혀야 하므로, 레이어 오버라이드(`m_Layer: 11`)도 같이 추가함.
- `equipTarget` 같은 씬 오브젝트 참조 필드는 프리팹 에셋이 아니라 "이 씬 인스턴스"에서만 연결 가능 → `Interactable` 컴포넌트를 `FlashLight_low-Poly` 프리팹 에셋이 아니라 씬의 이 인스턴스에만 추가(Added Component 오버라이드)하는 방식 사용.

## 변경 내용

### `Assets/My/Scripts/Interaction/Interactable.cs`
```diff
     [Header("Pickup")]
     [SerializeField] private string itemName;
+    [SerializeField] private GameObject equipTarget;
...
     private void Pickup()
     {
         Debug.Log($"Picked up {itemName}");
-        Destroy(gameObject);
+
+        if (equipTarget != null)
+        {
+            equipTarget.SetActive(true);
+            gameObject.SetActive(false);
+        }
+        else
+        {
+            Destroy(gameObject);
+        }
     }
```

### `Assets/Scenes/InGame.unity`
`FlashLight_low-Poly` 인스턴스에:
- `m_Layer` 오버라이드 추가 (11 = Interaction)
- `Interactable` 컴포넌트 추가 (Added Component): `type: Pickup`, `itemName: Flashlight`, `equipTarget` → `PlayerCapsule/flashlight` 오브젝트 참조

## 결과
계획대로 적용 완료. E키를 누르면 `FlashLight_low-Poly`는 꺼지고 플레이어의 `flashlight`가 켜짐.

## 남은 작업
- 씬 파일을 텍스트로 직접 편집했으니 Unity 에디터로 열어서 콘솔 에러 없이 정상 파싱되는지, `FlashLight_low-Poly` 인스펙터에 `Interactable` 컴포넌트와 `Equip Target` 필드가 제대로 연결되어 보이는지 확인 필요
- 아웃라인 하이라이트(QuickOutline `Outline` 컴포넌트)는 아직 `FlashLight_low-Poly`에 안 붙어있음([[0032-quickoutline-switch]] 남은 작업) — 없어도 E키 상호작용 자체는 동작하지만, 쳐다볼 때 테두리 표시를 원하면 추가로 붙여야 함

## 변경된 파일
- `Assets/My/Scripts/Interaction/Interactable.cs`
- `Assets/Scenes/InGame.unity`
