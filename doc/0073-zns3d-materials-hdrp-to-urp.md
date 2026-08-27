# 0073 - ZNS3D 머티리얼 깨진 것 수정 (HDRP → URP 변환)

## 요청
`Assets/AssetsFolder/ZNS3D` 폴더 안 머티리얼들이 깨진(마젠타) 것 수정.

## 조사
- 대상: `Assets/AssetsFolder/ZNS3D/Vintage Living Room Game Pack/Materials/` 아래 `.mat` 13개.
- 원인: 이 에셋 팩("Vintage Living Room Game Pack")은 **HDRP용**으로 제작됨.
  - 셰이더 GUID `7252379db4c18b641b517f2c91bb57e1` (HDRP/Lit), `6e4ae4064600d784cac1e41a9e6f2e59` (HDRP/Lit 투명).
  - 상단 MonoBehaviour 블록도 HDRP 버전(`da692e001514ec24dbc4cca1949ff7e8`).
  - `_MaterialID`, `_DiffusionProfile`, `_StencilRef*`, `_BaseColorMap` 등 HDRP 전용 프로퍼티 다수.
- 이 프로젝트는 **URP 17.4.0** (Unity 6000.4.8f1). → HDRP 셰이더를 못 찾아서 마젠타로 렌더.
- 프로젝트 표준 URP Lit 셰이더 GUID: `933532a4fcc9baf4fa0491de14d08ed7` (기존 `Assets/My` 머티리얼 1500+개가 이걸 사용).
- 투명 머티리얼 참고: `Assets/AssetsFolder/Flashlight/Flashlight/Materials/UnityLit_FakeVolumetric.mat`.
- 텍스처 확인: 각 `Mat_*`에 3장 (BaseColor / Normal / Metallic). Normal은 이미 textureType:1(Normal map)로 임포트됨. 별도 Roughness 맵(`_SpecGlossMap`)도 있으나 URP Lit엔 Roughness 입력이 없어 사용 안 함.
- 프리팹들은 머티리얼을 `.meta` GUID로 참조 → `.mat` 내용만 고치면 프리팹도 같이 정상화됨. `.meta`는 건드리지 않음(GUID 유지).
- 현재 `Assets/My` 및 씬에서 ZNS3D 머티리얼을 참조하는 곳은 없음(아직 미배치).

## 계획

각 `.mat` 파일을 URP Lit 머티리얼로 **덮어쓰기**. 상단 MonoBehaviour 블록은 URP AssetVersion(`d0353a89b1f911e48b9e16bdc9f2e058`, version 10)으로 교체. `--- !u!21 &2100000` fileID 유지.

### A. 텍스처 있는 10개 (불투명)
`Mat-Antique_Light, Mat_Ceiling, Mat_Decors_1, Mat_Decors_2, Mat_Floors, Mat_Furnitures_1, Mat_Furnitures_2, Mat_Gramophone, Mat_Walls, Mat_Windows_Doors`

- Shader: URP/Lit (`933532a4fcc9baf4fa0491de14d08ed7`)
- `_BaseMap` + `_MainTex` ← 기존 BaseColor 텍스처 GUID
- `_BumpMap` ← 기존 Normal 텍스처 GUID, 키워드 `_NORMALMAP`, `_BumpScale: 1`
- `_MetallicGlossMap` ← 기존 Metallic 텍스처 GUID, 키워드 `_METALLICSPECGLOSSMAP`, `_Metallic: 1`, `_SmoothnessTextureChannel: 0`(메탈릭 알파)
- `_GlossMapScale: 0.5` — 메탈릭 PNG에 알파가 없으면 smoothness가 1.0로 읽혀 과하게 반질거리므로 0.5로 낮춤 (조정 노브. 너무 무광이면 0.7~1.0로 올리면 됨)
- `_BaseColor: {1,1,1,1}`, `_Metallic`(스칼라)은 맵이 제어하므로 1
- RenderType Opaque, queue -1

