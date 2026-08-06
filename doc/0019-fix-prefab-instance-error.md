# 0019. AssetOrganizer 프리팹 저장 에러 수정

## 날짜
2026-08-06

## 요청 내용
> ArgumentException: Can't save part of a Prefab instance as a Prefab
> UnityEditor.PrefabUtility.SaveAsPrefabAssetArgumentCheck ...
> AssetOrganizer.Run () (at Assets/Editor/AssetOrganizer.cs:194)

## 원인
`PrefabUtility.InstantiatePrefab(root)`로 모델 전체를 씬에 인스턴스화한 뒤, 그 자식(`child.gameObject`)을 그대로 `PrefabUtility.SaveAsPrefabAsset`에 넘겼는데, 이 자식은 여전히 "더 큰 프리팹 인스턴스의 일부"로 취급돼서 Unity가 저장을 거부함(정확히 이 에러 메시지가 뜨는 상황).

## 수정
모델 자체(`AssetDatabase.LoadAssetAtPath`로 읽은 에셋)에서 최상위 자식 이름만 먼저 뽑고, 각 프롭마다 `PrefabUtility.InstantiatePrefab` 대신 `UnityEngine.Object.Instantiate(child.gameObject)`로 완전히 독립된 복제본을 하나씩 만들어서(프리팹 인스턴스 연결이 없는 순수 복제) 그걸 저장하고 바로 지우는 방식으로 변경. 씬에는 아무 흔적도 안 남음(저장 안 하니까).

다시 실행하면 됩니다. Dry Run으로 미리 본 로그(카테고리 분류 등)는 이번 수정과 무관해서 다시 확인 안 해도 됩니다.

## 변경된 파일
- `Assets/Editor/AssetOrganizer.cs` (`Run()` 메서드의 프리팹 추출 로직 수정)
