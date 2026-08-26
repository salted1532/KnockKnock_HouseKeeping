# 0071 - Glass 머티리얼 생성

## 요청
`Assets/My/InGame/Material` 폴더에 유리처럼 보이는 반투명 머티리얼(`Glass.mat`) 생성.

## 조사
- 기존 `Assets/My/InGame/Material/1x1 UV Blue.mat` 참고: 프로젝트의 URP Lit 셰이더 GUID(`933532a4fcc9baf4fa0491de14d08ed7`) 확인.
- 기존 `Assets/AssetsFolder/Flashlight/Flashlight/Materials/UnityLit_FakeVolumetric.mat` 참고: 이 프로젝트에서 Transparent Surface Type을 쓸 때 Unity가 실제로 채우는 필드(`_Surface: 1`, `disabledShaderPasses`, `stringTagMap.RenderType: Transparent`, `m_CustomRenderQueue: 3000`, `m_ValidKeywords: [_SURFACE_TYPE_TRANSPARENT]`) 확인.
- 유리는 매끈한 표면이라 반사가 보여야 하므로 Smoothness는 기존 규칙(신규 머티리얼 기본 0)의 예외로 높게(0.95) 설정. Metallic은 0 유지(유리는 금속이 아님).

## 계획 (신규 파일 생성이라 diff 아님 — 새로 만들 파일 내용)

`Assets/My/InGame/Material/Glass.mat` (신규):
- Shader: Universal Render Pipeline/Lit (기존 프로젝트와 동일 GUID)
- Surface Type: Transparent (`_Surface: 1`)
- Blend Mode: Alpha (`_Blend: 0`, `_SrcBlend: 5`(SrcAlpha), `_DstBlend: 10`(OneMinusSrcAlpha), `_ZWrite: 0`)
- Cull: Back (`_Cull: 2`, 일반적인 창문/판유리 기준. 양면 다 보여야 하면 추후 0(Both)으로 변경 요청 가능)
- Metallic: 0, Smoothness: 0.95 (유리라서 예외적으로 높게)
- BaseColor: 옅은 하늘색 틴트, 낮은 알파 `{r:0.85, g:0.93, b:1, a:0.15}`
- RenderType: Transparent, RenderQueue: 3000
- `.meta` 파일도 신규 GUID로 함께 생성

## 승인 필요
위 스펙대로 `Glass.mat` + `.meta` 생성 진행해도 될까요? (Cull=Back / Smoothness=0.95 / 알파=0.15 값 조정 필요하면 알려주세요)
