# 0008. 머티리얼 Smoothness 기본값을 0으로 변경

## 날짜
2026-08-06

## 요청 내용
> 아마도 여러 에셋을 테스트해볼 생각이라 이런작업이 자주 일어날탠데 머티리얼을 생성할때 Smoothness를 0으로 해줘 0.5로 되어있었는데 그러면 색감이 좀 탁해져서 내가 원하는 느낌이 아니네

## 적용한 변경
지금까지 만든 URP 머티리얼 21개(`_Smoothness: 0.5` → `0`, 짝인 `_Glossiness`도 함께 0으로) 전부 수정:
- `Assets/AssetsFolder/All/Materials/Plants.mat`
- `Assets/AssetsFolder/Models pack psx/Materials/Branch.mat`, `Branch_01.mat`, `Branch_02.mat`
- `Assets/AssetsFolder/Models pack psx/Materials/Bush.mat`, `Bush_01~06.mat`
- `Assets/AssetsFolder/Models pack psx/Materials/Plants.mat`, `Plants_01~09.mat`
- `Assets/AssetsFolder/Models pack psx/Materials/Tree.mat`

앞으로도 같은 작업(다른 에셋 팩 임포트 시 알파 클리핑 머티리얼 생성)이 반복될 거라고 해서, 이 프로젝트에서 새 머티리얼을 만들 때 기본으로 Smoothness/Glossiness를 0으로 시작하도록 메모리에 저장해둠(`feedback_material-smoothness-default-zero`) — 다음에 같은 작업할 때 다시 안 물어봐도 됨.

## 변경된 파일
- 위 21개 `.mat` 파일 (`_Smoothness`, `_Glossiness`: 0.5 → 0)
