# NightNewsBriefing

`Assets/My/Scripts/Game/NightNewsBriefing.cs`

일차 종료 연출 (SYS-11). 새벽에 침대 상호작용 → 페이드 아웃 → 암전 중 플레이어를 `briefingAnchor` 로 **순간이동**(화면고정 + 상호작용 차단) → 왼쪽 대화창에 그날 뉴스 나레이션(선택지 없음) + (있으면) 오른쪽 인게임 TV 슬라이드 → 다 보면 아침으로 전환. 설계·구현 이력 [`doc/0145`](../doc/0145-nightly-tv-news-briefing.md).

## 트리거

`bed_03_Interior`(Owner's_Motel_Room 안) 의 [`NewsBriefingEffect`](InteractionEffect.md) — `Current==Dawn` && `!NightNewsBriefing.Playing` 이면 `briefing.Play(() => DayPhaseManager.TransitionTo(Morning))`. 브리핑 미할당이거나 오늘 뉴스 콘텐츠 없으면 `TransitionTo(Morning)` 직행 (기존 `PhaseSwitchEffect` 동작). `PhaseCondition`(Dawn) 이 상호작용 자체를 게이팅.

## 필드

| 필드 | 설명 |
|---|---|
| `campaign` (`CampaignData`) | 뉴스 문구·슬라이드를 오늘 일차(`DayPhaseManager.DayCount`)로 조회 |
| `briefingAnchor` (`Transform`) | 플레이어 순간이동 위치/정면. 카메라가 왼쪽=대화창 / 오른쪽=TV 가 되게 배치 |
| `newsPanel` ([`SpeechBubble`](DialogueSystem.md)) | 왼쪽 중앙 스크린 대화 패널 (billboard off). 문틈 대화용 `dawnPanel` 과 별개 |
| `tv` (`GameObject`, 선택) | 브리핑 동안만 켜짐. 시작 비활성 권장 |
| `tvImage` (`Image`, 선택) | TV 화면 — 슬라이드 스프라이트 표시 |

## API

- `static bool Playing` — 브리핑 진행 중. `DayPhaseManager.Update` 디버그 키(N/Q) 가드 + 재상호작용 차단.
- `bool Play()` — 오늘 `DayPlan` 에 `newsLinesEn` 이 비어 있으면 **`false`** 반환(호출측이 바로 전환). 아니면 시퀀스 시작하고 `true`. 끝나면 **스스로** 아침으로 전환 (콜백 없음).

## 시퀀스

1. **여는 `Fade()`** — atBlack: `UIInteractionMode.FreezeForOverlay(true, briefingAnchor)` (CC 끄고 즉시 순간이동, 전환 애니 없음) + `tv` 켜기 + 슬라이드 0. → 아침이 아니라 브리핑 화면으로 밝아짐.
2. 줄마다 — TV 슬라이드 `slides[min(i, count-1)]` 로 교체 → `newsPanel.ShowLine(lines[i])` (타이핑 + 클릭/E/Space 대기). 선택지·허브 없음.
3. `newsPanel.Hide()`.
4. **닫는 `Fade()`** — atBlack: `tv` 끄기 + `FreezeForOverlay(false)`(**암전 중** 플레이어 원위치 순간이동 + 조작 복구) + `DayPhaseManager.TransitionTo(Morning, false)`(페이드 없이 `DayCount++`·조명 스왑). → 아침으로 밝아짐.
5. `Playing = false`.

`Fade(atBlack)` — `ScreenFader.FadeThrough` 래퍼. 소프트락 방지: 앞선 페이드 최대 2초 대기 → `FadeThrough` → `done` 최대 3초 대기 → 안 오면 `atBlack()` 직접 (페이더가 도메인 리로드 등으로 멈춰도 상태는 넘어감).

## 뉴스 콘텐츠 — [`CampaignData`](DialogueSystem.md) `DayPlan`

| 필드 | 설명 |
|---|---|
| `newsLinesEn` / `newsLinesKo` (`List<string>`) | 대화창 나레이션. 한 항목 = 한 줄. Ko 비면 En 폴백 (`LocalizationManager.Korean`) |
| `newsSlides` (`List<Sprite>`) | 오른쪽 TV 슬라이드. 줄 i → 슬라이드 i (모자라면 마지막 유지). 텍스트 없는 이미지 권장(문구는 나레이션이 담당) |

`newsLinesEn` 이 비면 그 날은 브리핑 스킵.

## 관련

[DayPhaseManager](DayPhaseManager.md) · [ScreenFader](ScreenFader.md) · [UIInteractionMode](UIInteractionMode.md) · [DialogueSystem](DialogueSystem.md) · [`doc/0145`](../doc/0145-nightly-tv-news-briefing.md)
