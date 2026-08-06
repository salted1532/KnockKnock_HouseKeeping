# 0018. 프롭 프리팹+머티리얼 추출 에디터 스크립트

## 날짜
2026-08-06

## 요청 내용
> 이제 Assetsfolder안에 있는 모든 에셋의 모델링을 프리팹형식으로 나눠서 Assets/My/Prefabs/에다가 넣어주고 각 모델링이 사용하는 머티리얼은 머티리얼로 생성해서 Assets/My/Materials/ 에다가 넣어줘 그리고 프리팹에 사용하는 머티리얼은 Assets/My/Materials/로 생성한 머티리얼을 맞게 연결해줘 투명화했던 머티리얼도 그대로 Assets/My/Materials/로 이동시키고 맞게 연결해줘

[[0014-prop-prefab-extraction-proposal]]에서 이미 설명한 이유로, 이 규모(모델 20개, 최상위 오브젝트 수천 개)의 프리팹/머티리얼 추출은 제가 파일을 직접 손으로 만드는 방식으로는 할 수 없습니다(프리팹은 메시마다 Unity 내부 fileID가 필요하고, 그건 Unity가 실제로 모델을 열어봐야 알 수 있음). 그래서 **Unity 에디터에서 실행하는 C# 스크립트**를 작성했습니다: `Assets/Editor/AssetOrganizer.cs`.

## 스크립트가 하는 일
Unity 메뉴 **Tools → Organize Assets → Extract Props To Prefabs+Materials** 실행 시:

1. `AssetsFolder` 밑의 모델 24개(StarterAssets 제외)를 순회
2. 각 모델의 **최상위 자식 오브젝트 하나하나(=프롭)**에 대해:
   - 이름 기반 규칙으로 [[0015-category-folder-taxonomy-proposal]]의 카테고리 중 하나에 배정(예: 이름에 "chair"/"armchair"가 들어가면 `Furniture/Chairs_Seating`)
   - 매칭되는 규칙이 없으면 `Uncategorized`로
   - `Cube`, `Cylinder`, `NurbsCurve` 같은 순수 지오메트리 이름이나 `Ar10`, `B1`, `V2`처럼 본/리깅 헬퍼로 보이는 짧은 코드 이름은 프롭이 아니라고 보고 건너뜀
   - 프롭이 쓰는 머티리얼을:
     - **이미 [[0011-full-alpha-transparency-sweep]] 등에서 실제 `.mat` 파일로 뽑아둔 것**(알파 투명화 작업한 것들)은 → `AssetDatabase.MoveAsset`으로 그대로 `Assets/My/Materials/<카테고리>/`로 옮김. 이 방식은 GUID가 그대로 유지돼서, 기존에 각 모델 `.fbx.meta`에 심어둔 `externalObjects` 연결이 자동으로 계속 유효함(별도 재연결 작업 불필요)
     - **아직 모델에 내장된 채인 나머지 머티리얼**은 → 복사본을 새로 만들어서 `Assets/My/Materials/<카테고리>/`에 저장하고, 프롭의 렌더러가 그 새 머티리얼을 쓰도록 연결
   - 로컬 Position/Rotation을 0으로 리셋(Scale은 유지) — 대부분의 팩이 프리뷰용으로 프롭들을 씬 여기저기 흩어놓은 채로 export돼 있어서, 리셋 안 하면 프리팹 원점이 이상한 곳에 잡힘
   - `Assets/My/Prefabs/<카테고리>/<이름>.prefab`로 저장. 이름이 겹치면(여러 팩에 `Tree`가 다 있음) 자동으로 `_팩이름` 접미사를 붙여 구분

## 아직 실행 안 함 — 사용자가 Unity에서 실행해야 함
스크립트 파일만 만들었고, 실제로 프리팹/머티리얼이 생기는 건 사용자가 Unity 에디터에서 메뉴를 실행해야 일어납니다(제가 Unity를 직접 실행할 수 없음). 실행 전 참고:
- 파일 맨 위 `DryRun` 상수를 `true`로 바꾸고 한 번 실행하면, 실제로 아무것도 안 만들고 Console에 "무엇을 어디로 만들 예정인지"만 전부 로그로 찍힙니다. 카테고리 분류가 이상해 보이면 실행 전에 이걸로 먼저 확인해보는 걸 추천합니다.
- 규모가 크고(수천 개) 제가 직접 컴파일/실행해서 검증할 방법이 없어서, 실제 실행 결과가 예상과 다를 수 있습니다. 실행 후 Console 로그(마지막 줄에 생성된 프리팹/머티리얼 개수 요약이 찍힘)와 실제 폴더를 보고 이상한 점 알려주시면 스크립트를 고치겠습니다.
- 카테고리 규칙(`Rules` 배열)이나 리셋 여부(`resetTransform`)는 팩별로 스크립트 안에서 바로 조정 가능합니다.

## 변경된 파일
- `Assets/Editor/AssetOrganizer.cs` (신규 — 에디터 스크립트, 실행 전까지는 프로젝트의 다른 에셋에 영향 없음)
