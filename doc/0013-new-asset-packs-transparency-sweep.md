# 0013. 새로 추가된 에셋 팩 전체 투명화 스윕

## 날짜
2026-08-06

## 요청 내용
> 이제 테스트 에셋들을 다 추가했어 AssetsFolder안에 있는 모든 에셋파일들을 분석하고 투명화 이미지로 이루어진 머티리얼이있으면 투명화 머티리얼을 생성하고 적용까지 시켜줘

## 조사 내용
[[feedback_transparency-sweep-workflow]] 절차 그대로 재적용. `AssetsFolder`를 다시 스캔하니 새 에셋 팩 7개(모델 파일 18개)가 추가돼 있었음: `6twelve`, `Buildings`, `BurgerPiz`, `Bus_stop`(Props/Stop/Stop_01 3개), `DINER`(DINER/Objects 2개), `Laundry`(Laundry/Laundry_Props 2개), `Tacos`(Tacos/Tacos_Props 2개), `Trailer_Park`(캐릭터 4개 + Trailer_Park/Trailer_Park_Props 2개).

참고로 이전에 다루던 `House/House/...` 경로가 이번 커밋들(`에셋추가2`, `에셋정리`)에서 `House/...`로 평평해져 있었는데, GUID는 그대로 유지돼 있어서(같은 `.meta` 그대로 이동) 0009에서 만든 머티리얼/매핑은 깨지지 않고 정상 작동함 — 확인만 하고 별도 조치는 안 함.

- `Buildings.fbx`: 텍스처 전부(`Shops_01~31` 등) 알파 채널 없거나 완전 불투명 → 대상 없음
- `Trailer_Park`의 캐릭터 4개(`Character_Female`, `_01`, `Character_Male`, `_01`)는 전용 텍스처가 있었지만 전부 완전 불투명(알파 255) → 대상 없음
- `StarterAssets`는 기존과 동일하게 대상 아님(재확인 안 함, 이전 결론 유지)

나머지 7개 팩에서 실제로 알파를 쓰는(알파<10인 픽셀이 유의미하게 존재) 머티리얼 90개를 찾음. 대부분 나무/식물(Tree, Trees, Plants, Branches, Bush 등), 배경 카드(Background), 울타리/철사(Fence, Wire, Metal_Fence), 아이콘 시트형 소품(Cans, Cookware, Clothings 등)이었음.

## 적용한 변경
지금까지와 동일 방식(URP Lit, `_AlphaClip: 1`, **`_Cull: 2`(Front)** — [[0012-render-face-front]] 반영, Smoothness/Glossiness 0, `Alpha Is Transparency` 켬, `.fbx.meta`에 `externalObjects` 명시적 매핑):

| 팩 | 새 머티리얼 수 | 비고 |
|---|---|---|
| 6twelve | 13 | Branches, Cigars, Garbage_bag, Grass, Plants_01/02, Plastic_05, RoadMarkings, Scattered, Store_fridge, Tree, Tree_01, Trees_Background |
| BurgerPiz | 10 | Background, Lines, Refrijerator_M, Shelf_FR, Tree~Tree_03, Wire, plants_flowers |
| Bus_stop | 12 (Props/Stop/Stop_01 3개 fbx가 일부씩 공유) | Plants·Plants_01/02, Tree~Tree_03, fence_metal, Land, Grass_02, Wheat, Wire |
| DINER | 17 (DINER.fbx/Objects.fbx 공유) | Bush, Cans, Grid, Grid_01, Lines, Moss, Oven, Plants류, Refrigerator, Sewer, Tree/Tree_01, Trees_Background, V, Wire |
| Laundry | 11 (Laundry.fbx/Laundry_Props.fbx 공유) | Background/01, Lines, Tree~Tree_03, building_03, clothes, hanging_clothes, plants_flowers |
| Tacos | 5 (Tacos.fbx/Tacos_Props.fbx 공유) | Branches, Plants, Plants_02, Wire, plastic_bag_P |
| Trailer_Park | 22 (Trailer_Park.fbx/_Props.fbx 공유) | Background/01, Clothings, Cookware, Covered, Curtain/01, FabricPlain, Fence, Flowers, Mesh, Metal_Fence, Plants/01, Refrigerator, Trees~Trees_05, bathroom_drain |

총 90개. 이름이 겹치는 머티리얼(예: 같은 팩의 두 fbx가 둘 다 `Tree`를 씀)은 파일 하나만 만들어 공유, 각 fbx에는 그 fbx가 실제로 갖고 있는 이름만 매핑함(fbx마다 실제 포함된 머티리얼 목록이 조금씩 다름 — 예: `Tacos_Props.fbx`는 `Tacos.fbx`가 쓰는 5개 중 2개만 가지고 있음).

**주의(확신도 낮은 것 1건)**: `Bus_stop`의 `Tree_01.png`는 16비트 PNG라 알파 픽셀 분석 스크립트가 지원을 못 해서 실제 픽셀 확인은 못 했음. 다만 이 프로젝트의 모든 팩에서 "Tree" 계열 텍스처가 예외 없이 전부 실제 투명 텍스처였던 패턴이 있어서, 같은 패턴으로 보고 컷아웃 처리함.

## 변경된 파일
- 7개 팩의 `Materials/*.mat`, `*.mat.meta` 90쌍 (신규)
- 위 90개에 대응하는 텍스처 `.png.meta` (`alphaIsTransparency: 0` → `1`)
- 13개 `.fbx.meta`(`6twelve.fbx`, `BurgerPiz.fbx`, `Bus_stop`의 `Props.fbx`/`Stop.fbx`/`Stop_01.fbx`, `DINER.fbx`/`Objects.fbx`, `Laundry.fbx`/`Laundry_Props.fbx`, `Tacos.fbx`/`Tacos_Props.fbx`, `Trailer_Park.fbx`/`Trailer_Park_Props.fbx`)에 `externalObjects` 매핑 추가
