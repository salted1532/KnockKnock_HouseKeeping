# 0076 - 스왑된 메쉬(침대/커튼)에 외곽선이 안 생기는 문제

## 증상
TidyBed, Curtain 상호작용에서 **처음 활성 메쉬는 외곽선이 나오는데, 상호작용으로 다른 메쉬로 바뀌면 외곽선이 안 나옴.**

## 원인
`InteractionOutline.cs`는 매 프레임 카메라 중앙 레이캐스트 →
`hit.collider.GetComponentInParent<Outline>()` 로 외곽선 컴포넌트를 찾는다.

- `GetComponentInParent<T>()`는 **자기 자신 + 상위(부모) 계층만** 검색한다. 형제/자식은 못 찾음.
- `Outline`(QuickOutline)은 `Awake()`에서 `GetComponentsInChildren<Renderer>()` 로 렌더러를 캐시하는데, **기본 오버로드는 비활성 오브젝트를 제외**한다.

즉 스왑되는 두 메쉬 중 **처음에 켜져 있는 쪽에만** `Outline`(또는 콜라이더)이 붙어 있으면:
- 스왑 후 그 오브젝트는 꺼지고 → 레이가 새 메쉬를 맞혀도 그 위/자신에 `Outline`이 없어 `null` → 외곽선 없음.
- 또는 `Outline`을 공통 부모에 두면, `Awake` 시점에 꺼져 있던 두 번째 메쉬의 렌더러가 캐시에서 빠져 → 그 메쉬엔 외곽선 머티리얼이 안 붙음.

## 해결 (택1)

### B안 — 프리팹 배선만 수정 (코드 변경 없음, 권장)
스왑되는 **각 메쉬 오브젝트마다** 다음을 붙인다:
1. `Collider` (레이캐스트 대상)
2. `Outline` 컴포넌트
   - `enabled` 체크 해제 (평소엔 안 보이게, `InteractionOutline`이 바라볼 때만 켬)
   - **`Precompute Outline` 체크** — 안 하면 그 메쉬가 처음 활성화될 때 런타임에 노멀을 굽느라 살짝 끊김
3. `Interactable`은 두 메쉬의 공통 부모에 그대로 둔다.

동작: 스왑 후 새 메쉬가 켜지면 자기 콜라이더로 레이를 맞고, 자기 자신의 `Outline`을 `GetComponentInParent`가 찾아 켠다. `InteractionOutline`은 `Interact()` 후 `currentOutline`을 null로 비우므로 다음 프레임에 새 것을 잡음(1프레임 공백, 체감 없음).

### A안 — QuickOutline 수정 + Outline을 부모로
`Assets/AssetsFolder/QuickOutline/Scripts/Outline.cs`:
- `Awake()`: `GetComponentsInChildren<Renderer>()` → `GetComponentsInChildren<Renderer>(true)`
- `LoadSmoothNormals()` / `Bake()`: `GetComponentsInChildren<MeshFilter>()` → `(true)`, `GetComponentsInChildren<SkinnedMeshRenderer>()` → `(true)`

그러면 `Outline` 하나를 공통 부모(= `Interactable`과 같은 오브젝트, 콜라이더도 여기)에 두고 두 메쉬를 자식으로 두면, 비활성 메쉬의 렌더러까지 외곽선 머티리얼이 붙어 스왑돼도 유지됨.
- 장점: 변형 메쉬가 많아도 컴포넌트 하나로 끝.
- 단점: 서드파티 스크립트 수정(업데이트 시 재적용 필요), 항상 두 메쉬 머티리얼에 외곽선 패스가 붙어 있음(미세 오버헤드).

## 권장
**B안** (침대/커튼 각 2개씩이면 컴포넌트 추가가 더 간단하고 안전). 변형 메쉬가 앞으로 많아지면 그때 A안.

## 진행 (2026-08-27)
B안 검토 중 실제 프리팹 구조 확인: `Motel_Room.prefab`의 `curtain` 루트(`&2834467152334571446`)에
`Interactable`(type=Curtain) + `Outline`(비활성) + `BoxCollider`가 모두 있고, 커튼 메쉬 2개(`curtain_1`/`curtain_2`)는
**중첩 프리팹 인스턴스**(한쪽은 시작 시 비활성). BoxCollider는 루트 하나로 두 메쉬 공용.
→ B안(변형 메쉬마다 Outline+Collider)은 중첩 프리팹 오버라이드 + Precompute 굽기가 필요해 부적합.
→ **A안 채택** ("진행해줘").

### 적용: `Assets/AssetsFolder/QuickOutline/Scripts/Outline.cs` (4곳, 비활성 자식 포함)
- `Awake()`: `GetComponentsInChildren<Renderer>()` → `GetComponentsInChildren<Renderer>(true)`
- `Bake()`: `GetComponentsInChildren<MeshFilter>()` → `(true)`
- `LoadSmoothNormals()`: `GetComponentsInChildren<MeshFilter>()` → `(true)`, `GetComponentsInChildren<SkinnedMeshRenderer>()` → `(true)`

### 에디터 세팅 (사용자)
- 커튼: 이미 됨 (Outline이 `curtain` 루트에 있음). 그대로 동작.
- 침대(TidyBed): `Outline`을 침대 **루트**(messy/tidy 메쉬의 공통 부모, 콜라이더도 그쪽)에 두면 동일하게 해결. 현재 messy 메쉬에만 있으면 루트로 옮길 것.

### 주의
- 서드파티 스크립트 수정 → QuickOutline 재임포트/업데이트 시 재적용 필요. 파일 상단에 표시 없음, 이 문서로 추적.
- 두 변형 메쉬 머티리얼에 항상 outline mask/fill 패스가 추가됨(안 보일 땐 렌더 안 되지만 material 인스턴스는 상주).
