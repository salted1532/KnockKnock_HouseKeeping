# 0124 - 에디터 콘솔의 SerializedObjectNotCreatableException 진단

날짜: 2026-08-31

## 증상 (사용자)

```
SerializedObjectNotCreatableException: Object at index 0 is null
  UnityEditor.Editor.CreateSerializedObject ()
  UnityEditor.AudioSourceInspector.OnEnable ()
MissingReferenceException: The variable m_Targets of GameObjectInspector doesn't exist anymore.
  UnityEditor.PrefabUtility.IsPartOfVariantPrefab (...)
  UnityEditor.GameObjectInspector.OnEnable ()
```

도메인 리로드(스크립트 재컴파일 / 플레이모드 진입)마다 4× `SerializedObjectNotCreatableException` + 1× `MissingReferenceException` 발생.

## 진단 — 프로젝트 코드/에셋 문제 아님

| 확인 | 결과 |
|---|---|
| `uloop compile` | Error 0, Warning 1 (`AssetOrganizer.cs` CS0162, 기존 무해) |
| 씬 missing script | 0 |
| 프리팹 missing script (My/ 전체) | 0 |
| 프리팹 타입 | 전부 `Regular` (깨진 variant 아님), 씬 인스턴스 링크 정상 |
| `AssetPostprocessor` / `DidReloadScripts` / `[InitializeOnLoadMethod]` (My/) | 없음 |
| 시작 시 `Destroy` 호출 (게임 코드) | 없음 — 전부 이벤트 기반 |
| **깨끗한 Play (선택 없음)** | **에러 0** |
| 게임 flow 강제 실행 (페이즈 순회 + 체크인 + 노크) | 우리 코드 관련 에러 0 |
| `Selection.activeObject` / `ActiveEditorTracker.sharedTracker` | 둘 다 비어 있는데도 리로드 시 에러 발생 |

**스택 트레이스가 전부 `UnityEditor.*` 내부** (`AudioSourceInspector.OnEnable`, `GameObjectInspector.OnEnable`, `Editor.CreateSerializedObject`).
→ **핀 고정된(또는 두 번째) Inspector 창**이 오디오소스 있는 GameObject 를 잡고 있다가, 도메인 리로드 후 `m_Targets` 재바인딩에 실패하는 **Unity 에디터 자체 버그**. 이 상태는 에디터 창 레이아웃 / `Library/` 에 있고 프로젝트 에셋엔 없음.

## 조치

### 1. `Assets/TutorialInfo/` + `Assets/Readme.asset` 삭제

직접 원인은 아니지만:
- README 가 이미 "게임 미사용 잔재"로 표시
- `ReadmeEditor` 가 `[InitializeOnLoad]` + `EditorApplication.delayCall` 로 세션 시작 시 **커스텀 창 레이아웃(`TutorialInfo/Layout.wlt`)을 로드**함 → 핀 고정 Inspector 같은 이상 레이아웃의 잠재적 원인
- `Assets/Readme.asset` 은 `TutorialInfo/Scripts/Readme.cs` 참조 → TutorialInfo 지우면 missing script 되므로 같이 삭제
- `Assets/AssetsFolder/StarterAssets/Readme.asset` 은 **다른** Readme.cs(StarterAssets 패키지) 참조 → 유지

삭제 후에도 리로드 에러는 그대로 → 역시 에디터 창 상태 문제 확정.

### 2. 사용자 조치 (에디터 재시작 필요)

이 에러는 빌드에 안 나오고 게임플레이/에셋에 영향 없음. 없애려면:
1. **Unity 에디터 재시작** (핀 고정된 Inspector 캐시 초기화)
2. 재발 시 **Window → Layouts → Default Layout** (Inspector 창 리셋)
3. 그래도면 **`Library/` 폴더 삭제** 후 Unity 재실행 (에디터 캐시 완전 초기화, 재임포트 몇 분)

## 상태

2026-08-31 진단 완료. 프로젝트 클린 확인. TutorialInfo 제거. 나머지는 에디터 재시작으로 해결 (코드/에셋 수정 대상 아님).
