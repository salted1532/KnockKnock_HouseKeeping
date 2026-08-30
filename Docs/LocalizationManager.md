# LocalizationManager

`Assets/My/Scripts/Localization/LocalizationManager.cs`

게임의 표시 언어(영어/한글)를 관리. 씬에 빈 GameObject 하나로 배치.

## 필드

| 필드 | 설명 |
|---|---|
| `language` (`Language` enum) | 인스펙터 콤보 박스. `English` / `Korean`. **게임 시작 시** 이 값으로 고정 |

## 동작

- `Awake` 에서 `language` → 정적 `Current` 로 확정. 런타임 전환은 없음.
- `LocalizationManager.Korean` (bool), `LocalizationManager.Current` (enum) 를 다른 스크립트가 표시 직전에 읽음 — 이벤트·리프레시 불필요.
- `LocalizationManager.T(en, ko)` : 정적 UI 문자열용 인라인 페어. `ko` 가 비면 `en` 폴백. 키 테이블 없음.
- `[DefaultExecutionOrder(-500)]` — 다른 컴포넌트보다 먼저 `Awake`.

## 텍스트가 언어를 읽는 곳

| 출처 | 방식 |
|---|---|
| 대사 줄 | `DialogueLine.Text` (CSV `text_en` / `text_ko`) |
| 질문·선택지 버튼 | `DialogueEntry.Label` / `Choice.Label` (CSV `label_en` / `label_ko`) |
| 손님 이름 | `NpcData.DisplayName` (`displayName` / `displayNameKo`) |
| 상호작용 프롬프트 | `Interactable.Prompt` → `T(en, ko)` |
| 시간대 라벨 | `PhaseLabel` |
| "대화 종료" 버튼 | `QuestionPanel.Show` → `T("Done", "대화 종료")` |
| 정적 UI 라벨 (메뉴/HUD) | `LocalizedLabel` 컴포넌트 |

## LocalizedLabel

`Assets/My/Scripts/Localization/LocalizedLabel.cs` — `TMP_Text` 와 같은 오브젝트에 부착.
`english` / `korean` 두 필드를 인스펙터에 입력하면 `OnEnable` 에서 현재 언어로 채운다.
코드가 값을 넣지 않는 정적 텍스트(타이틀, 안내문 등)에 사용.

## 폰트

기본 폰트는 `Galmuri11 SDF`, 폴백은 `LiberationSans SDF` (`TMP Settings`). Galmuri 에 없는
글리프(`·`, 악센트 문자 등)는 자동으로 폴백에서 렌더.
`Tools > Localization > Apply Galmuri Font` 로 씬/프리팹의 모든 `TMP_Text` + `TMP Settings` 를 일괄 설정.
