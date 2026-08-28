# 0100 - 접객 모드 상호작용(노트/모니터) + 하루 시간대 전환 (설계)

여러 차례 대화로 확정. 최종본은 이 문서 기준.

## 요청 (누적)
1. **노트** → 상호작용 시 Canvas UI 이미지(혹은 클릭 요소)가 화면에 표시.
2. **모니터(컴퓨터)** → 상호작용 시 접객처럼 앵커로 이동 + 화면 고정 + 모니터 UI 클릭 (줌인).
3. 상호작용 프롬프트 `접객` → **`화면고정`** 으로 이름 변경. 접객 세션 진입은 별도 코드로 분리.
4. **시간대 전환**: 바뀔 때마다 페이드 아웃 → 조명/설정 스왑 → 페이드 인 (Day/Night 방식 + 페이드).
5. 시간대를 넘기는 **부착형 스크립트**(`InteractionEffect`) — 해당 오브젝트에 컴포넌트로 붙이면 상호작용 시 다음 단계로. `직접입력` 프롬프트는 쓰지 않음.
6. 하루 흐름:
   ```
   아침 (방청소)      ── 할일 완료 → [게시판] 상호작용 ──▶ 점심
   점심 (하루일과)    ── 할일 완료 → [접객 테이블] 상호작용 ──▶ 저녁
   저녁 (숙박객 모집) ── 모집 종료 → 자동 페이드 ──▶ 새벽
   새벽 (자유활동/탐문) ── 행동력 소진 → [침대] 상호작용 ──▶ 다음날 아침 (DayCount++)
   ```
   기획서 대응: SYS-12(시간대 전환), SYS-02(아침/점심 태스크), SYS-03~06(저녁 접객), SYS-10/11(새벽/취침).

---

## 1. 현재 코드 상태

| 조각 | 상태 |
|---|---|
| `Game/DayPhaseManager.cs` | enum `Morning/Noon/Evening/Dawn`, `Advance()`(N키 디버그), `OnPhaseChanged`. **비주얼 구독자 없음** |
| `Environment/DayNightSwitcher.cs` | Q키로 night/morning 2상태, 스카이박스·라이트·볼륨·fog 즉시 스왑. **`DayPhaseManager` 와 미연결** |
| `Game/ReceptionManager.cs` | `UIInteractionMode.Entered/Exited` 구독, "저녁이면 접객 세션" **heuristic** (모니터가 오검출시킴) |
| `Interaction/Modes/UIInteractionMode.cs` | `Enter(anchor)`: 앵커 이동 + 화면 고정 + 커서 + Gaze↔Cursor 전환, ESC 종료. `Entered/Exited` 이벤트 |
| `Interaction/Effects/EnterUIModeEffect.cs` | `anchor` → `UIInteractionMode.Enter(anchor)` |
| `Interaction/Interactable.cs` | 프롬프트 enum + `SyncEffectsToPrompt` (우클릭 재설정), `ManagedEffects` |
| 페이드 연출 | 없음 |

---

## 2. 신규 / 수정 컴포넌트

### 신규 `Environment/ScreenFader.cs` (싱글턴)
- Overlay 캔버스에 풀스크린 검정 `Image` + `CanvasGroup`(alpha 0). 페이드 중 `blocksRaycasts=true`.
- `void FadeThrough(Action atBlack, Action done = null)` — alpha 0→1(`outDur` 0.4s) → `atBlack()` → `hold`(0.1s) → 1→0(`inDur` 0.6s) → `done()`. 시간은 인스펙터.
- 재진입 안전(코루틴 1개 유지). 싱글턴 없으면 호출측이 `atBlack`/`done` 즉시 실행(null-safe).

### 수정 `Game/DayPhaseManager.cs`
- `Advance()` 가 `ScreenFader.Instance?.FadeThrough(...)` 를 거침:
  - **암전 시점**(`atBlack`): `Current` 갱신, `Dawn→Morning` 이면 `DayCount++`, `OnPhaseChanged(Current)` 발동.
  - **페이드 인 완료**(`done`): 신규 `OnPhaseChangeFinished(Current)` 발동.
- `OnPhaseChanged` = 게이트·비주얼용 (암전 중, 기존 유지). `OnPhaseChangeFinished` = 후속 연출용 (신규).
- ScreenFader 없으면 즉시 전환 (현재 동작 + N키 디버그 유지).

