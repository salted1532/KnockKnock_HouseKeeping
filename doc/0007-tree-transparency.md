# 0007. Tree 머티리얼 투명화

## 날짜
2026-08-06

## 요청 내용
> Tree도 머티리얼도 투명화인걸 확인했어 투명 머티리얼 생성해주고 적용까지 해줘

## 조사 내용
`Tree`는 `Models pack psx/models.fbx`에만 실제 Material로 존재함(`All.fbx`엔 없음). 텍스처 `Models pack psx/Texture/Tree.png`는 RGBA(알파 채널 있음) 확인.

## 적용한 변경
0005/0006과 동일한 방식(URP Lit, Alpha Clipping, Cull Off, Alpha Is Transparency)으로 처리:
- `Assets/AssetsFolder/Models pack psx/Materials/Tree.mat` 생성(+ `.meta`, 신규 GUID 직접 부여 — Unity가 아직 자동 생성 전이라 수동으로 채움)
- `Assets/AssetsFolder/Models pack psx/Texture/Tree.png.meta`: `alphaIsTransparency: 0` → `1`
- `Assets/AssetsFolder/Models pack psx/Models/models.fbx.meta`: `externalObjects`에 `Tree` → `Tree.mat` 매핑 추가

## 변경된 파일
- `Assets/AssetsFolder/Models pack psx/Materials/Tree.mat` (신규)
- `Assets/AssetsFolder/Models pack psx/Materials/Tree.mat.meta` (신규)
- `Assets/AssetsFolder/Models pack psx/Texture/Tree.png.meta`
- `Assets/AssetsFolder/Models pack psx/Models/models.fbx.meta`
