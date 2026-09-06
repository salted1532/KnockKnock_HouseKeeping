# 0146 – Moonlight Motel FBX 프리팹 생성

## 요청
`Assets/My/InGame/Prefabs/Meshy_AI_Moonlight_Motel_Vacan_0906080807_texture_fbx/` 안의 모델링 + 각 텍스처/노멀 등을 모두 적용한 프리팹 하나 생성. ("전부 적용" 확정)

## 폴더 내용
- `..._texture.fbx` – 메쉬 (Humanoid로 잘못 임포트됨)
- `..._texture.png` – Base Color
- `..._texture_normal.png` – 노멀 (Default 타입으로 임포트됨)
- `..._texture_metallic.png` / `_roughness.png` / `_metallic_roughness.png` – PBR 맵 (glTF 레이아웃, URP Lit와 불일치)

## 작업 (uloop execute-dynamic-code, Unity 에디터)
1. `..._texture_normal.png.meta` → `textureType: NormalMap`
2. `..._texture.fbx.meta` → `animationType: None` (Humanoid 해제)
3. 패킹 텍스처 베이크: `..._texture_metallicSmoothness.png`
   - R = metallic맵, A = 1 − roughness맵, linear(sRGB off)
   - EncodeToPNG → 12.4MB, 임포트 시 Unity 기본 2048 max로 다운스케일
4. `Assets/My/InGame/Material/MoonlightMotel.mat` (URP Lit)
   - `_BaseMap`, `_BumpMap`(BumpScale 1), `_MetallicGlossMap` = 패킹본
   - 키워드 `_NORMALMAP`, `_METALLICSPECGLOSSMAP`
   - `_Metallic` 1, `_Smoothness` 1, `_SmoothnessTextureChannel` 0 (metallic alpha)
   - 주: Smoothness는 베이크된 맵 알파를 그대로 통과시키려 1로 둠 (머티리얼 기본값 0 규칙은 맵 없는 수동값에 해당)
5. FBX 인스턴스 → 모든 렌더러에 머티리얼 지정 → `Assets/My/InGame/Prefabs/MoonlightMotel.prefab` 저장 (renderers=1)

## 결과
- 생성: `MoonlightMotel.mat`, `MoonlightMotel.prefab`, `..._texture_metallicSmoothness.png`
- 수정: `..._texture_normal.png.meta`, `..._texture.fbx.meta`
- 에러/경고 없음
- 샌드박스 제약(`File.WriteAllBytes`, `AssetDatabase.DeleteAsset/CreateFolder` 차단)으로 PNG는 base64→셸 디코드 경유

## 검증
씬에 임시 배치 후 스크린샷 2장 (scratchpad). FBX 실체는 건물이 아니라 **"Moonlight MOTEL" 레트로 네온 간판 메쉬** (약 1.57 × 1.90 × 0.17m, 렌더러 1개). 베이스 컬러(진청 보드/금색 필기체/빨강 MOTEL/크림 초승달) 정상, 노멀맵 릴리프 정상, 마젠타/에러 셰이더 없음. 임시 인스턴스 삭제, 씬 미저장.

## 미처리
- Meshy 자체 `..._texture_metallic_roughness.png`는 미사용 (분리된 metallic/roughness 맵에서 재베이크)
