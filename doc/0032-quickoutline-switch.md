# 0032 - QuickOutline 에셋으로 아웃라인 방식 교체

## 날짜
2026-08-20

## 요청 내용 (원문)
> 맞아 현재 카메라는 포스터라이즈 렌더텍스쳐용인데 현재 그래서 UI용 카메라를 연결시키니깐 아웃라인이 나오긴하네
> 근데 아웃 라인이 오브젝트의 완전 외곽만 나오는게 아니라 면마다 라인이 나와서 붕 떠이는거 처럼 보이네
> 내가 QuickOutline이라는 외곽선 에셋을 임포트 해놨이깐 이걸 이용해서 만들어줄래 쉐이더랑 머티리얼 둘다 있는거 같아 확인해보고 적용시켜줘

## 조사 내용
- 카메라 문제(0031)는 UI 카메라 연결로 해결됨. 이번 요청은 아웃라인 품질 문제: 직접 만든 `Outline.shader`(노멀 방향으로 정점 확장 + Cull Front)는 **버텍스(정점) 노멀**을 그대로 쓰기 때문에, 각 폴리곤 면 경계마다 노멀이 꺾여서 면 단위로 선이 따로 떠 보이는 현상(하드 엣지) 발생 — 예상된 한계.
- `Assets/AssetsFolder/QuickOutline/` 확인 결과 Chris Nolet의 QuickOutline 에셋이 이미 임포트되어 있음:
  - `Scripts/Outline.cs` — 오브젝트에 붙이는 `Outline` 컴포넌트. `Awake()`에서 스무스 노멀(같은 위치의 버텍스 노멀을 평균 내서 부드럽게 이어지는 노멀)을 계산해 UV3에 저장 → 면 경계에서 선이 끊기지 않고 매끈하게 이어짐. `OnEnable`/`OnDisable`에서 렌더러의 `sharedMaterials` 뒤에 마스크+필 머티리얼 2장을 붙였다 뗐다 함 (우리가 만든 방식과 원리는 비슷하지만 스텐실 마스크로 본체를 가려서 겹치는 부분 처리가 더 정교함).
  - `Resources/Materials/OutlineMask.mat`, `Resources/Materials/OutlineFill.mat` — 각각 `Resources/Shaders/OutlineMask.shader`, `OutlineFill.shader` 사용. `Outline.cs`가 `Resources.Load`로 직접 불러와 인스턴스화하므로 별도 연결 작업 불필요.
  - 두 쉐이더 모두 `LightMode` 태그가 없는 일반 `CGPROGRAM` 패스 → URP에서는 기본적으로 `SRPDefaultUnlit`로 취급되어 URP 렌더러가 그대로 그려줌 (URP 프로젝트에서도 별도 포팅 없이 동작하는 것으로 널리 알려진 방식). 별도 URP 전용 포팅 불필요.
- 결론: 우리가 만든 `Assets/My/InGame/Material/Outline.shader` / `Outline.mat`은 QuickOutline으로 대체되어 더 이상 필요 없음 → 삭제. `InteractionOutline.cs`는 렌더러 머티리얼을 직접 조작하던 방식에서, 대상 오브젝트에 미리 붙여둔 QuickOutline `Outline` 컴포넌트의 `enabled`를 켜고 끄는 방식으로 변경.

## 계획

### 1. 삭제 (더 이상 사용하지 않음)
- `Assets/My/InGame/Material/Outline.shader` (+ `.meta`)
- `Assets/My/InGame/Material/Outline.mat` (+ `.meta`)

### 2. `Assets/My/Scripts/Player/InteractionOutline.cs` 전면 수정
```csharp
using UnityEngine;

public class InteractionOutline : MonoBehaviour
{
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private Camera playerCamera;

    private Outline currentOutline;

    private void Update()
    {
        Outline hitOutline = null;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            hitOutline = hit.collider.GetComponentInParent<Outline>();
        }

        if (hitOutline == currentOutline)
            return;

        if (currentOutline != null)
            currentOutline.enabled = false;
        if (hitOutline != null)
            hitOutline.enabled = true;

        currentOutline = hitOutline;
    }
}
```
- `outlineMaterial` 필드 제거 (QuickOutline이 자체적으로 `Resources`에서 머티리얼을 불러오므로 불필요). 씬에 이미 저장된 `outlineMaterial` 참조 값은 필드 삭제 후 Unity가 다음 저장 시 자동으로 정리함 (씬 파일 직접 수정 불필요).
- 대상 오브젝트를 껐다 켰다 하는 것도 `Renderer.sharedMaterials` 대신 `Outline` 컴포넌트의 `enabled` 토글로 바뀜 → 스무스 노멀/스텐실 마스크 처리는 QuickOutline이 알아서 함.

## 결과
승인 후 계획대로 적용 완료.

## 남은 작업 (씬 작업, 사용자 진행)
1. 아웃라인을 표시하고 싶은 물건(우선 `FlashLight_low-Poly`)에 `Outline` 컴포넌트(`Assets/AssetsFolder/QuickOutline/Scripts/Outline.cs`)를 부착.
   - Outline Mode: 기본은 `OutlineVisible` 추천 (가려진 부분은 안 그려짐)
   - Outline Color / Width: 원하는 대로 조정
   - 컴포넌트의 **Enabled 체크는 꺼둘 것** (평소엔 꺼져있다가 `InteractionOutline`이 쳐다볼 때만 켜줌)
2. `InteractionOutline` 컴포넌트(PlayerCapsule)의 `interactMask`가 여전히 해당 오브젝트들의 레이어(Interaction)를 가리키는지 확인 (이번 수정으로 필드 값 자체는 안 바뀜).

## 변경된 파일
- `Assets/My/InGame/Material/Outline.shader`, `.mat` (+ `.meta`) — 삭제
- `Assets/My/Scripts/Player/InteractionOutline.cs` — QuickOutline `Outline` 컴포넌트를 켜고 끄는 방식으로 전면 수정
