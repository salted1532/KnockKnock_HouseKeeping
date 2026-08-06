# 0006. 새로 만든 머티리얼을 모델의 기존 머티리얼에 명시적으로 매핑

## 날짜
2026-08-06

## 요청 내용
> 너가 만든 머티리얼들을 models에 같은 머티리얼(같은 이름 + 투명화 안된)에다가 적용시켜줘

## 조사 내용
0005에서 만든 `.mat`들은 모델 임포터의 "이름이 일치하는 외부 머티리얼 자동 검색"(`materialLocation: 1`, Recursive-Up)에 의존하고 있어서, 실제로 연결되려면 Unity가 재임포트를 수행해야 함. 사용자가 명시적으로 "적용"을 요청한 것을 보아, 자동 검색에 맡기지 말고 모델 임포터의 `.meta`에 직접 매핑을 박아두는 게 확실하다고 판단함.

Unity가 실제로 이런 매핑에 쓰는 `.meta` 포맷(`ModelImporter.externalObjects`)을 로컬에 캐시된 다른 Unity 프로젝트의 패키지 샘플(`com.unity.shadergraph`/`com.unity.render-pipelines.core` 패키지 캐시 내 실제 리매핑된 FBX `.meta` 예시, 예: `Rock_A_01.fbx.meta`)에서 실제 포맷을 확인 후 그대로 사용함:
```yaml
externalObjects:
- first:
    type: UnityEngine:Material
    assembly: UnityEngine.CoreModule
    name: <내부 머티리얼 이름>
  second: {fileID: 2100000, guid: <mat 파일 guid>, type: 2}
```

## 적용한 변경
- `Assets/AssetsFolder/All/Models/All.fbx.meta`: `Plants` → `All/Materials/Plants.mat` 매핑 1건 추가
- `Assets/AssetsFolder/Models pack psx/Models/models.fbx.meta`: `Branch`, `Branch_01`, `Branch_02`, `Bush`, `Bush_01`~`Bush_06`, `Plants`, `Plants_01`~`Plants_09` → 각각 대응하는 `Models pack psx/Materials/*.mat` 매핑 20건 추가 (`Bush_07`은 0005에서 애초에 머티리얼을 안 만들었으므로 매핑 대상 아님)

이제 Unity를 열면(또는 재임포트하면) 자동 검색을 거치지 않고 이 매핑을 통해 곧바로 투명화된 머티리얼이 적용됨.

## 변경된 파일
- `Assets/AssetsFolder/All/Models/All.fbx.meta`
- `Assets/AssetsFolder/Models pack psx/Models/models.fbx.meta`
