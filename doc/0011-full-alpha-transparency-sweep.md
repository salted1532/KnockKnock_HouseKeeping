# 0011. AssetsFolder 전체 텍스처 알파 투명화 전수 조사 및 적용

## 날짜
2026-08-06

## 요청 내용
> 현재 AssetsFolder안에 있는 모든 텍스쳐를 확인해서 투명부분이있는 png가 사용된 머티리얼들은 모두 투명화 머티리얼을 생성해주고 적용까지 시켜줘 이 작업은 에셋 테스트하면서 계속 반복될거 같아

## 조사 방법
1. `AssetsFolder` 안의 머티리얼 내장 모델(`.fbx`)을 전부 나열: `All.fbx`, `models.fbx`(psx), `House.fbx`(+`House_Colliders.fbx`), `Pizzeria_Props.fbx`, `Pizzeria_Scene.fbx`, `Objects_Interior(Village)_Demo.fbx`, `StarterAssets`의 환경 메쉬 9개.
2. `StarterAssets` 환경 메쉬는 `materialImportMode: 1`(임베드 방식 아님) + 이미 만들어진 `.mat`(`Blue_Mat` 등)을 정상적으로 쓰고 있고, 쓰이는 텍스처(`Grid_01/02_BaseMap`)도 알파 채널이 없어 대상에서 제외. `House_Colliders.fbx`는 머티리얼/텍스처 자체가 없어 제외(충돌용 메쉬).
3. 나머지 6개 모델 파일 전부에서 "진짜 머티리얼 이름"만 뽑아서(FBX 내부 오브젝트 타입 확인, Geometry/Model 이름과 혼동 방지) 같은 이름의 `.png`가 있는지 대조.
4. **알파 채널이 "있다"와 "실제로 투명하게 쓰인다"는 다르다는 점을 주의함**: 알파 채널이 있어도 전부 255(불투명)인 텍스처가 많고(가구/소품류 대부분), 반대로 노멀맵처럼 알파에 다른 정보(러프니스 등)를 담아 투명과 무관한 경우도 있을 수 있음. 그래서 Node.js(설치돼 있어 별도 설치 없이 사용, zlib 내장 모듈로 PNG를 직접 디코딩)로 각 PNG의 실제 알파 픽셀 값을 분석하는 스크립트를 짜서, "알파값이 거의 0인 픽셀이 의미 있는 비율(1% 이상)로 존재하는" 텍스처만 골라냄. 이 기준을 통과 못 한 알파-있음 텍스처(예: `Broom`, `Marble_01~03`, `bottles`, 대부분의 가구 아이콘 등)는 원래부터 완전 불투명이라 제외함.
5. 이미 처리된 것(Plants/Branch/Bush/Tree 계열, doc 0005~0010)은 재확인 없이 스킵.

## 새로 찾아서 처리한 것 (플랜트 계열 아님 — 아이콘/철사/틈새 있는 소품류)
알파가 실사용되는 텍스처는 대부분 "여러 개의 작은 소품을 한 장의 텍스처 시트에 배치하고 나머지는 투명 처리"하는 방식(아이콘 아틀라스)이거나, 울타리·와이어·선반처럼 실제로 뚫린 형태의 오브젝트였음.

| 팩 | 새로 만든 머티리얼 |
|---|---|
| All | Cutlery, Entertainment, Plastic_10, Wire (4개) |
| Models pack psx | Cutlery, Mesh, Wire, Wire2, Wire3 (5개) |
| House | Cutlery, Entertainment, Fence, Phone, Phone_01, Plastic_10, Rake, Rake_01, Refrigerator, Shelving (10개) |
| Pizzeria (Props+Scene 공유) | Pineapple, Refrigerator, Refrigerator_01, dough_press, Fence, Lines, Wire (7개, 이름 겹치는 건 파일 하나 공유) |
| Objects_Interior(Village)_Demo (신규 팩, 이번에 처음 다룸) | Cutlery, Flower_Table, Flower_Table_01, Gadgets, Phone, Phone_01, Refrigerator, Table_01, Table_02, Ventilator, Ventilator_01 (11개) |

총 37개 신규 머티리얼. 방식은 지금까지와 동일(URP Lit, Alpha Clipping on, Cull Off, Smoothness/Glossiness 0), 각 팩의 `Materials` 폴더에 저장하고 해당 모델의 `.fbx.meta`에 `externalObjects` 명시적 매핑 추가, 텍스처는 `Alpha Is Transparency` 켬.

## 참고 (다음에도 반복될 작업이라 남겨둠)
- 새 에셋을 추가할 때마다 이 절차(진짜 Material 이름 추출 → 동명 png 존재 확인 → 실제 알파 픽셀 분석 → 알파 실사용하는 것만 컷아웃 머티리얼 생성)를 반복하면 됨. 알파 채널 유무만으로 판단하면 안 되고, 실제 픽셀 값을 봐야 오탐(완전 불투명인데 RGBA 포맷인 파일)을 피할 수 있음.
- PNG 알파 분석용 Node 스크립트는 세션 스크래치패드에 있음(프로젝트 파일 아님, 재사용 시 다시 작성 필요).

## 변경된 파일
- 5개 팩의 `Materials/*.mat`, `*.mat.meta` 37쌍 (신규)
- 위 표에 나열된 텍스처들의 `.png.meta` 37개 (`alphaIsTransparency: 0` → `1`)
- `All/Models/All.fbx.meta`, `Models pack psx/Models/models.fbx.meta`, `House/House/Models/House.fbx.meta`, `Pizzeria/Pizzeria/Models/Pizzeria_Props.fbx.meta`, `Pizzeria/Pizzeria/Models/Pizzeria_Scene.fbx.meta`, `Objects_Interior(Village)_Demo/Models/Objects_Interior(Village)_Demo.fbx.meta` (`externalObjects` 매핑 추가)
