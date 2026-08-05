# ReadmeEditor.cs

- 경로: `Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs`
- 타입: `Editor` (에디터 전용, `[CustomEditor(typeof(Readme))]`, `[InitializeOnLoad]`)
- 출처: Unity 기본 템플릿 보일러플레이트. 프로젝트 고유 로직 없음.

## 역할
[Readme.md](Readme.md) (`Readme` ScriptableObject)를 인스펙터에서 제목/아이콘/섹션(제목·본문·링크)이 있는 안내 페이지 형태로 렌더링한다. 에디터 시작 시 자동으로 Readme 에셋을 선택하고, 최초 1회 지정된 창 레이아웃(`TutorialInfo/Layout.wlt`)을 로드한다.

## 주요 동작
| 메서드 | 설명 |
|---|---|
| `SelectReadmeAutomatically()` | 정적 생성자에서 `EditorApplication.delayCall`로 등록. 세션당 1회, Readme 에셋을 자동 선택 |
| `LoadLayout()` | 리플렉션으로 내부 `WindowLayout.LoadWindowLayout` 호출, `Layout.wlt` 적용 |
| `SelectReadme()` | `AssetDatabase.FindAssets("Readme t:Readme")`로 Readme 에셋 1개를 찾아 선택 |
| `OnHeaderGUI()` | 아이콘 + 제목 헤더 렌더링 |
| `OnInspectorGUI()` | 섹션 목록 렌더링 + "Remove Readme Assets" 버튼 |
| `RemoveTutorial()` | 확인 다이얼로그 후 `Assets/TutorialInfo` 폴더와 Readme 에셋을 삭제 |

## 비고
게임 로직과 무관한 템플릿 잔재. [Readme.md](Readme.md) 참고.
