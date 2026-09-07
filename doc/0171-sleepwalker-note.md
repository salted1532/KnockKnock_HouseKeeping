# 0171 – 숙소 노트 = 일차별 몽유병 환자 구별법

## 요청
현재 안 쓰이는 `note` 오브젝트를, **일차마다 다른 "몽유병 환자 구별법"** 을 보여주는 노트로 구현.
내용은 `1. ㅇㅇㅇ  2. ㅇㅇㅇ  3. ㅇㅇㅇ` 처럼 번호 매긴 목록.

## 현황 (조사)
- `Canvas/note_image` (InGame 씬, HUD Canvas, 700×900 양피지색 Image, inactive) + 자식 `Text (TMP)` = 플레이스홀더 `"sex"`, 200×50 rect 가 엉뚱한 위치(-224, 405), 흰색 글자(양피지색 배경이라 대비 나쁨)
- `Owner's_Motel_Room/note` 에 `ShowPanelEffect`(content = note_image) — 플레이어가 사장 방의 `note` 상호작용 → `note_image` 패널 ON, ESC 로 닫힘. (같은 방 `ID` 는 신분증 노트)
- 일차별 데이터 패턴: `CampaignData`(SO, `Assets/My/Scripts/Dialogue/Campaign.asset`) 의 `List<DayPlan>` — `DayPlan.day` + per-day 리스트(`newsLinesEn/Ko` 등). 런타임 조회 `campaign.Day(DayPhaseManager.Instance.DayCount)` → 없으면 `Day(1)` 폴백. en/ko 는 `LocalizationManager.Korean` 로 리스트 선택.
- 현재 Campaign.asset 엔 day 1 만 편성됨

## 작업

### 1. `CampaignData.DayPlan` 에 필드 추가
```csharp
[Header("몽유병 환자 구별법 노트 (doc/0171)")]
[Tooltip("숙소 노트에 뜨는 구별법. 한 항목 = 한 줄, 앞에 '1. 2. 3.' 자동 번호")]
[TextArea(1, 3)] public List<string> sleepwalkerHintsEn = new();
[Tooltip("비면 영어(sleepwalkerHintsEn) 사용")]
[TextArea(1, 3)] public List<string> sleepwalkerHintsKo = new();
```

### 2. 신규 `Assets/My/Scripts/Game/SleepwalkerNote.cs` (~55줄)
`note_image` 에 부착. `OnEnable`(패널 열릴 때) 마다 오늘 일차의 힌트를 읽어 TMP 텍스트를 채운다.
```
제목(몽유병 환자 구별법)
(빈 줄)
1. 첫 번째 힌트
2. 두 번째 힌트
3. ...
```
- day = `DayPhaseManager.Instance.DayCount`, `campaign.Day(day) ?? campaign.Day(1)`
- ko 리스트 있으면 ko, 없으면 en
- 힌트 없으면 `(오늘 자 기록 없음.)`
- 제목 en/ko 는 인스펙터 필드 (기본 "How to spot the sleepwalker" / "몽유병 환자 구별법")

### 3. 씬 배선 (`Assets/Scenes/InGame.unity`)
- `Canvas/note_image` 에 `SleepwalkerNote` 부착 → `campaign` = Campaign.asset, `label` = 자식 `Text (TMP)`
- 자식 `Text (TMP)` 정리: 앵커 stretch + 40px 마진으로 노트 채우기, TopLeft, wrap on, 폰트 ~28, **글자색 진한 갈색**(양피지 대비)

### 4. Campaign.asset day 1 샘플 힌트 채우기
동작 확인용 3줄(ko/en). 실제 문구·나머지 일차는 디자이너가 CampaignData 인스펙터에서 편집.
- ko: "말을 걸면 대답이 한 박자 느리다" / "전날 밤에 한 일을 기억하지 못한다" / "복도에서 마주쳐도 눈을 잘 안 맞춘다"
- en: "They answer a beat too slowly when spoken to." / "They don't remember what they did the night before." / "They avoid eye contact even when you pass them in the hall."

## 결과 (구현·플레이모드 검증 완료)
- `CampaignData.DayPlan` + `sleepwalkerHintsEn/Ko`, 신규 `SleepwalkerNote.cs`, `uloop compile` 에러 0
- `Canvas/note_image` 에 `SleepwalkerNote` 부착: `label`=자식 TMP, `campaign`=Campaign.asset. 자식 텍스트 = stretch+48px 마진, TopLeft, 폰트 30, 진한 갈색(#29 1C 0F), lineSpacing 8
- Campaign.asset day1 힌트 ko/en 3줄씩 입력
- 플레이모드(Korean, day 1): 노트 열면
  ```
  몽유병 환자 구별법

  1. 말을 걸면 대답이 한 박자 느리다
  2. 전날 밤에 한 일을 기억하지 못한다
  3. 복도에서 마주쳐도 눈을 잘 안 맞춘다
  ```
  로 표시. 스크린샷 확인 — 양피지색 노트에 가독성 OK.
- **참고:** `note_image` 배경은 스프라이트 없이 진한 노랑 단색 rect. 종이 텍스처 스프라이트를 넣으면 더 노트다워짐(이번 범위 밖).

## 안 하는 것
- 힌트를 `NpcData.isSleepwalker` 등에서 자동 생성 — 구별법은 작가가 일차별로 쓰는 내용이라 수동 입력
- 노트 상호작용/프리팹 구조 변경 없음 (기존 ShowPanelEffect 그대로)

## 수정 파일
- `Assets/My/Scripts/Game/CampaignData.cs` (필드 2개)
- `Assets/My/Scripts/Game/SleepwalkerNote.cs` (신규)
- `Assets/Scenes/InGame.unity` (note_image 에 컴포넌트 + 자식 텍스트 정리)
- `Assets/My/Scripts/Dialogue/Campaign.asset` (day 1 샘플 힌트)
