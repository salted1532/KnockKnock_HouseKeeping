# 0131 - 새벽 대화 5명 전원 + 거절 손님 1명

날짜: 2026-08-31
관련: `doc/0118`(새벽 노크), `doc/0121`(노크 거절 ScreenMessage), `Assets/My/Data/Dialogue/sample.csv`

## 요청

- 나그네(npc 1) 외 나머지는 새벽에 대화 불가 → 대화 추가
- 거절하는 npc 한 명 추가
- 거절 시 "노크 거절 받았다"가 잘 보이도록

## 변경

### `sample.csv` — Dawn 행 추가 (임포트: 121행 → 노드 78개)

| npc | Dawn 구성 |
|---|---|
| 1 떠돌이 | 기존 greeting + noise, **+ `heard`(복도 문소리 한 번), `leaving`(바로 떠남)** |
| 2 외판원 | greeting 2줄(더듬음) + `noise`(못 들었어요, 거의) / `night`(짐만 계속 쌈) / `seen`(커튼 계속 쳐놨어요) — 방어적·과잉설명 |
| 3 거만한 단골 | greeting 2줄(Angry) + `noise`(시끄러운 건 너뿐) / `night`(아무나 복도를 어슬렁대는데 잠이 오냐) / `halls`(내가 어떻게 알아) — 적대적, 복도 언급 |
| 4 경계심 여성 | **greeting/question 없음.** `Dawn/Node/refuse` 만: "...어두워지면 두드리지 말랬죠. 돌아가세요. 얘기는 아침에 하죠." |
| 5 노인 | greeting 2줄 + `noise`(**벽 너머 긁는 소리** 계속) / `night`(화장실 두 번, 복도는 비어 있었소) / `storm`(하늘이 가장자리부터 초록빛) — 실마리 제공 |

단서 연결: npc 3 "복도를 어슬렁", npc 1 "복도 문소리 한 번", npc 5 "벽 너머 긁는 소리".

### `Npc_4.asset`

`refusesDawnKnock = true` (체크인 대사 "어두워진 뒤엔 문 두드리지 마세요" 와 일관).

### 거절 피드백 명확화 — `KnockEffect.refuseMessages` (C# 기본값 + `Motel_Room.prefab`)

| 기존 | 변경 |
|---|---|
| "노크가 거절된 것 같다." / "깊이 잠든 것 같다." / "응답이 없다." | **"노크가 거절됐다." / "문을 열어주지 않는다." / "문 너머로 대답만 돌아온다."** |

"잠든 것 같다"(못 들음) → 능동적 거절 뉘앙스로 통일.

### 거절 흐름 (검증 완료)

노크 → 화면고정 → `knockWait` → `refusesDawnKnock` →
1. `ScreenMessage.Show()` 랜덤 문구 → 화면 중앙 (검증: `text="노크가 거절됐다." alpha=1.00`)
2. `dawnPanel`(GameManager SpeechBubble)로 `refuse` 노드 대사 재생 (검증: `visible=True`)
3. 대사 끝 → `EndSequence()` 화면고정 해제

## 검증

- CSV 임포트 warning 0, `goto 검사 통과`
- `uloop compile` Error 0
- 플레이: npc 4 노크 → ScreenMessage "노크가 거절됐다." + 문 너머 "...돌아가세요..." 동시 표시, 화면고정 유지
- 플레이: npc 3 노크 → 손님 스폰(`거만한 남성_화남_0` 스프라이트, Angry 표정), Dawn 대화 진행
- 5명 전원 `db.Query(id, Dawn, ...)` 정상 (npc 4 는 refuse 노드만)

## 추가 — 노크 중 ESC 취소 (소프트락 수정)

증상: 노크 후 ESC 로 화면고정을 빠져나가도 `KnockEffect` 코루틴이 계속 돌아 3초 뒤 문이 열리고 대화창이 뜸 → 게임 조작은 돌아왔는데 커서가 없어 소프트락.

| 파일 | 변경 |
|---|---|
| `Dialogue/DialogueRunner.cs` | `Cancel()` 추가 — 진행 중 대화 즉시 중단 (`StopAllCoroutines` + `Running=false` + `activeBubble.Hide()` + `questionPanel.Close()`). `Run`/`SayRoutine` 가 `activeBubble` 추적 |
| `Interaction/Effects/KnockEffect.cs` | `Knock` 코루틴을 프레임별 감시 루프로 재작성. `Locked(anchor)` = `UIInteractionMode.IsTopAnchor(anchor)` (peer 추가). 대기·거절대사·수락대화 각 루프가 `&& Locked(anchor)` 조건 → ESC 로 앵커가 빠지면 즉시 탈출. 공통 정리부: `cancelled` 면 `DialogueRunner.Cancel()`, 항상 손님 제거 + `PeekDoor(0)`(문 닫기), 정상 종료만 `Exit()`, 항상 `self.enabled=true`+`busy=false` → **재노크 가능** |

동작: ESC → 화면고정 해제(기존) + 코루틴이 다음 프레임에 취소 감지 → 문 안 열림(열렸으면 닫힘) + 손님/대화 정리 + 다시 노크 가능.

검증: compile Error 0. `IsTopAnchor` before/enter/exit = False/True/False, `Cancel()` 무해 확인. (플레이모드 코루틴 타이밍은 에디터 비포커스로 CLI 검증 불가 — 로직·빌딩블록 확인)

## 상태

2026-08-31 완료 (Dawn 대사 5명 + 거절 손님 + ESC 취소).
