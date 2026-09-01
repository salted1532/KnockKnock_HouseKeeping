# 0143 - README / Docs 갱신 (체크아웃·하우스키핑·돈·대화·아키텍처)

날짜: 2026-09-02
관련: `doc/0132`(체크아웃/하우스키핑), `doc/0135`~`0138`(대화·선택지), `doc/0137`·`0140`(돈), `doc/0139`(아키텍처 문서), `doc/0141`(모니터 외곽선), `doc/0142`(발소리)

## 요청 (원문)

> 현재 진행된 사항들을 Readme파일에다가 갱신해줘 새로 추가된 스크립트는 문서로 정리해서 연결해주고
> 기획에서 아까만큼 코드 아키텍처도 보이도록해줘
> (오늘 추가: ✓체크아웃 ✓하우스키핑 ✓무한 일차 ✓돈 시스템 ✓모니터 UI 호버 외곽선 버그)

## 배경

README 는 `f95d79fa` 이후 안 갱신 — 대화 시스템·접객 흐름·모니터 방배정·새벽 노크·체크아웃·
하우스키핑·돈·로컬라이제이션이 통째로 빠져 있었음. `README.md` 를 현재 상태로 맞추고
새 스크립트를 `Docs/` 에 연결.

## 변경

### `Docs/` 신규

| 파일 | 내용 |
|---|---|
| `GuestManager.md` | `GuestState`(stayNights·cleaningRequested·nightlyRate·payUpfront·settled·CheckOutDay·TotalCharge), API, 체크아웃/방배정 |
| `ScreenMessage.md` | 화면 중앙 관찰 문구 싱글턴 (`doc/0121`), KnockEffect 가 씀 |

### `Docs/` 갱신

- `Overview.md` — 상호작용 표에 `CheckInGuestEffect`·`InteractableProxyClick`·`AwaitingCheckInCondition`·`Interactor` 추가, 대화 표에 `GuestManager`·`Wallet` 링크, `ScreenMessage` 링크
- `DialogueSystem.md` — `GuestManager` 섹션을 새 필드로 갱신 + `GuestManager.md` 링크, `ReceptionManager` 손님 큐를 무한 셔플·모니터 방배정·숙박비 확정 반영

### `README.md`

- **핵심 루프** — 4단계를 실제 구현대로 재작성 (아침 체크아웃/하우스키핑, 저녁 대화+모니터 방배정, 새벽 노크 탐문, 돈)
- **프로젝트 구조** — `Scripts/` 에 `Dialogue/`·`UI/`·`Localization/` 추가, `Game/` 에 GuestManager·Wallet
- **코드 아키텍처** (신규 섹션) — 최상위 mermaid 요약 + `기획/코드-아키텍처.md` 링크
- **핵심 스크립트** 표 — 대화·접객·객실·돈·로컬라이제이션 13행 추가
- **구현 완료 기능** — `대화/접객` · `객실 생애주기/체크아웃/하우스키핑` · `돈` · `로컬라이제이션` 섹션 추가, `UI 모드` 갱신
- **로드맵** — 완료 항목(접객 골격) 제거, 판별 로직·태스크 게이트·점심 일과 등으로 정리
- **수정 필요** 표 — `ReceptionManager` "디버그 K 뿐" → "testShuffle 기본 on" 로 갱신
- 행동 카테고리 표에 `노크`(KnockEffect) 행 + 특수 효과 각주

## 검증

- README mermaid: `npx @mermaid-js/mermaid-cli@11` → 1/1 렌더 OK
- `기획/코드-아키텍처.md` mermaid 11/11 OK (변경 없음)

## 상태

2026-09-02 완료. 코드 변경 없음 (문서만).