### 신규 `Environment/PhaseVisuals.cs` (구 `DayNightSwitcher` 대체)
- `OnPhaseChanged` 구독 → 해당 단계 비주얼 즉시 적용 (암전 중이라 안 보임).
- 인스펙터: `[Serializable] struct PhaseLook { Material skybox; GameObject lightRoot; VolumeProfile volume; bool fog; }` 를 4단계 배열로. 아침/점심이 같은 값 가리켜도 됨.
- `DayNightSwitcher` 삭제, 씬에서 필드 재배선 필요. Q 디버그 토글은 제거(N키 Advance 로 통합).

### 신규 `Interaction/Effects/PhaseSwitchEffect.cs` (요청한 "부착형 스위치")
- `[RequireComponent(typeof(Interactable))]` 상속. `Play()` → `DayPhaseManager.Instance.Advance()`.
- 필드 없음. 현재 단계 제한은 같은 오브젝트의 `PhaseCondition` 담당.
- **managed 효과 아님** (자동 제거 안 됨) — 아무 `Interactable` 에 붙이는 부착형.

### 수정 `Game/ReceptionManager.cs` — 저녁 접객을 스스로 구동
- heuristic 삭제. `[SerializeField] Transform receptionAnchor` (접객 자리) 추가.
- `OnPhaseChanged` 구독: `phase == Evening` → `UIInteractionMode.Instance.Enter(receptionAnchor)` + `InSession = true` + `OnSessionStarted`. (암전 중 진입 → 앵커 이동이 페이드에 가려짐)
- `UIInteractionMode.Exited` 구독: `InSession` 이면 → `InSession = false` + `OnSessionEnded` + `DayPhaseManager.Instance.Advance()` (→ 새벽, 페이드).
  - `ponytail:` 임시 트리거 = ESC 로 접객 UI 모드 탈출 = 영업 종료. 손님 큐 시스템(SYS-03~06) 나오면 "마지막 손님 처리 완료" 또는 "영업 종료" 버튼으로 교체.
- 모니터의 `EnterUIModeEffect` 는 `ReceptionManager` 를 전혀 안 건드림 → 오검출 원천 차단. (별도 `ReceptionSessionEffect` 불필요, 안 만듦)

### 신규 `Interaction/Effects/ShowPanelEffect.cs` — 노트
- `[SerializeField] GameObject panel;` `[SerializeField] bool closeOnEsc = true;` `[SerializeField] bool pausePlayer = false;`
- `Play()`: `panel` 활성 토글. 켜졌고 `closeOnEsc` 면 `Update` 에서 ESC/재상호작용 → 끔.
- `pausePlayer = true`: `GazeInteractor.Suspended = true` + `Cursor` 표시 + FPC 정지 (패널에 클릭 요소 있을 때). 끌 때 복원.
  - 참조는 `UIInteractionMode.Instance` 의 것을 재사용하거나 `FindObjectOfType` 1회.
- 클릭 콘텐츠가 많은 노트는 `읽기` 대신 `화면고정`(모니터 방식)을 쓴다.

### 수정 `Interaction/Interactable.cs`
- enum: `접객` → **`화면고정`** (index 8 제자리 rename, 재직렬화 불필요). 끝에 `읽기`, `취침` 추가.
- `SyncEffectsToPrompt` switch:
  - `화면고정` → `EnterUIModeEffect` + `SfxEffect` (기존 `접객` 의 `PhaseCondition` 자동추가 **삭제** — 모니터는 시간대 제한 없음)
  - `읽기` → `ShowPanelEffect` + `SfxEffect`
  - `취침` → `SfxEffect` (PhaseSwitchEffect·PhaseCondition 은 수동)
- `ManagedEffects` 에 `ShowPanelEffect` 추가. `PhaseSwitchEffect` 는 넣지 않음(부착형).
- `Prompt`: `취침` → "취침", `읽기` → "읽기", `화면고정` → "화면고정".

### `EnterUIModeEffect.cs` — 변경 없음
모니터·기타 화면고정 진입용으로 유지. `reception` 플래그 안 넣음(2번 분리).

---

## 3. 조립 레시피

