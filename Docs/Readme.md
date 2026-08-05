# Readme.cs

- 경로: `Assets/TutorialInfo/Scripts/Readme.cs`
- 타입: `ScriptableObject`
- 출처: Unity 기본 템플릿(3D Sample Scene 등) 생성 시 자동 포함되는 보일러플레이트. 프로젝트 고유 로직 없음.

## 역할
에디터 인스펙터에 표시할 튜토리얼/안내문 데이터를 담는 순수 데이터 컨테이너.

## 필드
| 필드 | 타입 | 설명 |
|---|---|---|
| `icon` | `Texture2D` | 헤더에 표시할 아이콘 |
| `title` | `string` | 제목 |
| `sections` | `Section[]` | 본문 섹션 배열 |
| `loadedLayout` | `bool` | 에디터 레이아웃(`Layout.wlt`) 적용 여부 플래그 |

### `Section` (nested, `[Serializable]`)
`heading`, `text`, `linkText`, `url` — 섹션 하나(제목/본문/링크 텍스트/링크 URL).

## 연관 스크립트
- [ReadmeEditor.md](ReadmeEditor.md) — 이 데이터를 인스펙터에 그리는 커스텀 에디터

## 비고
게임 로직과 무관한 템플릿 잔재. 실제 개발 착수 시 `Assets/TutorialInfo` 폴더째 제거해도 무방 (`ReadmeEditor`의 "Remove Readme Assets" 버튼이 이 삭제를 수행함).
