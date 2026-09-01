# 0138 - 결정 선택지는 한 번 고르면 허브에서 사라짐

날짜: 2026-09-02
관련: `doc/0120`(stay 선택지), `doc/0135`(clean 선택지), `doc/0137`(결제 — 선불/후불 노드), `Assets/My/Scripts/Dialogue/DialogueRunner.cs`

> 참고: 원래 `doc/0137` 로 만들었으나 병행 세션의 결제 시스템 문서와 번호가 겹쳐 `0138` 로 이동.

## 요청 (원문)

> 현재 선불을 받는다나 어떤걸 결정하는 선택지가 있는데 해당 선택지를 선택을 하면 해당 질문 선택지는 사라지도록해줘 왜냐면 선택을 번복할수도 있기 때문에
> 그냥 질문은 반복해서 선택할수 있지만 결정하는 선택지는 한번 선택하면 번복을 못하도록 하는게 좋을거 같아

## 규칙

**질문 허브의 Question 노드에 `choices` 가 있으면 = "결정 토픽".** 플레이어가 그 안의 선택지를
하나 고르면 그 토픽은 이 손님 대화 동안 허브에서 사라진다 (번복 불가).

`choices` 없는 정보성 질문(이름·소음·날씨 등)은 계속 반복 선택 가능.

## 구현 — `DialogueRunner`

```csharp
private readonly HashSet<string> consumedTopics = new();
public void ResetConsumedTopics() => consumedTopics.Clear();   // 새 손님/새 노크 때 호출
```

`QuestionHub` 루프:
```csharp
var all = database.Query(npc.id, situation, day, EntryRole.Question);
var questions = all.Where(q => q.choices.Count == 0
                            || string.IsNullOrEmpty(q.nodeKey)
                            || !consumedTopics.Contains(q.nodeKey)).ToList();   // 소진된 결정 토픽 제외
...
var chosen = questions[pick];
yield return PlayNode(..., chosen);
if (chosen.choices.Count > 0 && !string.IsNullOrEmpty(chosen.nodeKey))
    consumedTopics.Add(chosen.nodeKey);
```

- `Play()` 안에서는 안 지운다 → **같은 손님 대화를 재재생(`RequestDialogueReplay`, 빈손 클릭)해도 결정 유지.**
- `ReceptionManager` 가 큐에서 **새 손님** 시작 시 `ResetConsumedTopics()` (기존 `pendingCleaning = null` 옆).
- `KnockEffect` 가 **노크(탐문)마다** `Play(Dawn)` 직전 `ResetConsumedTopics()`.

## 데이터상 분류 (검증)

| npc | 한 번만 (선택지 有) | 반복 (정보성) |
|---|---|---|
| 1 | where, **stay**, **clean** | name, reject |
| 2 | shaking, **stay**, **clean** | case, reject |
| 3 | id, **stay** | wait, clean, reject |
| 4 | alone, **stay** | road, clean, reject |
| 5 | trip, **stay**, need, **clean** | weather, reject |

`reject` 는 `choices` 없이 `goToNode=reject_insist` 라 항상 반복 가능 (여러 번 시도하다 커밋). ✅

## 부수 영향

`where`/`shaking`/`alone`/`trip`/`need` 도 선택지가 있어 한 번만 물어보게 됨 — 대개 무해하나
`trip`(노인 이야기 3버전)·`need`(담요/식사 제안)는 한 종류만 듣게 됨. 반복 필요하면 그 토픽을
선택지 대신 goto 체인으로 재구성하거나 `once` 플래그를 별도로 (지금은 안 함).

## 영향 파일

```
Dialogue/DialogueRunner.cs   수정  consumedTopics + ResetConsumedTopics + QuestionHub 필터
Game/ReceptionManager.cs     수정  새 손님 시 ResetConsumedTopics()
Interaction/Effects/KnockEffect.cs  수정  노크마다 ResetConsumedTopics()
Docs/DialogueSystem.md       갱신
```

## 상태

2026-09-02 코드 + 컴파일(Error 0) + 데이터 분류 검증 완료.
허브 UX(선택 후 토픽 사라짐)는 코드·데이터 확인까지 — 인게임 클릭 검증은 사용자.
