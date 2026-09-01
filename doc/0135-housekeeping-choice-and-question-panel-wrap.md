# 0135 - 하우스키핑 선택지(대사+선택) + 질문 버튼 2줄 배열

날짜: 2026-09-02
관련: `doc/0132`(체크아웃/아침 청소 — `allowsMorningCleaning`), `doc/0120`(stay 선택지 패턴), `Assets/My/Data/Dialogue/sample.csv`, `Docs/DialogueSystem.md`

## 요청 (원문)

> (선택) 접객 CSV 에 손님 청소 성향 대사 한 줄씩 <- 먼저 요청하는 손님도 있고
> 선택지에도 하우스키핑을 할지 물어보는식으로 해줘
> 그리고 지금 선택지 버튼들이 너무 옆으로만 길어지고 있어서 5개 이상부턴 밑으로 배열되도록 해줘 선택지 버튼들이

## A. 하우스키핑 대사 + 선택지

### 모델

`doc/0132` 의 `NpcData.allowsMorningCleaning` = **기본값**. 접객 대화에서 플레이어가 물어보고
손님이 답하면(`clean_yes` / `clean_no` 노드 도달) 그 결과가 기본값을 **덮는다**.

- 물어보지 않음 → `NpcData.allowsMorningCleaning` 그대로 (id 1·2·5 = 청소 / 3·4 = 안 함)
- `clean_yes` 도달 → `cleaningRequested = true`
- `clean_no` 도달 → `cleaningRequested = false`

id 3·4 는 `clean_yes` 노드 자체가 없음 — 물어봐도 손님이 거절만 함 (기본값 false 유지).

### 코드 — `ReceptionManager` 훅

| | |
|---|---|
| `bool? pendingCleaning` | 손님마다 리셋 (`null` = 안 물음). |
| `BeginSession` / `EndSession` / `OnDestroy` | `DialogueRunner.OnNodeReached += HandleReceptionNode` 구독·해제 |
| `HandleReceptionNode(npc, nodeKey)` | `"clean_yes"` → `pendingCleaning=true` / `"clean_no"` → `false` |
| 체크인 시 | `CheckIn(...)` 뒤 `pendingCleaning.HasValue` 면 `GuestManager.Get(npc).cleaningRequested` 덮어씀 |

`DialogueRunner` 는 이미 `PlayNode` 에서 `nodeKey` 있는 노드마다 `OnNodeReached` 발동 — 새 훅 불필요.

### 데이터 — `sample.csv` (임포트: 142행 → 노드 89개, goto 검사 통과)

| npc | greeting 추가 | `clean` 토픽 |
|---|---|---|
| 1 떠돌이 | — (중립) | Q + [매일 아침 정리]`clean_yes` / [부를 때만]`clean_no` |
| 2 외판원 | **"아침에 방 좀 정리해 주실 수 있을까요? 가방은 그대로 두시고요."** (먼저 요청) | Q + yes/no (yes 도 "견본은 옮기지 마세요") |
| 3 거만한 단골 | **"내 방엔 아무도 발 들이지 마. 청소부도, 너도."** (먼저 거절) | Q 1줄 (거절만, 선택지 없음) |
| 4 경계심 여성 | **"청소는 안 하셔도 돼요. 방은 제가 알아서 할게요."** (먼저 거절) | Q 1줄 (거절만) |
| 5 노인 | **"아침마다 잠깐 정리해 주시면 고맙겠소."** (먼저 요청) | Q + yes/no |

`clean` = `stay` 토픽과 같은 구조 (질문 1줄 + 선택지 2 → `clean_yes`/`clean_no` Node, 빈 goto = 허브 복귀).

## B. 질문 버튼 2줄 배열 (5개 이상)

`QuestionPanel` 의 `Button_Horizontal` 을 **`HorizontalLayoutGroup` → `GridLayoutGroup`** 으로 교체
(cellSize 240×60, spacing 12, `FixedColumnCount`). 버튼 프리팹이 고정 240×60 이라 그리드 셀에 딱 맞음.

### 코드 — `QuestionPanel`

| 추가 | |
|---|---|
| `int wrapAfter = 4` | 버튼(대화 종료 포함)이 이 수를 넘으면 여러 줄 |
| `RectTransform dialogueArea` | 대사 영역 (`Dialogue`). 버튼이 여러 줄이면 아래 여백을 늘림 |
| `ArrangeGrid(total)` | `rows = total<=wrapAfter ? 1 : ceil(total/wrapAfter)`, `cols = ceil(total/rows)` → `grid.constraintCount` |
| `FitPanelToButtons()` | 폭(기존) + **패널 높이 `base + (rows-1)*rowStride`**(pivot 하단 → 위로 성장) + `dialogueArea.offsetMin.y = base + (rows-1)*rowStride` |
| `Close()` | 패널 높이·`dialogueArea` 여백 기본값 복원 |

`rowStride = grid.cellSize.y + grid.spacing.y` (72).

### 씬 배선 (uloop, `InGame.unity` 저장됨)

- `Button_Horizontal`: `HorizontalLayoutGroup` 제거 → `GridLayoutGroup` 추가. `ContentSizeFitter` v=PreferredSize.
- `Dialogue`: 앵커 `(0,0.34)-(1,1)` → 스트레치 `(0,0)-(1,1)`, `offsetMin (16,96)` `offsetMax (-16,-16)` (버튼 1줄분 하단 확보).
- `QuestionPanel.dialogueArea` = `Dialogue`, `wrapAfter` = 4.

### 검증 (플레이)

7버튼(질문 6 + 대화 종료) → **4 + 3 두 줄**, 패널 372px(기본 300+72)로 위로 커지고 대사 영역이 위로 밀림.
`Close()` → 300 / 여백 96 복원 확인. 2·3버튼(선택지 서브메뉴)은 기존처럼 한 줄.

## 영향 파일

```
Dialogue/QuestionPanel.cs        수정  wrapAfter/dialogueArea + ArrangeGrid + 높이 성장
Game/ReceptionManager.cs         수정  pendingCleaning + OnNodeReached 훅 + 체크인 시 덮어쓰기
Data/Dialogue/sample.csv         데이터 clean 토픽 5명 + greeting 4명
Data/Dialogue/DialogueDatabase.asset  재생성 (임포터)
Scenes/InGame.unity              씬  Button_Horizontal Grid 전환 + Dialogue 앵커 + QuestionPanel 배선
```

## 스킵 / 메모

- `clean_yes` 로 "덮어쓰기" 는 체크인 직전 마지막 선택만 반영 (재대화 시 재선택). 정상.
- id 3·4 에 `clean_yes` 경로 없음 = 게임적으로 맞음(손님이 허락 안 함). 플레이어가 강제 못 함.
- 그리드 셀 240 고정이라 긴 라벨은 셀 안에서 2줄 랩. 라벨 길이 다듬기는 콘텐츠 튜닝.
- 폰트 아틀라스(`Galmuri11 SDF.asset`)가 플레이 중 베이크돼 16MB 로 커졌던 것 → `git checkout` 로 되돌림(동적 아틀라스, 런타임 재생성).

## 상태

2026-09-02 코드 + 씬/CSV + 컴파일(Error 0) + 플레이 검증(버튼 2줄) 완료.
`clean_yes/no` → `cleaningRequested` 전체 플로우(접객→체크인→다음날 아침 개방)는 미검증 — 코드·데이터 확인까지.