| 대상 | Interactable | 효과 / 조건 |
|---|---|---|
| **게시판** (아침→점심) | 조사 | `PhaseCondition(Morning)` + `PhaseSwitchEffect` + `SfxEffect` |
| **접객 테이블** (점심→저녁) | 조사 | `PhaseCondition(Noon)` + `PhaseSwitchEffect` + `SfxEffect`. 저녁 진입 후 접객 모드는 `ReceptionManager` 가 자동 |
| **침대** (새벽→아침) | 취침 | `PhaseCondition(Dawn)` + `PhaseSwitchEffect` + `SfxEffect` |
| **모니터** | 화면고정 | `EnterUIModeEffect`(anchor=모니터 `Player_Anchor`) + `SfxEffect`. `PhaseCondition` 없음. 화면 버튼 = Quad+BoxCollider+Interaction 레이어+`Interactable` |
| **노트** | 읽기 | `ShowPanelEffect`(panel=Overlay 캔버스 자식, 시작 비활성) + `SfxEffect` |

- `PhaseSwitchEffect` + `PhaseCondition` 은 재설정 대상 아님 → 수동 추가(부착형). `PhaseCondition.allowedPhases` 를 해당 단계로.
- 저녁 자동 진입: `ReceptionManager.receptionAnchor` = 접객 테이블의 `Player_Anchor` 연결.

---

## 4. 확인 필요

1. **`ScreenFader`**: Overlay 검정 Image + CanvasGroup 신규 생성. 페이드 out 0.4 / hold 0.1 / in 0.6 기본 OK?
2. **`DayNightSwitcher` → `PhaseVisuals` 개편** (삭제 + 씬 재배선). Q 디버그 토글 제거. OK?
3. **저녁 접객 종료 트리거**: 지금은 "ESC 로 접객 UI 탈출 = 영업 종료 → 새벽". 손님 큐 나오면 교체. OK? 아니면 "영업 종료" 버튼(씬 오브젝트) 지금 만들지.
4. **저녁 접객 진입 방식**: `ReceptionManager` 가 `Evening` 되면 **자동으로** 접객 자리로 이동 + 세션 시작. (테이블 재클릭 없음) OK?
5. **할일 완료 게이트 (SYS-02)**: 태스크 시스템 미존재 → 이번엔 게시판/테이블이 조건 없이 전환. 나중에 `TasksCompleteCondition : InteractionCondition` 훅. 스킵 확인.
6. **새벽 행동력**: 침대는 이번엔 조건 없이 전환. 행동력 시스템은 나중. 스킵 확인.
7. **`취침` enum 추가** vs 침대도 `조사`/`상호작용` 재사용. (추가 추천)
8. **노트 `ShowPanelEffect.pausePlayer` 기본값**: false(이미지만) / true(정지+커서).

---

## 5. 파일 목록

| 파일 | 종류 |
|---|---|
| `Environment/ScreenFader.cs` | 신규 |
| `Environment/PhaseVisuals.cs` | 신규 |
| `Environment/DayNightSwitcher.cs` | 삭제 |
| `Game/DayPhaseManager.cs` | 수정 (페이드 경유, `OnPhaseChangeFinished`) |
| `Game/ReceptionManager.cs` | 수정 (heuristic 삭제, `Evening` 자동 진입, 종료 시 `Advance`) |
| `Interaction/Effects/PhaseSwitchEffect.cs` | 신규 |
| `Interaction/Effects/ShowPanelEffect.cs` | 신규 |
| `Interaction/Interactable.cs` | 수정 (enum rename + `읽기`/`취침`, switch 3케이스, ManagedEffects) |
| `doc/0079-interaction-effects-reference.md` | 수정 (새 효과·프롬프트·레시피·하루 흐름) |

