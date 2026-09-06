# 0157 – DayEndTitle "N일차" 하루 밀림 수정

## 요청
게임 중앙에 뜨는 "N일차" 카드가 HUD보다 하루 밀려서 나옴. 1일차 시작 → 자고 나면 "2일차"여야 하는데 "1일차"가 뜸.

## 원인
- `PhaseLabel` (HUD "N일차 · 아침"): `DayPhaseManager.DayCount` 그대로 사용 → 맞음
- `DayEndTitle` (중앙 카드): `DayCount - 1` ("방금 끝난 일차") 사용 → HUD와 하루 어긋남

`TransitionTo.AtBlack`이 Morning 진입 시 `DayCount++` 후 `OnPhaseChanged`/`OnPhaseChangeFinished` 발생. 둘 다 증가 후 값을 읽으므로 `DayCount`로 통일하면 일치.

## 수정 (`Assets/My/Scripts/Game/DayEndTitle.cs` `Handle`)
```
- int ended = DayPhaseManager.Instance.DayCount - 1;
- if (ended < 1) return;
- label.text = ... $"{ended}일차" : $"Day {ended}";
+ int day = DayPhaseManager.Instance.DayCount;
+ if (day < 2) return;   // 게임 시작 아침(1일차)엔 표시 안 함
+ label.text = ... $"{day}일차" : $"Day {day}";
```
클래스 주석도 갱신.

## 검증 (플레이모드)
| | DayCount | PhaseLabel | DayEndTitle |
|---|---|---|---|
| 1일차 시작 | 1 | `1일차 · 아침` | 안 뜸 ✓ |
| 사이클 1 후 | 2 | `2일차 · 아침` | `2일차` ✓ |
| 사이클 2 후 | 3 | `3일차 · 아침` | `3일차` ✓ |

컴파일 클린. 스크린샷에서 HUD·카드 "2일차" 일치 확인.

## 미처리 (별개, 에이전트가 3회째 지적)
`DayPhaseManager.Instance` / `ScreenFader.Instance` 가 플레이 중 백그라운드 재컴파일 → 도메인 리로드 시 null 됨. `Awake`에서만 대입하고 `[RuntimeInitializeOnLoadMethod]` 정적 리셋이 없음 (`ActionPoints`엔 `ResetStatics`로 있음). 증상:
- `PhaseLabel`이 `day=1`로 폴백 → "1일차" 오표시
- `DayEndTitle.Handle`은 null 가드 없어 NRE 가능 (기존 코드도 동일했음, 회귀 아님)
- `PhaseCondition.IsMet`도 `Instance==null`이면 fail-open (doc/0154 참조)

→ `DayPhaseManager`/`ScreenFader`에 `ActionPoints` 패턴(정적 리셋 + Awake 대입) 추가하면 근본 해결.
