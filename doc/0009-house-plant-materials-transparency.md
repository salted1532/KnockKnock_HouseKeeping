# 0009. House 팩 포함, 식물 연관 머티리얼 전체 투명화

## 날짜
2026-08-06

## 요청 내용
> Background, bush, tree, plants 등 식물과 연관있는 이름이 있는 머티리얼들을 모두 투명화 작업을 시행해줘 머티리얼을 투명화로 생성 적용까지

## 조사 내용
`All.fbx`, `models.fbx`(Models pack psx)는 이미 0005~0008에서 식물 관련 머티리얼(Plants / Branch·Bush·Plants·Tree)을 전부 처리했음 — 재확인해도 추가로 나온 건 없음.

"Background"라는 이름의 재질은 저 두 모델엔 없었고, 지금까지 다루지 않았던 **세 번째 에셋 팩 `Assets/AssetsFolder/House/House/`**(`House.fbx`)에 존재함(기존 커밋에 이미 포함돼 있던 폴더). 이 모델의 실제 Material 목록을 전수 확인해서 식물/자연물 관련 이름 19개를 추림:

`Background`, `Bush`, `Flower_Table`, `Flowers`, `NaturePlants`, `NaturePlants_01~06`, `Plants`, `Tree`, `Tree_01~04`, `Tree_06`, `plants_flowers`

같은 계열이지만 알파 채널이 없는 `Grass.jpg`, `Hedges.jpg`는 원래부터 불투명 재질(바닥/생울타리 타일링용)로 보여 제외함(나머지 19개는 전부 PNG RGBA로 알파 채널 확인).

**주의(확신도 낮음)**: 머티리얼 `Tree_06`에 대응하는 텍스처 파일이 없음(`Tree.png`~`Tree_04.png`, `Tree_05.png`까지만 존재, `Tree_06.png` 없음). 머티리얼 개수(6개: Tree, 01~04, 06)와 텍스처 개수(6개: Tree, 01~05)가 정확히 일치해서, 마지막 머티리얼 `Tree_06`은 마지막 텍스처 `Tree_05.png`를 쓰는 것으로 추정하고 그렇게 연결함. 실제 모델에서 이상하게 보이면 알려주시면 재조정하겠음.

## 적용한 변경
0005~0007과 동일한 방식(URP Lit, Alpha Clipping, Cull Off, Alpha Is Transparency, **Smoothness/Glossiness 0** — [[feedback_material-smoothness-default-zero]] 적용):
- `Assets/AssetsFolder/House/House/Materials/` 새로 생성 — 19개 `.mat`(+`.meta`, Unity가 아직 자동 생성 전이라 GUID 직접 부여)
- `Assets/AssetsFolder/House/House/Textures/{Background,Bush,Flower_Table,Flowers,NaturePlants,NaturePlants_01~06,Plants,Tree,Tree_01~05,plants_flowers}.png.meta`: `alphaIsTransparency: 0` → `1`
- `Assets/AssetsFolder/House/House/Models/House.fbx.meta`: `externalObjects`에 19개 매핑 추가(머티리얼 이름 → 새 `.mat`, `Tree_06`만 `Tree_05.png` 사용 머티리얼에 연결)

## 변경된 파일
- `Assets/AssetsFolder/House/House/Materials/*.mat`, `*.mat.meta` (19개씩)
- `Assets/AssetsFolder/House/House/Textures/*.png.meta` (19개)
- `Assets/AssetsFolder/House/House/Models/House.fbx.meta`