(텍스처 GUID 매핑 — 확인 완료:)
| 머티리얼 | BaseColor | Normal | Metallic |
|---|---|---|---|
| Mat-Antique_Light | b51ae97f20389384... | bcbf1b9cc4bcfc94... | 4d5638469b403bb4... |
| Mat_Ceiling | df950057aefe5094... | e45a88e597239324... | 31eeae285647a234... |
| Mat_Decors_1 | 850ade9d1c7bf634... | 97477f419d2de6c4... | c8211dc1127a8774... |
| Mat_Decors_2 | 3459b5b386c1e5c4... | 9b772f37664ca294... | fc62da4ffbe80c64... |
| Mat_Floors | da64138dec5af7c4... | 6bf8000041f0c9f4... | 0e06a5dc25eb7904... |
| Mat_Furnitures_1 | 3e47c6f284d21ca4... | b598efba6856a994... | 6e47f539ba966f64... |
| Mat_Furnitures_2 | 991728fe23fd20a4... | adfaebe883c63a44... | 1a63f5adcc813184... |
| Mat_Gramophone | dbb85f711baa62c4... | 8e6fbd2bc2ae7e24... | b1e35634a1278284... |
| Mat_Walls | 475749334dd98ec4... | 2fb3fdd4adfa0544... | dbeeb3532741d6a4... |
| Mat_Windows_Doors | 540446bdf32f5864... | ee1dbfdb5a123fb4... | b282ae2896d13494... |

### B. Glass, Glass 1 (투명, 텍스처 없음)
- Shader: URP/Lit, Surface Type Transparent (`_Surface: 1`, `_Blend: 0`, `_SrcBlend: 5`, `_DstBlend: 10`, `_ZWrite: 0`, queue 3000, 키워드 `_SURFACE_TYPE_TRANSPARENT`, RenderType Transparent, `disabledShaderPasses: [MOTIONVECTORS]`)
- 기존 값 유지: `_BaseColor` (Glass 1 `{0.840,0.953,0.936,0.631}`, Glass `{0.962,0.938,0.704,0.745}`), `_Metallic: 0.717`, `_Smoothness: 0.778` (유리라서 rule-0 예외)
- Glass 는 발광 램프 유리 → `_EmissionColor: {4.088,3.935,0.582}` (기존 HDR 값), 키워드 `_EMISSION`
- `_Cull: 2` (Back)

### C. New Material.mat (불투명, 텍스처 없음)
- URP/Lit 불투명, `_BaseColor: {0.226,0.226,0.226,1}`, `_Metallic: 0`, `_Smoothness: 0` (rule-0)
- ("New Material" 이름 그대로. 사용처 없으면 삭제도 가능 — 원하면 알려주세요)

## 리스크 / 노브
- 메탈릭 맵 알파 유무를 파일에서 확인 불가 → `_GlossMapScale: 0.5`로 시작. 임포트 후 씬에서 보고 조정.
- 별도 Roughness 맵은 URP Lit에서 못 씀(버림). 광택이 어색하면 머티리얼별 `_Smoothness`/`_GlossMapScale` 수동 조정.
- Glass 의 `_Metallic: 0.717`은 원본 값 그대로 둠(유리치곤 높음). 어색하면 0으로.

## 승인 및 결과 (2026-08-27, "진행시켜줘")
승인 3항목 모두 계획대로 진행 (GlossMapScale 0.5 / Glass 원본 수치 / New Material 유지).

생성 스크립트: `scratchpad/gen_mats.sh` (URP Lit 템플릿 3종 — opaque textured / plain / glass transparent).

13개 `.mat` 전부 덮어씀. 검증:
- HDRP 셰이더/AssetVersion GUID 잔존 0
- URP Lit 셰이더 GUID 13/13
- 구조는 프로젝트 표준 URP 머티리얼(`WoodPlanks.mat`, `UnityLit_FakeVolumetric.mat`)과 동일
- `.meta` 미변경 → 프리팹 참조 유지

**Unity에서 확인 필요**: 에디터로 프로젝트 열어 리임포트 후 씬에서 광택/투명 확인. 어색하면 머티리얼별 `_GlossMapScale`(현 0.5) / `_Smoothness` 조정.
