# 0031 - 아웃라인 상호작용이 반응하지 않는 문제 (카메라 참조 수정)

## 날짜
2026-08-20

## 요청 내용 (원문)
> 물건에 Interaction 레이어를 주고
> 언터렉션 마스크도 해당 레이어로 설정했는데 쳐다보는데 외곽선이 안생겨

## 조사 내용
- `ProjectSettings/TagManager.asset`에 `Interaction` 레이어(11번)가 추가되어 있고, 씬에서 `FlashLight_low-Poly` 오브젝트(프리팹 `Assets/AssetsFolder/Low-Poly FlashLight/.../FlashLight_low-Poly.prefab`)의 레이어가 11로 오버라이드되어 있음을 확인. 이 프리팹은 `MeshRenderer`와 `BoxCollider`(트리거 아님)가 같은 오브젝트에 있어 레이캐스트 대상으로 문제없음.
- `InteractionOutline.cs`에 배치된 `interactMask`의 `m_Bits` 값은 2048(=2^11) → `Interaction` 레이어와 정확히 일치. 레이어/마스크 설정 자체는 정상.
- 문제는 카메라 쪽. `Assets/Scenes/InGame.unity`를 확인한 결과, 씬에서 `MainCamera` 태그를 가진 오브젝트는 StarterAssets `MainCamera.prefab` 인스턴스 단 하나뿐인데, 이 인스턴스가:
  - `m_TransformParent: {fileID: 0}` → 플레이어 밑이 아니라 씬 루트에 별도 배치됨
  - 위치가 고정값(-14.232964, 1.875, 18), 회전도 고정 → 플레이어를 따라다니지 않음
  - `m_TargetTexture`가 `Assets/My/InGame/RenderTexture/Posterize.renderTexture`로 설정됨 → 화면이 아니라 별도 렌더텍스처로 출력 중 (포스터라이즈 이펙트용으로 보임)
  - `m_CullingMask.m_Bits`도 4055로 별도 커스터마이즈됨
- 즉 `Camera.main`(태그 검색)으로 얻어지는 카메라가 실제 플레이어 시점 카메라가 아니라, 화면에 보이지 않는 고정 위치의 포스터라이즈용 카메라임. `InteractionOutline.Awake()`가 이 카메라를 캐싱해서 화면 중앙 기준으로 레이캐스트를 쏘다 보니, 플레이어가 실제로 보는 방향과 전혀 다른 곳으로 레이가 나가서 아무 물건에도 맞지 않음.
- 사용자에게 두 가지 해결 방향(①카메라 필드 직접 연결 ②MainCamera 태그 재정리)을 제시했고, **①카메라 필드 직접 연결**을 선택함 — 포스터라이즈 카메라/태그 설정은 건드리지 않고, `InteractionOutline`이 어떤 카메라를 참조할지 인스펙터에서 직접 지정하도록 변경.

## 계획

### `Assets/My/Scripts/Player/InteractionOutline.cs` 수정
```diff
     [SerializeField] private float interactDistance = 3f;
     [SerializeField] private Material outlineMaterial;
     [SerializeField] private LayerMask interactMask = ~0;
+    [SerializeField] private Camera playerCamera;

-    private Camera cam;
     private Renderer currentRenderer;

-    private void Awake()
-    {
-        cam = Camera.main;
-    }
-
     private void Update()
     {
         Renderer hitRenderer = null;

-        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
+        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
```
- `Camera.main` 태그 검색을 없애고, 인스펙터에서 실제 플레이어 시점 카메라를 직접 드래그해서 연결하는 방식으로 변경. 어떤 오브젝트가 `MainCamera` 태그를 갖고 있든 상관없이 정확한 카메라를 쓰게 됨.

## 결과
승인 후 계획대로 적용 완료.

## 남은 작업 (씬 작업, 사용자 진행)
1. 씬의 `InteractionOutline` 컴포넌트(PlayerCapsule에 부착됨) 인스펙터에서 `Player Camera` 필드에 **실제로 화면에 렌더링되는 플레이어 시점 카메라**를 연결. (현재 씬에 `MainCamera` 태그 카메라 외에 다른 카메라가 안 보이므로, 플레이어가 실제로 보는 화면을 담당하는 카메라가 어떤 오브젝트인지 직접 확인해서 연결 필요 — 못 찾겠으면 알려주면 같이 확인)

## 변경된 파일
- `Assets/My/Scripts/Player/InteractionOutline.cs` — `Camera.main` 대신 인스펙터에서 직접 연결하는 `playerCamera` 필드 사용
