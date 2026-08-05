# Docs/

스크립트별 레퍼런스 문서 모음. 세션 로그는 `doc/`(소문자, 별도 폴더)에 기록됨 — 혼동 금지.

> Windows는 폴더/파일명 대소문자를 구분하지 않으므로 `Doc/`(대문자)와 `doc/`(소문자)는 실제로 같은 폴더가 된다.
> 이를 피하기 위해 스크립트 레퍼런스 문서는 `Docs/`(복수형), 세션 로그는 `doc/`(단수, 소문자)로 이름을 분리한다.

## 현재 스크립트 목록

프로젝트에는 아직 Unity 기본 템플릿이 생성한 스크립트 2개만 존재하며, 게임 고유 로직 스크립트는 없음 (2026-08-06 기준).

| 스크립트 | 경로 | 설명 |
|---|---|---|
| [Readme.cs](Readme.md) | `Assets/TutorialInfo/Scripts/Readme.cs` | 템플릿 안내문 데이터 컨테이너 |
| [ReadmeEditor.cs](ReadmeEditor.md) | `Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs` | 위 데이터의 커스텀 인스펙터 |

새 게임플레이 스크립트가 추가되면 이 폴더에 스크립트당 1개의 `.md` 문서를 추가하고 이 표를 갱신한다.
