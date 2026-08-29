# 0106 - 상호작용 프롬프트 텍스트 영어화

## 요청
접객 모드에서 마우스 호버 시 뜨는 상호작용 텍스트(프롬프트)를 전부 영어로.

## 분석
- 호버 텍스트 = `CursorInteractor.cs:70` → `promptLabel.text = hovered.Prompt`
- `Interactable.Prompt` 는 `InteractionPrompt` enum 값을 문자열로 반환 (한글 enum 이름 또는 한글 리터럴)
- `InteractionPrompt.*` 참조는 전부 `Interactable.cs` 안에만 존재 → 이름 변경 안전
- 프리팹은 int 인덱스로 직렬화 → enum 순서 유지하면 매핑 그대로
- `customPrompt` 값: Guest 프리팹 "Check in" 하나뿐(이미 영어), 나머지 전부 빈 값

## 변경 (Assets/My/Scripts/Interaction/Interactable.cs 만)
enum 이름 (순서/인덱스 유지):

| idx | 기존 | 신규 | 화면 표시 |
|---|---|---|---|
| 0 | 상호작용 | Interact | Interact |
| 1 | 여닫기 | OpenClose | Open / Close |
| 2 | 켜고끄기 | Toggle | Turn on / Turn off |
| 3 | 줍기 | PickUp | Pick up |
| 4 | 사용 | Use | Use |
| 5 | 조사 | Inspect | Inspect |
| 6 | 정리하기 | CleanUp | Clean up |
| 7 | 밀기 | Push | Push |
| 8 | 화면고정 | ViewScreen | View |
| 9 | 직접입력 | Custom | (customPrompt) |
| 10 | 걸기 | Hang | Hang |
| 11 | 읽기 | Read | Read |
| 12 | 아침종료 | EndMorning | End shift |
| 13 | 점심종료 | EndNoon | End shift |
| 14 | 저녁종료 | EndEvening | Close up |
| 15 | 하루종료 | EndDay | Sleep |

- `Prompt` switch: 한글 리터럴 → 영어, `_ => ToString()` fallback 유지(Interact/Use/Push/Hang)
- `SyncEffectsToPrompt()` case 라벨 및 `promptType == PickUp` 갱신
- 주석은 한글 유지(화면에 안 나옴)

## 검증
`uloop compile` → Success, Error 0 (기존 무관 warning 1건).
프리팹/씬 수정 없음.