## 6. 사용자 씬 작업 (구현 후)
1. 컴파일 확인.
2. `ScreenFader`: Overlay 캔버스에 풀스크린 검정 Image (게임 화면 RawImage 위) + `CanvasGroup` + `ScreenFader` 컴포넌트.
3. `PhaseVisuals`: 컴포넌트 추가, 4단계 `PhaseLook` (skybox/lightRoot/volume/fog) 채우기. 구 `DayNightSwitcher` 제거.
4. 게시판/접객 테이블/침대: `Interactable`(조사/조사/취침) → 재설정 → `SfxEffect`. 수동 추가: `PhaseCondition`(Morning/Noon/Dawn) + `PhaseSwitchEffect`.
5. 접객 테이블에 `Player_Anchor` 자식 배치 → `ReceptionManager.receptionAnchor` 연결.
6. 모니터: `Player_Anchor` 배치, `Interactable`(화면고정) → 재설정, `EnterUIModeEffect.anchor` 연결. 화면 버튼 = Quad+BoxCollider+Interaction 레이어+`Interactable`.
7. 노트: `Interactable`(읽기) → 재설정, `ShowPanelEffect.panel` = Overlay 자식 패널(시작 비활성).
8. 검증: N키로 각 단계 → 페이드+조명 스왑. 게시판/테이블/침대 상호작용 → 페이드+전환. 저녁 되면 접객 자리 자동 이동, ESC → 새벽. 모니터 E → 줌인, 화면 버튼 클릭, ESC 복귀. 노트 E → 패널.

---

## 7. 확정 (2026-08-28, 확인 1~8 답변 반영) — 이 섹션이 최종

**1. 페이드 시간** — out 0.4 / hold 0.1 / in 0.6 확정.

**2. `PhaseVisuals` 개편 + 디버그 Q키 유지** — 단 night/day 토글이 아니라 `DayPhaseManager.Advance()`
호출 (다음 시간대, 페이드 작동). `DayPhaseManager.debugAdvanceKey` 가 **N + Q 둘 다** 수신.
Q → 아침→점심→저녁→새벽→아침 순환(페이드 인/아웃). Q 로 저녁 진입해도 접객 모드 자동 진입되어 흐름 전체 테스트 가능.

**3. 저녁 종료 = "그날 숙박객 전원 처리 시 자동".** 손님 시스템(SYS-03~06/09) 미존재 →
- `ReceptionManager.EndSession()` 를 실제 API 로 둠. 손님 큐가 나중에 호출.
- **임시 트리거**: 디버그 키 `K` → `EndSession()`. `ponytail:` 주석으로 교체 지점 표시.
- 세션 중 `ESC` 는 하루를 안 넘김 — `UIInteractionMode` 에 `suppressEscExit` 플래그, `ReceptionManager` 가 세션 동안 set.

**4. 접객 테이블(점심) 상호작용 → 저녁 전환 + 접객 모드 즉시 진입.**
- 테이블: `점심종료` + `PhaseCondition(Noon)` + `PhaseSwitchEffect` + `SfxEffect`.
- `ReceptionManager` 가 `OnPhaseChanged`(암전 시점) 에서 `phase==Evening` → `UIInteractionMode.Enter(receptionAnchor)`
  + `InSession=true` + `OnSessionStarted`. 앵커 이동이 페이드에 가려짐 → 페이드 인 시 이미 착석.
- 저녁 도달 경로가 테이블뿐이라 "테이블 클릭 → 접객" 성립.

**5. 할일 완료 게이트** — 스킵 확정 (아침/점심 미구현). 나중에 `TasksCompleteCondition` 훅.

**6. 새벽 행동력 게이트** — 스킵 확정.

**7. `취침` 폐기 → 하루종료 스위치 enum 4종 추가.**
`아침종료`(→점심), `점심종료`(→저녁), `저녁종료`(→새벽), `하루종료`(→다음날 아침, `DayCount++`).
- `SyncEffectsToPrompt`(우클릭 재설정): 각 값 → `PhaseSwitchEffect` + `SfxEffect` 자동 추가
  + `PhaseCondition` 자동 추가 & `allowedPhases` 를 해당 단계로 **자동 설정**.
- `PhaseSwitchEffect` = **managed 효과**로 등록 (이 4개에서 자동 추가/제거).
  동작은 `DayPhaseManager.Advance()` 하나 — 어느 전환인지는 `PhaseCondition` 이 결정,
  프롬프트 값은 표시 문구 + 재설정 자동화용 (doc 0079 "promptType 자체는 기능 없음" 원칙 유지).
- 문구: `아침종료`/`점심종료` → "일과 종료", `저녁종료` → "영업 종료", `하루종료` → "취침".
- **"1가지 상호작용만" 한계**: 한 오브젝트가 시간대별로 다른 상호작용을 하려면 지금은 자식 오브젝트
  분리(기존 패턴). 이번 흐름은 트리거가 전부 별개 오브젝트(게시판/테이블/침대)라 각 1개면 충분.
  다중 컨텍스트 상호작용 시스템 개편은 실제 필요 케이스 나올 때 별도 doc 로. (여기선 안 함)

