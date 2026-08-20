# 0030 - 상호작용 오브젝트 아웃라인(외곽선) 하이라이트 제안

## 날짜
2026-08-20

## 요청 내용 (원문)
> 물건과 상호작용 기능을 구현하려고 하는데
> 화면 가운데의 크로스 헤어를 둘태니깐
> 화면 가운데에 레이캐스트 쏴서
> 물건에 닿으면 닿은 물건의 outline 외곽선이 생기도록 해줄래 외곡선 쉐이더나 머티리얼을 따로 만들어 두는게 좋을거 같아
> 계속 재사용하기 좋게 만들게 되면 inGame폴더 안에 Material폴더 안에 생성해줘 쉐이더나 머티리얼

## 조사 내용
- 렌더 파이프라인: `Packages/manifest.json`에 `com.unity.render-pipelines.universal 17.4.0` 확인 → URP 사용 중. 쉐이더는 URP 호환(HLSL, `UniversalPipeline` 태그)으로 작성해야 함.
- 재사용 가능한 산출물 저장 위치: `Assets/My/InGame/Material/`가 이미 존재하고 다른 머티리얼들(`1x1 UV Blue.mat` 등)이 여기 모여 있음 → 요청대로 이 폴더에 쉐이더+머티리얼 생성.
- 카메라: `PlayerCapsule.prefab`은 Cinemachine 타겟만 가지고 있고 실제 렌더 카메라는 씬(`InGame.unity`)에 별도로 있음 → `Camera.main`으로 화면 중앙 레이캐스트하면 Cinemachine 여부와 무관하게 동작.
- 상호작용 대상 구분용 태그/레이어: `ProjectSettings/TagManager.asset` 확인 결과 "Interactable" 같은 전용 태그/레이어는 아직 없음(있는 건 발소리용 Wood/Concrete/Metal/Grass 레이어뿐). 이번 요청 범위는 "레이캐스트로 맞은 물건에 외곽선 표시"까지이므로, 상호작용 가능 여부를 구분하는 태그/레이어는 만들지 않고 레이캐스트에 맞은 모든 Renderer에 외곽선을 적용하는 방식으로 최소 구현. 나중에 "상호작용 가능한 물건만" 구분이 필요해지면 그때 레이어/태그 필터를 추가하면 됨.
- 기존 스크립트 스타일 참고: `Assets/My/Scripts/Player/FootstepSystem.cs` - Player 오브젝트에 붙는 단독 컴포넌트 형태를 그대로 따름.
- 외곽선 구현 방식: 별도 아웃라인 오브젝트 복제나 URP Renderer Feature(스텐실) 없이, 가장 널리 쓰이는 "인버티드 헐(inverted hull)" 방식 채택 - 노멀 방향으로 정점을 살짝 밀어내고 `Cull Front`로 뒷면만 그려서 실루엣만 남기는 단일 패스 쉐이더. 이 머티리얼을 감지된 오브젝트의 `Renderer.sharedMaterials` 배열 끝에 추가/제거하는 것만으로 켜고 끌 수 있어 렌더러 세팅(Renderer Feature 등) 수정이 불필요함.

## 계획

### 1. `Assets/My/InGame/Material/Outline.shader` (신규)
```hlsl
Shader "My/Outline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (1, 0.8, 0, 1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+1" }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionOS = IN.positionOS.xyz + IN.normalOS * _OutlineWidth;
                OUT.positionHCS = TransformObjectToHClip(positionOS);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
```
- 오브젝트의 기존 서브메시/머티리얼은 그대로 그려지고, 이 패스가 머티리얼 배열 맨 뒤에 추가로 붙어서 확대된 뒷면만 그려짐 → 테두리만 남는 전형적인 아웃라인 효과.

### 2. `Assets/My/InGame/Material/Outline.mat` (신규)
- 위 쉐이더를 사용하는 머티리얼. `_OutlineColor`, `_OutlineWidth` 인스펙터에서 바로 조절 가능(기본값: 주황색, 두께 0.02).

### 3. `Assets/My/Scripts/Player/InteractionOutline.cs` (신규)
```csharp
using System.Collections.Generic;
using UnityEngine;

public class InteractionOutline : MonoBehaviour
{
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private LayerMask interactMask = ~0;

    private Camera cam;
    private Renderer currentRenderer;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        Renderer hitRenderer = null;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            hitRenderer = hit.collider.GetComponentInParent<Renderer>();
        }

        if (hitRenderer == currentRenderer)
            return;

        SetOutline(currentRenderer, false);
        SetOutline(hitRenderer, true);
        currentRenderer = hitRenderer;
    }

    private void SetOutline(Renderer target, bool show)
    {
        if (target == null)
            return;

        var materials = new List<Material>(target.sharedMaterials);
        materials.Remove(outlineMaterial);
        if (show)
            materials.Add(outlineMaterial);
        target.sharedMaterials = materials.ToArray();
    }
}
```
- 매 프레임 화면 정중앙(크로스헤어 위치)에서 `interactDistance`(기본 3m)까지 레이캐스트.
- 맞은 물건이 바뀔 때만 이전 물건 아웃라인 제거 + 새 물건 아웃라인 추가 (매 프레임 배열을 새로 만들지 않아 가볍게 동작).
- `sharedMaterials` 사용 → 머티리얼 인스턴스가 새로 생성되지 않아 메모리 누수 없음.

## 남은 작업 (승인 후 씬 작업, 사용자 진행)
1. `InteractionOutline` 컴포넌트를 플레이어(카메라를 참조할 수 있는 오브젝트, 예: PlayerCapsule)에 부착
2. 인스펙터에서 `outlineMaterial`에 새로 만든 `Outline.mat` 연결
3. 필요하면 `interactDistance`, `interactMask` 조정 (기본은 모든 레이어 대상)

## 결과
승인 후 계획대로 적용 완료.

## 변경된 파일
- `Assets/My/InGame/Material/Outline.shader` (신규)
- `Assets/My/InGame/Material/Outline.mat` (신규, `Outline.shader` 참조, 기본 주황색/두께 0.02)
- `Assets/My/Scripts/Player/InteractionOutline.cs` (신규)
