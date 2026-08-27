# 0084 - Pekdata CRT Monitor 머티리얼 깨진 것 수정 (Built-in → URP)

## 요청
`Assets/AssetsFolder/Pekdata` 안 머티리얼 깨진(마젠타) 것 수정.

## 조사
대상: `Assets/AssetsFolder/Pekdata/PekdataCRTMonitor/` 아래 `.mat` 12개.

원인: **Built-in RP** 셰이더 참조가 남아 있음 (URP 프로젝트라 마젠타).
- 9개: `m_Shader: {fileID: 46, guid: 0000000000000000f000000000000000}` = **Built-in Standard**
- 3개(smokeparticles/sparks/whitesmokeparticlesmaterial): `{fileID: 200, ...}` = **Built-in Particles**

일부는 예전에 URP 컨버터가 돌아 URP 프로퍼티(`_BaseMap`/`_BaseColor`/`_Surface`/`_WorkflowMode`)와 AssetVersion 블록이 이미 들어가 있으나 `m_Shader` 만 안 바뀐 상태. 나머지(FBX 임베디드 `Material`/`Material.001`/`No Name`/`normal`, `defaultMaterial`~`3`)는 순수 Standard.

URP 셰이더 GUID (패키지에서 확인):
- URP Lit: `933532a4fcc9baf4fa0491de14d08ed7`
- URP Particles Unlit: `0406db5a14f94604a8c57ccfbc9f3b46`

Pekdata 는 예제 씬/프리팹(`CRTMonitor.prefab`)에서만 쓰이고 `Assets/My` 게임 콘텐츠에서 참조 없음.

## 계획

각 `.mat` 의 `m_Shader` 를 교체 + 정리. `.meta`(GUID) 는 안 건드림 → 참조 유지.

### A. Standard → URP Lit (9개, 불투명)
`CRTMonitorMAterial`, `CRTMonitorScreenOnMaterial`, `defaultMaterial`, `defaultMaterial2`, `defaultMaterial3`, `Material`, `Material.001`, `No Name`, `normal`

- `m_Shader` → `{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}`
- `_BaseMap` 없으면 `_MainTex` 값 복사, `_BaseColor` 없거나 회색 자동세팅이면 `_Color` 값으로 맞춤
- `m_ValidKeywords` 를 실제 맵에 맞게: `_NORMALMAP`(BumpMap), `_METALLICSPECGLOSSMAP`(MetallicGlossMap), `_OCCLUSIONMAP`(OcclusionMap), `_EMISSION`(EmissionMap+색), 반사 끔이면 `_ENVIRONMENTREFLECTIONS_OFF`
- `m_InvalidKeywords` 비움
- opaque 세팅 확인: `_Surface: 0`, `_Blend: 0`, `_SrcBlend: 1`, `_DstBlend: 0`, `_ZWrite: 1`, `_Cull: 2`

개별:
| 머티리얼 | 맵 | 비고 |
|---|---|---|
| CRTMonitorMAterial | Diffuse/Normal/Metallic/AO | 본체. `_BaseColor` → `_Color`(흰색)로 |
| CRTMonitorScreenOnMaterial | BaseColor=EmissionMap | 화면. `_EMISSION` + `_EmissionColor {1,1,1,1}` 유지 |
| defaultMaterial~3, Material, Material.001, No Name, normal | 없음 | `_BaseColor` = 기존 `_Color` 값 |

### B. Particles → URP Particles Unlit (3개, 투명)
`smokeparticles`, `sparks`, `whitesmokeparticlesmaterial`

- `m_Shader` → `{fileID: 4800000, guid: 0406db5a14f94604a8c57ccfbc9f3b46, type: 3}`
- URP 파티클 프로퍼티는 이미 다 있음(`_ColorMode`/`_FlipbookBlending`/`_SoftParticlesEnabled`/`_DistortionEnabled`/`_BaseColor`/`_BaseMap` 등)
- `m_InvalidKeywords` → `m_ValidKeywords` 로 이동 (URP 파티클도 지원: `_FLIPBOOKBLENDING_ON`, `_COLORADDSUBDIFF_ON`). `_RECEIVE_SHADOWS_OFF` 는 Unlit 에 불필요 → 드롭
- 블렌딩 유지: smoke/white = 알파(`_SrcBlend:5 _DstBlend:10`), sparks = `_Blend: 2`(가산) 확인
- `disabledShaderPasses` 의 `SHADOWCASTER` 유지

## 리스크
- 파티클 머티리얼은 키워드/블렌딩 조합이 미묘 → 임포트 후 예제 씬에서 연기/스파크 확인, 이상하면 인스펙터에서 셰이더를 다시 한 번 선택하면 URP 가 키워드 자동 정리.
- FBX 임베디드 머티리얼 4개는 CRT 메시가 실제로 안 쓸 가능성(진짜 본체는 `CRTMonitorMAterial`) — 그래도 마젠타 방지 위해 같이 변환.

## 결과 (2026-08-27, "고쳐줘")

- 12개 `.mat` 의 `m_Shader` 교체:
  - Standard(fileID 46) 9개 → URP Lit (`933532a4...`)
  - Particles(fileID 200) 3개 → URP Particles Unlit (`0406db5a...`)
- `_BaseColor` 없던 7개(FBX 임베디드 4 + defaultMaterial 3)에 `_Color` 값 복사.
- `CRTMonitorMAterial` — `_BaseColor`/`_Color` 를 `{0.5}` → `{1,1,1,1}` (본체 텍스처 반감 방지).
- **Unity 가 백그라운드 실행 중이라, `m_Shader` 변경 즉시 리임포트하며 키워드 자동 재검증** — `_METALLICSPECGLOSSMAP`/`_OCCLUSIONMAP`/`_ENVIRONMENTREFLECTIONS_OFF`/`_SURFACE_TYPE_TRANSPARENT`/`_COLORADDSUBDIFF_ON` 등이 valid 로 자동 정리됨. URP AssetVersion MonoBehaviour 블록도 자동 추가. Built-in 잔여 키워드(`_GLOSSYREFLECTIONS_OFF`, `_RECEIVE_SHADOWS_OFF` 등)는 `m_InvalidKeywords` 에 남지만 무해(무시됨).

## 검증
- Built-in 셰이더 참조 0, URP Lit 9 / URP Particles 3.
- 12개 전부 `_BaseColor` 존재, YAML 정상(각 2 도큐먼트).
- `CRTMonitor.prefab` 은 `CRTMonitorMAterial`/`CRTMonitorScreenOnMaterial`/`smokeparticles`/`sparks` 사용 — 전부 변환됨.
- 남은 확인: 예제 씬 `CRTMonitorExampleScene` 에서 연기/스파크 블렌딩 눈으로 확인. 이상하면 인스펙터에서 셰이더 재선택.

## 상태
2026-08-27 완료.