**8. 노트** — `ShowPanelEffect` 는 **항상** 플레이어 퍼즈 (이동/시야 정지 + 커서 표시). `pausePlayer` 토글 제거. `읽기` 프롬프트 전부 적용.

**9. 안전장치** — `DayPhaseManager` 에 `transitioning` 가드 (페이드 중 재-Advance 무시).

### 최종 조립 레시피

| 대상 | promptType | 재설정 자동 구성 | 수동 |
|---|---|---|---|
| 게시판 (아침→점심) | `아침종료` | `PhaseSwitchEffect`+`SfxEffect`+`PhaseCondition(Morning)` | — |
| 접객 테이블 (점심→저녁) | `점심종료` | `PhaseSwitchEffect`+`SfxEffect`+`PhaseCondition(Noon)` | — |
| 침대 (새벽→아침) | `하루종료` | `PhaseSwitchEffect`+`SfxEffect`+`PhaseCondition(Dawn)` | — |
| 모니터 | `화면고정` | `EnterUIModeEffect`+`SfxEffect` | anchor. 화면버튼=Quad+BoxCollider+Interaction 레이어+`Interactable` |
| 노트 | `읽기` | `ShowPanelEffect`+`SfxEffect` | panel (Overlay 자식, 시작 비활성) |

`저녁종료` 는 이번엔 오브젝트 미배치 (자동 종료 / 디버그 K). 나중에 접객 뷰의 "영업 종료" 클릭 오브젝트로.

### 최종 파일 목록

| 파일 | 종류 | 요지 |
|---|---|---|
| `Environment/ScreenFader.cs` | 신규 | 검정 Image+CanvasGroup, `FadeThrough(atBlack, done)` |
| `Environment/PhaseVisuals.cs` | 신규 | `OnPhaseChanged` 구독, 4단계 `PhaseLook` 배열 스왑 |
| `Environment/DayNightSwitcher.cs` | 삭제 | |
| `Game/DayPhaseManager.cs` | 수정 | `Advance` 페이드 경유 + `transitioning` 가드, `OnPhaseChangeFinished`, 디버그 N+Q |
| `Game/ReceptionManager.cs` | 수정 | heuristic 삭제, `receptionAnchor`, `OnPhaseChanged→Evening` 자동 진입, `EndSession()`(디버그 K), 세션 중 ESC 억제 |
| `Interaction/Modes/UIInteractionMode.cs` | 수정 | `suppressEscExit` 플래그 |
| `Interaction/Effects/PhaseSwitchEffect.cs` | 신규 | `Play()` → `DayPhaseManager.Advance()` |
| `Interaction/Effects/ShowPanelEffect.cs` | 신규 | panel 토글 + 항상 퍼즈, ESC/재상호작용 닫기 |
| `Interaction/Interactable.cs` | 수정 | enum `접객`→`화면고정`, +`읽기`/`아침종료`/`점심종료`/`저녁종료`/`하루종료`; switch 케이스; `ManagedEffects` += `ShowPanelEffect`,`PhaseSwitchEffect`; `PhaseCondition.allowedPhases` 자동설정 |
| `doc/0079-interaction-effects-reference.md` | 수정 | 신규 효과·프롬프트·레시피·하루 흐름 |

---

## 8. 구현 완료 (코드, 2026-08-28)

