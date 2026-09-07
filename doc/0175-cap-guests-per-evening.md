# 0175 – 한 저녁 손님 최대 5명

## 요청
손님 오는 건 최대 5명으로 한정.

## 배경
`ReceptionManager.testShuffleAllGuests = true` (doc/0167) → 매 저녁 `NpcCatalog` 전체(투숙 중 제외)를 셔플해 전부 등장. 카탈로그가 9명이라 매일 9명이 옴.

## 수정 (`Assets/My/Scripts/Game/ReceptionManager.cs`)
- 신규 필드 `[SerializeField] int maxGuestsPerEvening = 5;` (0 이하 = 제한 없음).
- `BuildGuestIds()` 재구성: 셔플/캠페인 편성으로 목록을 만든 뒤 마지막에 `maxGuestsPerEvening` 개로 자름.
  - 셔플 모드: 셔플 후 앞에서 5명 = 매일 랜덤한 5명.
  - 캠페인 모드: `eveningGuestIds` 를 **복사**(에셋 원본 안 건드림) 후 앞 5명.
- `InGame.unity` 씬 `ReceptionManager.maxGuestsPerEvening` = 5 로 저장.

## 검증
```
scene ReceptionManager.maxGuestsPerEvening = 5
BuildGuestIds() → count=5  ids=[7,5,2,6,1]   (9명 중 랜덤 5명)
```
`uloop compile` 클린.

## 참고
- 하루 손님 수를 바꾸려면 `ReceptionManager` 인스펙터의 **Max Guests Per Evening**.
- 이미 투숙 중(Approved)인 손님은 여전히 풀에서 제외 → 밤이 갈수록 후보 풀이 줄어듦.
