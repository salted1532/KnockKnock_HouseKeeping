# 0012. Render Face를 Both에서 Front로 일괄 변경

## 날짜
2026-08-06

## 요청 내용
> 생성된 Render Face를 모두 Front로 바꿔줘 이제부터 생성한 메테리얼은 모든 Front로 만들어줘

## 적용한 변경
지금까지 만든 알파 클리핑 머티리얼 79개(`_Cull: 0` = Both → `_Cull: 2` = Front, 즉 뒷면 컬링하는 일반적인 단면 렌더링) 전부 일괄 변경:
- `Assets/AssetsFolder/All/Materials/*.mat`
- `Assets/AssetsFolder/Models pack psx/Materials/*.mat`
- `Assets/AssetsFolder/House/House/Materials/*.mat`
- `Assets/AssetsFolder/Pizzeria/Pizzeria/Materials/*.mat`
- `Assets/AssetsFolder/Objects_Interior(Village)_Demo/Materials/*.mat`

앞으로 만들 머티리얼도 기본값을 Front(`_Cull: 2`)로 하도록 [[feedback_transparency-sweep-workflow]]에 반영해둠 — 다음부터 다시 안 물어봐도 됨.

## 변경된 파일
- 위 5개 폴더의 `.mat` 79개 (`_Cull`: 0 → 2)