| 파일 | 내용 |
|---|---|
| `Environment/ScreenFader.cs` | 신규. `FadeThrough(atBlack, done)` 코루틴 (out 0.4 / hold 0.1 / in 0.6, 인스펙터). 진행 중 재호출 무시. `blocksRaycasts` 로 페이드 중 클릭 차단 |
| `Environment/PhaseVisuals.cs` | 신규. `PhaseLook[4]{skybox,lightRoot,volume,fog}`. `Start` 에서 `OnPhaseChanged` 구독 + 즉시 적용. 모든 lightRoot 끄고 현재 것만 켬 |
| `Environment/DayNightSwitcher.cs` | **삭제** (.cs + .meta) |
| `Game/DayPhaseManager.cs` | `TransitionTo(target)` (신규, `ScreenFader` 경유) + `Advance()` = 다음 순환. `AtBlack`: `target==Morning` 이면 `DayCount++`, `Current` 갱신, `OnPhaseChanged`. `Done`: `OnPhaseChangeFinished`. `Transitioning` 가드 + `target==Current` 무시. 디버그 키 N **+ Q** |
| `Game/ReceptionManager.cs` | 재작성. `receptionAnchor` 필드. `OnPhaseChanged`→`Evening` 시 `BeginSession`(Enter + `OnSessionStarted`). `EndSession()` public (디버그 **K**) → `ExitAll()` + `OnSessionEnded` + `Advance()`(→새벽). `UIInteractionMode.Exited` 구독 → ESC 로 접객 레벨까지 빠져나오면 `HandleUIExit` 가 세션만 정리 (하루 전환 없음) |
| `Interaction/Modes/UIInteractionMode.cs` | **앵커 스택** — `Enter` 가 이미 Active 면 그 위에 쌓음(접객→모니터), `Exit` 는 한 단계 pop(하위 남으면 복귀, 비면 완전 종료 `Teardown` + `Exited` 이벤트), `ExitAll` 은 전부 닫음. `Depth` 프로퍼티. **ESC = 항상 한 겹 벗김** (노트 열렸으면 노트가 먼저 소비). `SuppressEscExit` 제거. `FreezeForOverlay(bool)` — `Active` 면 무시. `crosshair` — 첫 Enter 시 숨김, Teardown 시 복원 |
| `Interaction/Effects/ShowPanelEffect.cs` (추가) | `[RuntimeInitializeOnLoadMethod]` 로 `openCount`/`lastCloseFrame` static 초기화 — Domain Reload 꺼도 ESC 영구 차단 안 되게 |
| `Interaction/Drivers/CursorInteractor.cs` | GazeInteractor 와 동일한 가림 체크 추가 — 대상 앞에 막는 콜라이더 있으면 무시 (벽 너머 클릭 차단). `interactMask` 를 Interaction 레이어로 설정해야 동작 (기본 Everything 이면 무효) |
| `Interaction/Effects/PhaseSwitchEffect.cs` | 신규. `from`/`to` 명시. `Play()` → 현재==`from` 이면 `DayPhaseManager.TransitionTo(to)`, 아니면 무시. 재설정으로 from/to + `PhaseCondition` 자동 세팅 |
| `Interaction/Effects/ShowPanelEffect.cs` | 신규. `content`(아무 GameObject) 토글, `Awake` 에서 비활성화. Open/Close 가 `UIInteractionMode.FreezeForOverlay` 호출 (없으면 커서만). `Update` ESC 로 Close, `public Close()` 는 UI 닫기 버튼용. `OnDisable` 안전 복구. `static ConsumesEsc` — 열려있거나 이번 프레임 ESC 로 닫혔으면 true |
| ESC 우선순위 | `UIInteractionMode.Update` / `ReceptionManager.Update` 의 ESC 분기가 `!ShowPanelEffect.ConsumesEsc` 체크 → UI/접객 모드에서 노트 열고 ESC 누르면 **노트만** 닫히고, 다음 ESC 에 모드 탈출. Update 실행 순서 무관 (frame-stamp) |
| `Interaction/Interactable.cs` | enum: `접객`→`화면고정`(idx8 유지), 끝에 `읽기`/`아침종료`/`점심종료`/`저녁종료`/`하루종료`. `Prompt` switch (일과 종료/영업 종료/취침). `SyncEffectsToPrompt`: 화면고정→EnterUIMode, 읽기→ShowPanel, 종료4종→PhaseSwitch + `wantPhase` 로 `PhaseCondition.allowedPhases` SerializedObject 자동 설정. `ManagedEffects` += ShowPanel, PhaseSwitch |
| `Audio/SoundManager.cs` | Q 토글 삭제. `Start` 에서 `OnPhaseChanged` 구독 → 저녁·새벽=nightClip / 아침·점심=morningClip. `using InputSystem` 제거 |
| `Game/PhaseLabel.cs` | 신규. TMP 텍스트에 현재 시간대 표시 ("Day 1 · Morning"). `OnPhaseChanged` 구독. Canvas 의 "Watch" 텍스트에 붙임 |
| `Game/ActivateOnAwake.cs` | 신규. 시작 시 지정 오브젝트 `SetActive(true)`. 에디터에선 꺼둔 FadeOverlay 를 런타임에 켜는 용도. 항상 활성인 부모(Canvas)에 붙임 |
| `doc/0079` | "2026-08-28 갱신" 섹션 추가 |

