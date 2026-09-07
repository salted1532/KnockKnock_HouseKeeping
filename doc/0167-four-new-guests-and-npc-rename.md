# 0167 – 신규 손님 4명 + 기존 5명 이름·데이터 정리

## 요청
- 접객 손님 4개 추가: 트럭기사, 아버지와 아들, 사진작가, 목사. 대사는 자연스럽게 구성.
- 기존 5명 NpcData 이름을 `test1~5` → 나그네/회사원/거만한 남성/젊은 여성/노인.
- 정면/뒷모습/옆모습 스프라이트 연결. 화남 표정은 추후 → 칸 비움.

## 조사
- 기존 5명 `Assets/My/Scripts/Dialogue/NPC_Data/Npc_*.asset` 는 **이미 스프라이트 4종(정면/화남/뒷모습/옆모습) 다 연결됨**. 이름만 `test1~5`.
- 스프라이트: `Assets/My/image/숙박객/<이름>/` (트럭기사/아버지와아들/사진작가/목사/나그네/회사원/거만한남성/여성/노인). PNG 는 textureType Sprite.
  - 신규 4명 폴더엔 화남(_화남) 없음. 목사엔 옆모습도 없음.
- id2 기존 대사 = "브러시 견본" 방문판매원 → 이름 "회사원" 과 안 맞음.

## 적용

### 1. NpcData (`npcdata.csx` 실행)
- **rename**: id1 Drifter/나그네, id2 Salesman/회사원, id3 Arrogant Man/거만한 남성, id4 Young Woman/젊은 여성, id5 Old Man/노인. 스프라이트·화남 배선 그대로 둠.
- **신규 4개 생성** `Npc_6~9.asset`:
  | id | 이름 | stayNights | 아침청소 | 스프라이트 |
  |---|---|---|---|---|
  | 6 | Truck Driver / 트럭기사 | 1 | 허용 | 정면·뒷·옆 |
  | 7 | Father / 아버지 | 1 | 거부 | 정면·뒷·옆 |
  | 8 | Photographer / 사진작가 | 2 | 허용 | 정면·뒷·옆 |
  | 9 | Pastor / 목사 | 3 | 거부 | 정면·뒷 (옆 없음 → 비움) |
  - `angryPortrait` 전부 비움(추후), isSleepwalker/visitorOnly/refusesDawnKnock = 0.
- **NpcCatalog** = t:NpcData 전수 수집 → 9개 (id 1~9).

### 2. 대사
- `sample.csv` id2 3줄 재구성: "견본/브러시/청소솔" → "서류/계약서" (회사원에 맞게).
- **신규** `Assets/My/Data/Dialogue/guests_06-09.csv` — id 6~9 접객(인사3 + 이름/목적/숙박+선불분기/청소분기/거절분기 + checkin/checkin_paid/reject_final) + 새벽(인사 + 관찰질문3). 전부 `Neutral` 표정.
  - 트럭기사: 화물 안 봄·주차장 응시 / 아버지: 말없는 아들·밤에 아이 목소리 여럿 / 사진작가: 복도 촬영·사진에 없던 사람 / 목사: 끝방·청소 거부·문마다 손·"이 방에서 누가 떠났나".
- `Tools > Dialogue > Import CSV → DialogueDatabase` 재실행.

## 검증
```
[DialogueImporter] CSV 2개 / 262행 → 노드 166개.  goto 검사 통과  (미등록 npcId 경고 없음)
id6=19노드 id7=18 id8=20 id9=19
쿼리: 각 신규 NPC Greeting 3줄 / Questions 5~6 / checkin·checkin_paid·reject_final(outcome=Rejected) / Dawn 인사1+질문3
NpcData 9개: 이름·플래그·정면/뒷/옆 스프라이트 모두 확인. 신규 4명 angry 비움.
```

## 추가 (같은 요청 이어서) — 옆모습·화남 연결
사용자가 목사 옆모습 + 신규 4명 화남 스프라이트를 추가 → `wire_angry_side.csx`:
- id6 트럭기사 `angryPortrait` = 트럭기사_화남
- id7 아버지 `angryPortrait` = 아버지와아들_화남
- id8 사진작가 `angryPortrait` = 사진작가_화남
- id9 목사 `angryPortrait` = 목사_화남, `sidePortrait` = 목사_옆모습 (신규)
- **검증: 9명 전원 portrait 4종(정면/화남/뒷/옆) 완비.**

## 추가 — 편성 + id5 stayNights
- **랜덤 등장은 이미 동작 중**: `InGame` 씬 `ReceptionManager.testShuffleAllGuests = true` → 매 저녁 `NpcCatalog` 전체를 Fisher-Yates 셔플해 순서대로 등장(이미 투숙 중 = `Verdict.Approved` 는 제외). 카탈로그가 9명이 되면서 자동으로 9명 랜덤. **캠페인 편성은 이 모드에서 무시됨.**
- `Campaign.asset` day1 `eveningGuestIds` → `[1..9]` 로 갱신(편성도 맞춤 — testShuffleAllGuests 를 끄면 이 순서로 나옴).
- **id5 노인 `stayNights` 3 → 7** (대사 "일주일" 과 일치).

## 미처리 / 참고
- **화남 대사**: 스프라이트는 다 연결됐지만 CSV `expression` 은 아직 전부 `Neutral` → 화남 초상화가 실제로 뜨진 않음. 거절/추궁 분기 몇 줄에 `Angry` 를 넣으면 노출됨(별도).
- 매 저녁 9명 전원 등장이 길면, `ReceptionManager` 에 "하루 N명만 랜덤" 옵션을 추가하는 건 별도 작업.
- **id5 노인** `stayNights=3` 인데 대사는 "일주일" — 기존 불일치, 이번 범위 밖(요청은 rename만). 7로 올리려면 별도.
- **Campaign 편성** 안 건드림 — day 1 은 여전히 손님 1~5. 신규 4명을 인게임에서 보려면 `Campaign.asset` `days` 에 일차 추가하고 `eveningGuestIds` 에 6~9 넣어야 함.
- `아버지와아들` 컨셉: 스프라이트에 아들이 보이므로 "보이지 않는 아이"(손님-정보.md #7) 대신 "말없이 응시하는 아들 + 밤에 아이 목소리가 여럿" 으로 조정.
