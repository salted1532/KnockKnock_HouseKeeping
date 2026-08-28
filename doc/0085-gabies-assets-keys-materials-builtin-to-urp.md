# 0085 - Gabies_Assets 머티리얼 깨진 것 수정 (Built-in → URP)

## 요청
`Assets/Gabies_Assets` 안 머티리얼 깨진(마젠타) 것 수정.

## 조사
대상: `Assets/Gabies_Assets/Keys/Materials/` 아래 `.mat` 6개 전부.
- Key Simple 02 / 02 Eyelets / 02 Tag
- Key Simple 03 / 03 Eyelets / 03 Tag

원인: 전부 **Built-in Standard** 셰이더 참조 (`m_Shader: {fileID: 46, guid: 0000000000000000f000000000000000}`) — URP 프로젝트라 마젠타로 깨짐. [[project_hdrp-asset-packs-need-urp-conversion]]와 동일 패턴 (이번엔 HDRP가 아니라 Built-in).

6개 전부 불투명(Opaque, `_Mode:0`, `_SrcBlend:1 _DstBlend:0 _ZWrite:1`), 구성도 동일:
- `_MainTex`(Albedo), `_BumpMap`(Normal), `_MetallicGlossMap`, `_OcclusionMap`, `_ParallaxMap` 사용
- Emission 없음, `_Color` 는 전부 흰색(1,1,1,1)
- 머티리얼마다 `_GlossMapScale`(0.72~1), `_OcclusionStrength`(0~0.4)만 다름 — 텍스처가 실제 스무스니스/AO를 공급하므로 이 값들은 그대로 유지 ([[feedback_material-smoothness-default-zero]]의 "신규 머티리얼 기본값 0" 규칙은 새로 만드는 머티리얼용이고, 여기는 아티스트가 준 기존 GlossMap 데이터라 보존)

URP Lit GUID (프로젝트 표준): `933532a4fcc9baf4fa0491de14d08ed7`

## 계획
`Assets/My/Materials/.../WoodPlanks.mat`을 템플릿으로 6개 전부 동일하게 변환. `.meta`는 안 건드림(GUID 유지 → 프리팹 참조 보존).

- `m_Shader` → `{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}`
- URP AssetVersion MonoBehaviour 블록 추가
- `_MainTex` 값을 `_BaseMap`에도 복사 (텍스처 그대로 유지)
- `_Color` → `_BaseColor`에도 복사 (흰색이라 그대로)
- `_BumpMap`/`_MetallicGlossMap`/`_OcclusionMap`/`_ParallaxMap`은 기존 텍스처+값 그대로 유지
- `_GlossMapScale`/`_OcclusionStrength`/`_Parallax`는 머티리얼별 기존 값 그대로 유지
- URP 필수 float 보충: `_Surface:0`, `_Blend:0`, `_Cull:2`, `_WorkflowMode:1`, `_EnvironmentReflections:1`, `_ReceiveShadows:1`, `_SpecularHighlights:1`, `_QueueOffset:0` 등 (WoodPlanks.mat 기준)
- `m_ValidKeywords`: `_NORMALMAP`, `_METALLICSPECGLOSSMAP`, `_PARALLAXMAP` (URP 프로퍼티명 기준으로 정리, `_METALLICGLOSSMAP`는 Built-in 전용이라 드롭)

## 리스크
- 낮음 — 6개 다 같은 패턴(불투명, 파티클/투명 아님), 참고 사례([[project_hdrp-asset-packs-need-urp-conversion]], doc/0084)와 동일.
- Unity 에디터가 열려있으면 저장 즉시 리임포트되며 키워드가 자동 재검증됨(0084 사례 참고).

## 결과 (2026-08-28, 승인 후 적용)
계획대로 6개 `.mat` 전부 `m_Shader`를 URP Lit로 교체하고 URP AssetVersion 블록 추가, `_BaseMap`/`_BaseColor` 보충. 텍스처 GUID·`_GlossMapScale`·`_OcclusionStrength`·`_Parallax`는 머티리얼별 기존 값 그대로 유지. `.meta`는 손대지 않아 프리팹 참조 보존됨.

## 검증
- Built-in 셰이더 참조 0개, URP Lit 6개.
- 남은 확인: 프로젝트를 열어 Keys 프리팹/모델에서 마젠타 사라졌는지 눈으로 확인.

## 상태
2026-08-28 완료.
