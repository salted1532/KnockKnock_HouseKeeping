# 0181 – README 갱신 (doc/0159~0180 반영)

## 요청
Readme 파일 갱신.

## 범위
`README.md` 만. `Docs/*.md`(스크립트 문서)는 손대지 않음. 마지막 갱신 doc/0158 이후 doc/0159~0180 분량.

## 반영한 변경
- **신규 스크립트**: `RoomNumberLabel`(0161), `MainMenu`(0165), `PauseMenu`(0166), `ReceptionIdCard`(0168·0173),
  `HoverTextOutline`(0169), `LightFlicker`(0170), `SleepwalkerNote`(0171), `GuestWalkBob`(0172·0180),
  `SpriteLightResponse`(0179) → 프로젝트 구조 폴더 주석 + 핵심 스크립트 표 + 구현 완료 기능.
- **동작 변경**: 새벽 행동력은 대화 종료 시 소모(0160), 화면 비율 레터박스(0162), ActionPointsBar "행동력" 라벨(0166),
  접객 모드 던지기/사용 차단·슬롯은 허용(0167·0176), 저녁 손님 최대 5명(0175), CarSpawner 낮에만(0177),
  접객 종료 시 지정 앵커 복귀(0178), MoneyHud `$` 초록(0180), HUD BG 자동 크기(0159), DayEndTitle 페이드(0168),
  Poster 하얗게 뜨던 문제 수정(0174).
- **손님**: 카탈로그 5→9명, `test1~5` → 나그네/회사원/거만한 남성/젊은 여성/노인 (0167).
- **로드맵**: 판별 로직 항목을 "신분증/노트 표시는 됨, 위조 판정·브리핑 콘텐츠·밤 판정 연동이 남음" 으로 갱신.

## 검증
`git diff --stat README.md` 확인. 코드 변경 없음.
