# 0010. Pizzeria 에셋의 Tree 머티리얼 투명화

## 날짜
2026-08-06

## 요청 내용
> Pizzeria 에셋폴더에있는 Tree도 작업해줘

## 조사 내용
`Assets/AssetsFolder/Pizzeria/Pizzeria/`에 모델이 두 개(`Pizzeria_Props.fbx`, `Pizzeria_Scene.fbx`) 있고, 둘 다 `Tree` 머티리얼을 가지고 있으며 같은 텍스처(`Textures/Tree.png`, RGBA 알파 채널 확인)를 참조함. 그 외 식물 관련 후보로 `Pizzeria_Scene.fbx`에 `Grass` 머티리얼이 있었지만 텍스처가 `Grass.jpg`(알파 없음)라 House/psx 때와 같은 기준으로 제외.

## 적용한 변경
0005~0009와 동일 방식(URP Lit, Alpha Clipping, Cull Off, Alpha Is Transparency, Smoothness/Glossiness 0):
- `Assets/AssetsFolder/Pizzeria/Pizzeria/Materials/Tree.mat` 신규 생성(+`.meta`, GUID 직접 부여)
- `Assets/AssetsFolder/Pizzeria/Pizzeria/Textures/Tree.png.meta`: `alphaIsTransparency: 0` → `1`
- `Pizzeria_Props.fbx.meta`, `Pizzeria_Scene.fbx.meta` 둘 다에 `Tree` → 같은 `Tree.mat` 매핑 추가(두 모델이 같은 이름의 머티리얼을 공유하므로 머티리얼 파일도 하나 공유)

## 변경된 파일
- `Assets/AssetsFolder/Pizzeria/Pizzeria/Materials/Tree.mat` (신규)
- `Assets/AssetsFolder/Pizzeria/Pizzeria/Materials/Tree.mat.meta` (신규)
- `Assets/AssetsFolder/Pizzeria/Pizzeria/Textures/Tree.png.meta`
- `Assets/AssetsFolder/Pizzeria/Pizzeria/Models/Pizzeria_Props.fbx.meta`
- `Assets/AssetsFolder/Pizzeria/Pizzeria/Models/Pizzeria_Scene.fbx.meta`
