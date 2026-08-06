# 0016. 카테고리 폴더 생성 (Prefabs / Materials)

## 날짜
2026-08-06

## 요청 내용
> Assets/Prefabs/, Assets/Materials/ 에다가 각 분류별 폴더만 일단 만들어줘 아직 프리팹이나 머티리얼을 뽑아내진마

[[0015-category-folder-taxonomy-proposal]]에서 제안한 분류 구조 그대로 승인.

## 적용한 변경
`Assets/Prefabs/`, `Assets/Materials/` 밑에 동일한 카테고리 폴더 구조를 만듦(15개 대분류, 소분류 포함 총 41개 폴더씩). 폴더만 만들었고 안은 비어 있음 — 프리팹/머티리얼 추출은 하지 않음.

```
Furniture/{Beds, Chairs_Seating, Tables, Storage, Sofas_Couches, Soft_Furnishing}
Kitchen/{Appliances, Cookware, Tableware_Cutlery, Sinks}
Bathroom/{Fixtures, Toiletries}
Laundry_Cleaning/{Appliances, Supplies, Cleaning_Tools}
Food/{Fruits_Vegetables, Prepared_Food, Drinks_Condiments, Bakery_Snacks}
Plants_Nature/{Trees, Bushes_Plants, Flowers, Ground_Terrain}
Buildings_Architecture/{Buildings, Doors_Windows, Walls_Floors_Ceilings, Roofing, Fences_Railings}
Electronics/{Appliances, Phones, Registers_Machines}
Lighting/{Indoor_Lamps, Outdoor_Lighting}
Vehicles/{Cars, Parts}
Decor/{Wall_Art, Accessories}
Outdoor_Street/{Street_Furniture, Road}
Signage
Characters
```

## 참고
Unity를 열면 이 폴더들에 대한 `.meta`가 자동 생성됨. Git은 빈 폴더를 추적하지 않으니, 커밋해서 원격에 남기고 싶으면 안에 파일이 하나라도 생겨야 함(프리팹/머티리얼이 채워지면 자연히 해결됨).

## 변경된 파일
- `Assets/Prefabs/` 밑 41개 폴더 (신규, 빈 폴더)
- `Assets/Materials/` 밑 41개 폴더 (신규, 빈 폴더)
