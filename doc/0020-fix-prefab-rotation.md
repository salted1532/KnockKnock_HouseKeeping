# 0020. 생성된 프리팹 회전 X = -90 일괄 수정

## 날짜
2026-08-06

## 요청 내용
> 생성된 prefab들을 확인해 봤는데 프리팹 방향이 눕혀져 있어서 모든 프리팹의 회전 x값을 -90으로 만들어줘

## 적용한 변경
### 1) 이미 생성된 프리팹 6585개 전부 수정
각 `.prefab` 파일에서 루트 Transform(=`m_Father: {fileID: 0}`인 것, 하위 부품의 Transform은 건드리지 않음)의 `m_LocalRotation`을 X축 -90도에 해당하는 쿼터니언으로, `m_LocalEulerAnglesHint`를 `{x: -90, y: 0, z: 0}`으로 일괄 변경(Node.js 스크립트로 6585개 파일 전수 처리, 전부 정상 처리됨 — 루트를 못 찾거나 루트가 2개 이상인 파일 없음).

### 2) 앞으로 생성될 프리팹도 동일하게 나오도록 스크립트 수정
`Assets/Editor/AssetOrganizer.cs`에서 프롭 추출 시 로컬 회전을 `Quaternion.identity`로 리셋하던 걸 `Quaternion.Euler(-90, 0, 0)`으로 바꿈. 기존엔 캐릭터 4개(`resetTransform: false`)는 회전을 안 건드렸었는데, 이번 요청이 "모든 프리팹"이라 회전만큼은 `resetTransform` 값과 무관하게 전부 적용되도록 분리함(포지션 리셋 여부만 `resetTransform`이 결정).

## 변경된 파일
- `Assets/My/Prefabs/` 밑 `.prefab` 6585개 (`m_LocalRotation`, `m_LocalEulerAnglesHint`)
- `Assets/Editor/AssetOrganizer.cs` (기본 회전값 로직 수정)
