# 0158 - README / Docs 갱신 (일과 게이트·행동력·뉴스 브리핑·환경)

날짜: 2026-09-07
관련: `doc/0144`(아침 할일·유도 마커), `doc/0145`(뉴스 브리핑·PhaseMessage), `doc/0148`(자동차·점심 일과), `doc/0150`(N일차 타이틀), `doc/0151`(전환 중 커서 숨김), `doc/0156`(가로등), `doc/0157`(DayEndTitle off-by-one)

## 요청 (원문)

> Readme파일 갱신좀 해줘. 새로 추가된 스크립트 문서 만들어저 정리해주고 연결까지 해주고 현재 진행된거 갱신하고

## 배경

`README.md` / `Docs/Overview.md` 는 `ed8bb250`(doc/0143, 2026-09-02) 이후 안 갱신.
그 사이 커밋 `c6460dcd`~`80641f34` 로 스크립트 19개 추가 — 이 중 `NightNewsBriefing.md`·`PhaseMessage.md`
2개만 문서화됨(커밋 `72135b17`, 그나마 Overview 미연결).

## 변경

### `Docs/` 신규 9개

| 파일 | 커버하는 스크립트 |
|---|---|
| `MorningTasks.md` | `Game/MorningTasks.cs` + `Conditions/TasksCompleteCondition.cs` |
| `LunchTasks.md` | `Game/LunchTasks.cs` + `Interaction/LunchTaskTarget.cs` + `Conditions/LunchTasksCompleteCondition.cs` |
| `ActionPoints.md` | `Game/ActionPoints.cs` + `Conditions/ActionPointsDepletedCondition.cs` + `UI/ActionPointsHud.cs` |
| `DayEndTitle.md` | `Game/DayEndTitle.cs` |
| `MonitorViewEffect.md` | `Interaction/Effects/MonitorViewEffect.cs` |
| `NewsBriefingEffect.md` | `Interaction/Effects/NewsBriefingEffect.cs` |
| `ObjectiveMarker.md` | `UI/ObjectiveMarker.cs` + `Interaction/OutlineWhileInteractable.cs` + `UI/ExitHintGauge.cs` |
| `CarSpawner.md` | `Environment/CarSpawner.cs` |
| `ActiveInPhases.md` | `Environment/ActiveInPhases.cs` |

(이미 있던 `NightNewsBriefing.md`·`PhaseMessage.md` 는 그대로, Overview 에 연결만 추가)

### `Docs/Overview.md`

- 상호작용 표: `MonitorViewEffect`·`NewsBriefingEffect`·`TasksCompleteCondition`·`LunchTasksCompleteCondition`·`ActionPointsDepletedCondition`·`LunchTaskTarget`·`OutlineWhileInteractable` 행 추가
- 게임 진행/환경 표: `PhaseMessage`·`DayEndTitle`·`MorningTasks`·`LunchTasks`·`ActionPoints`·`NightNewsBriefing`·`ObjectiveMarker`·`ActiveInPhases`·`CarSpawner` 행 추가

### `README.md`

- **핵심 루프** — 4단계 각각에 할일 게이트(MorningTasks/LunchTasks/ActionPoints) + 유도 표식 반영, 5번째 "일차 종료 뉴스 브리핑" 단계 추가, 전환 중 커서 숨김
- **프로젝트 구조** — `Game/`·`UI/`·`Environment/` 주석에 신규 스크립트
- **행동 카테고리 표** — 아침/점심 종료 = 할일 게이트, 하루 종료 = `NewsBriefingEffect` + 행동력 게이트. 특수 효과에 `MonitorViewEffect`
- **핵심 스크립트 표** — 9행 추가 (일과·행동력·브리핑·타이틀·유도·환경)
- **코드 아키텍처 mermaid** — `TASK`(할일 게이트) + `NNB`(뉴스 브리핑) 노드 + 게이트/전환 엣지 추가
- **구현 완료 기능** — `하루 진행/환경` 에 게이트·유도 표식 추가, `일차 종료 연출`·`환경/분위기` 섹션 신규, UI 모드/새벽 탐문 갱신
- **로드맵** — 완료분(4단계 게이트, 행동력, 브리핑 골격) 반영, 남은 것(일과 다종화·점심 내용·판별 연동)으로 정리
- **수정 필요 표** — 폴링 비용, 트리거 수동 배선, `HideHud` 한계 3행 추가

## 검증

- README mermaid: `npx @mermaid-js/mermaid-cli@11` → 1/1 렌더 OK
- 코드 변경 없음 (문서만)

## 상태

2026-09-07 완료.
