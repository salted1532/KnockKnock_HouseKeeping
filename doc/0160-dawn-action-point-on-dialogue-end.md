# 0160 – 새벽 행동력, 대화 종료 시점에 소모

## 요청
"새벽에 대화가 끝나면 그때 행동력이 닳도록 수정"

## 이전 동작
`KnockEffect.Knock` 에서 손님이 문을 여는 순간(대화 시작 전) `ActionPoints.Instance?.Use(1)` 호출.
→ HUD 행동력 칸이 대화 시작하자마자 닳음. ESC로 중간에 나가도 이미 소모됨.

## 수정 (`Assets/My/Scripts/Interaction/Effects/KnockEffect.cs`)
```
  DialogueRunner.Instance.ResetConsumedTopics();
- ActionPoints.Instance?.Use(1);
  DialogueRunner.Instance.Play(npc, bubble, Situation.Dawn, _ => done = true);
  while (!done && Locked(anchor)) yield return null;
+ if (done) ActionPoints.Instance?.Use(1);   // 대화를 끝까지 마쳤을 때만 (ESC 취소 시 X)
```
- `done` = `Play` 의 onResult 콜백. 정상 종료·거절 노드 종료 시 true. ESC 취소(`DialogueRunner.Cancel()` → StopAllCoroutines)면 콜백 안 옴 → false.
- 거절 손님(문 안 열어줌)은 이전처럼 소모 없음 (accept 분기 밖).
- `ActionPoints.cs` 클래스 주석도 갱신.

## 사용자 확정
ESC 취소 시 행동력 소모 안 함.

## 검증
`uloop compile` 클린 (에러 0, 경고 0).