### 알려진 한계 / 후속
- **저녁 종료 트리거 = 디버그 K** (`ponytail:` 주석). 손님 큐(SYS-03~06/09) 나오면 "전원 처리 완료" 가 `ReceptionManager.EndSession()` 호출하도록.
- **ESC 계층**: 노트 열림 → 노트만 / 모니터 뷰(Depth 2) → 모니터만 닫고 접객 복귀 / 접객 레벨(Depth 1) → 완전 종료 + `ReceptionManager.HandleUIExit` 가 세션 정리 (하루 전환 없음 — 테스트용).
- **K** = 정상 종료 (→ 새벽 페이드). 손님 큐 나오면 "전원 처리 완료" 가 `EndSession()` 호출.
- **접객 중 모니터**: 접객 모드에서 모니터 `화면고정` 클릭 → 앵커 스택에 쌓여 모니터 뷰로. `EnterUIModeEffect.anchor` 는 접객 앵커와 **다른** 오브젝트여야 함 (같으면 재진입으로 무시됨).
- **할일 완료 게이트 없음** — 게시판/테이블/침대가 조건 없이 전환. SYS-02 태스크 시스템 후 `TasksCompleteCondition : InteractionCondition` 추가.
- **`edgeLook` 는 `UIInteractionMode` 전역** — 접객·모니터 모두 완전 고정(false)이라 지금은 무관. 접객만 둘러보기 켜려면 `Enter(...)` 파라미터화 필요.
- **한 오브젝트 다중 컨텍스트 상호작용** 미지원 — 필요 시 자식 오브젝트 분리 or 별도 doc.

### 사용자 씬 작업 (필수)
1. 컴파일 확인.
2. **씬의 구 `DayNightSwitcher` 오브젝트** → 스크립트 빠진 상태. `PhaseVisuals` 붙이고 `globalVolume` + `looks[4]`(Morning/Noon/Evening/Dawn 각 skybox/lightRoot/volume/fog) 채우기.
3. **`ScreenFader`**: 게임 화면 RawImage 위에 풀스크린 검정 `Image` + `CanvasGroup` + `ScreenFader`. (Overlay 캔버스)
4. **`DayPhaseManager`** 오브젝트 확인 (없으면 생성). `ReceptionManager` 에 `receptionAnchor` = 접객 테이블의 `Player_Anchor` 연결.
5. **게시판/접객 테이블/침대**: `Interactable` promptType = `아침종료`/`점심종료`/`하루종료` → 우클릭 "Prompt Type에 맞게 효과 재설정". (PhaseCondition 단계까지 자동)
6. **모니터**: `Interactable`(화면고정) → 재설정. `EnterUIModeEffect.anchor` = 모니터 `Player_Anchor`. 화면 버튼 = Quad+BoxCollider+Interaction 레이어+`Interactable`.
7. **노트**: `Interactable`(읽기) → 재설정. `ShowPanelEffect.content` = 보여줄 오브젝트(UI 이미지/패널/별도 Canvas/3D 등, 시작 비활성 — Awake 가 자동으로 끔).
8. `SoundManager` 의 `nightClip`/`morningClip` 유지 (Q 인풋만 없어짐).

### 검증
- N 또는 Q → 아침→점심→저녁→새벽 순환, 매번 페이드 아웃/인 + 조명·앰비언스 스왑.
- 게시판(아침)/테이블(점심)/침대(새벽) 상호작용 → 해당 전환 페이드. 다른 단계에선 프롬프트 안 뜸.
- 점심에 테이블 E → 페이드 → 저녁 되며 접객 자리로 자동 착석 (페이드에 가려짐), 커서 표시.
- 접객 중 ESC 무시됨. 디버그 K → 페이드 → 새벽, 원위치 복귀.
- 모니터 E → 줌인, 화면 버튼 클릭, ESC 복귀.
- 노트 E → 패널 + 플레이어 정지 + 커서. ESC → 닫힘, 조작 복원.

## 상태
2026-08-28 구현 완료 (코드). 에디터 배선/검증 대기.
