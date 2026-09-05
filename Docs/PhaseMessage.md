# PhaseMessage

`Assets/My/Scripts/Game/PhaseMessage.cs`

특정 시간대로 넘어가 **페이드 인이 끝난 직후**([`DayPhaseManager`](DayPhaseManager.md) `OnPhaseChangeFinished`) 화면 중앙에 문구 1회 ([`ScreenMessage`](ScreenMessage.md)). [`PhaseLabel`](PhaseLabel.md) 과 동일한 구독 패턴 (`Start` 구독 / `OnDestroy` 해제).

한 GameObject 에 여러 개 붙여 시간대별로 쓸 수 있다.

## 필드

| 필드 | 설명 |
|---|---|
| `phase` (`DayPhase`) | 이 단계 진입 완료 시 발동 |
| `messageEn` / `messageKo` | 문구 (한쪽 비면 나머지로 폴백). 둘 다 비면 안 띄움 |

## 씬 배치

`GameManager` 에 컴포넌트로 추가. 현재:
- `phase=Dawn` — 저녁(접객) 종료 → 새벽 전환 직후 `"손님은 다 온 것 같다. 이제 조용한 시간이다."` (`doc/0145` 4차)

## 관련

[DayPhaseManager](DayPhaseManager.md) · [ScreenMessage](ScreenMessage.md) · [`doc/0145`](../doc/0145-nightly-tv-news-briefing.md)
