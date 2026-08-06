> **주의: 이 문서는 제안서입니다. 아래 내용은 사용자 승인 전까지 실제 프로젝트에 반영되지 않았습니다.**

# 0015. 프롭 카테고리 폴더 구조 제안 (Prefabs / Materials)

## 날짜
2026-08-06

## 요청 내용
> 지금 에셋들은 가구, 물건, 건물, 식물 등 다양한 카테고리에 에셋들이 합쳐져 있는데 각 모델들의 이름을 분석해서 너가 세세하게 폴더를 나눠서 그안에 각각 프리팹으로 만들어줘야해 가구라고 해도 침대, 의자 등 다 다른 폴더로 구성해야해 일단은 Assetsfolder안에 있는 모든 모델의 이름을 분석하고 가능한 세세한 카테고리 폴더를 만들어서 prefabs와 materials에다가 만들어줘 추후에 materials들도 뽑아달라고 할꺼야

즉 이번 작업 범위는: **모델 이름 분석 + 카테고리 폴더 구조 생성**까지. 실제 프리팹/머티리얼을 그 폴더 안에 채우는 건 각각 별도 작업(프리팹은 [[0014-prop-prefab-extraction-proposal]], 머티리얼은 추후 요청 예정).

## 조사 내용
`AssetsFolder` 안의 모델(비 StarterAssets) 20개 `.fbx` 전체에서 오브젝트 이름을 추출해서(총 6259개, 숫자 접미사 정리 후 907개 종류) 실제 어떤 카테고리들이 필요한지 분석함. 이 팩들은 스페인어/영어가 섞여 있음(`Mesa`=테이블, `Arbol`=나무, `Sarten`=프라이팬 등).

의미 없는 지오메트리 이름(`Cube`, `Cylinder`, `NurbsCurve` 등)이나 리깅/본 헬퍼로 보이는 것(`A`, `B1`, `Ar3`, `V2` 같은 짧은 코드성 이름)은 카테고리에서 제외함 — 실제 프롭이 아니라서.

## 제안하는 카테고리 구조 (2단계: 대분류/소분류)

```
Furniture/
  Beds
  Chairs_Seating
  Tables
  Storage           (옷장, 캐비닛, 서랍장, 선반, 책장)
  Sofas_Couches
  Soft_Furnishing   (커튼, 러그, 쿠션, 블라인드)

Kitchen/
  Appliances        (냉장고, 오븐, 레인지, 전자레인지, 블렌더, 커피머신)
  Cookware          (냄비, 프라이팬, 도마)
  Tableware_Cutlery (접시, 컵, 포크/나이프/스푼, 그릇)
  Sinks

Bathroom/
  Fixtures          (변기, 샤워, 욕조, 세면대)
  Toiletries        (비누, 샴푸, 칫솔, 휴지)

Laundry_Cleaning/
  Appliances        (세탁기)
  Supplies          (세제, 섬유유연제)
  Cleaning_Tools    (빗자루, 대걸레, 쓰레기통, 쓰레받기)

Food/
  Fruits_Vegetables
  Prepared_Food     (피자, 버거, 핫도그, 튀김류)
  Drinks_Condiments (소스, 음료, 조미료)
  Bakery_Snacks     (빵, 도넛, 케이크, 과자)

Plants_Nature/
  Trees
  Bushes_Plants
  Flowers
  Ground_Terrain    (잔디, 지형, 돌)

Buildings_Architecture/
  Buildings
  Doors_Windows
  Walls_Floors_Ceilings
  Roofing
  Fences_Railings

Electronics/
  Appliances        (TV, 라디오, 모니터)
  Phones
  Registers_Machines (계산대, 자판기)

Lighting/
  Indoor_Lamps
  Outdoor_Lighting  (가로등, 스포트라이트)

Vehicles/
  Cars
  Parts             (타이어, 가스통)

Decor/
  Wall_Art          (그림, 액자, 거울)
  Accessories       (화병, 책, 시계)

Outdoor_Street/
  Street_Furniture  (버스정류장, 신호등, 전신주)
  Road              (도로 마킹, 인도, 횡단보도)

Signage/

Characters/
```

15개 대분류, 소분류 합쳐서 약 45개 폴더. 이 구조를 `Prefabs`와 `Materials` 양쪽에 동일하게(미러) 만듭니다.

## 결정 필요 사항
1. **폴더 위치**: `Assets/Prefabs/...`, `Assets/Materials/...`처럼 최상위에 새로 만들지(제안, `AssetsFolder`의 원본 팩별 구조와 분리되는 "정리된" 뷰), 아니면 `Assets/AssetsFolder/Prefabs/...`처럼 그 안에 넣을지
2. 위 카테고리 구조가 괜찮은지, 빠졌거나 더 쪼개고 싶은 분류가 있는지 (예: `Kitchen`을 더 세분화, `Vehicles`를 뺄지 등)

## 변경된 파일
없음(제안서만 작성, 승인 대기)
