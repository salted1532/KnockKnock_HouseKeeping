# 0155 – 뉴스 브리핑 매 일차 작동 + 대사집 없는 날 1일차로 대체

## 요청
매 일차 새벽 → TV 브리핑이 작동하도록. 오늘 대사집이 없어도 일단 1일차 TV 대사가 나오도록.

## 원인
`Campaign.asset` 에 `day: 1` 편성만 존재 → 2일차 이후 `campaign.Day(day) == null` → `LinesForToday()` null → `NightNewsBriefing.Play()` 가 false 반환 → `NewsBriefingEffect` 가 브리핑 없이 바로 아침 전환.

## 수정 — `NightNewsBriefing.cs` (LinesForToday / TodayPlan)

- `TodayPlan()`: 오늘 편성이 없거나(`Day(day)==null`) 뉴스 문구가 비었으면(`!HasLines`) → `campaign.Day(1)` 로 대체.
- `LinesForToday()`: plan 의 문구가 비어 있으면 `FallbackLines()`(캠페인 자체가 없을 때의 최후 1줄, en/ko).
- 결과: `Play()` 는 이제 항상 lines 를 받아 브리핑을 시작 → **매 일차 새벽마다 TV 작동**.
- `SlidesForToday()` 도 `TodayPlan()` 을 타므로 자동으로 1일차 슬라이드로 대체.

## 확인 (Play, 리플렉션)
- `campaign` 배선됨. `Day(2)`, `Day(3)` = null → `TodayPlan()` → `plan.day == 1`, `LinesForToday()` = 4줄(1일차 영어 대사).
- day1 도 정상 4줄.

## 참고
- 1일차 대사집은 `Campaign.asset` 에 이미 있음(en/ko 각 4줄). 2일차+ 편성 추가 시 그 날 대사가 우선.
- `FallbackLines()` 는 `campaign` 미배선/1일차도 비었을 때만. 디자이너가 실제 편성을 넣으면 안 쓰임.
