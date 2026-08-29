# 넉넉 하우스키핑 (KnockKnock_HouseKeeping)

> 가제: 넉넉 하우스키핑 / 체크아웃 플리즈
> 미국 모텔 운영 시뮬레이션 공포 게임

## 게임 소개

원인 모를 전염병 **"과대 몽유병"**이 퍼진 미국을 배경으로 한 1인칭 모텔 운영 심리 공포 게임.

몽유병 환자는 밤마다 좀비나 괴물처럼 다른 사람을 습격한다. 플레이어는 모텔 주인이 되어 숙박객을 받을지 거절할지 결정해야 하며, TV에서 매일 제공되는 "구별법"만으로 몽유병 환자를 가려내야 한다. 다만 이 구별법은 나폴리탄 괴담처럼 실제 질병과 무관하거나 이해할 수 없는 내용으로 구성되어 있어, 믿을지 말지는 전적으로 플레이어의 선택에 달려 있다.

숙박객 중 몽유병 환자가 있으면 밤마다 확률적으로 다른 숙박객이 살해당하고, 몽유병 환자 수가 정상 숙박객 수보다 많아지면 플레이어가 죽어 게임 오버가 된다.

- **몽유병 1 : 숙박객 1** → 숙박객 또는 플레이어 사망
- **몽유병 n : 숙박객 n** → 숙박객 1명 사망

날이 지날수록 의심스러운 숙박객이 늘어나고, 평화롭던 모텔 주변 분위기는 점점 어두워지며 멀리 보이는 도시도 불타오르는 등 세계관이 붕괴되어 간다.

### 기본 정보

| 항목 | 내용 |
|---|---|
| 시점 | FPS (1인칭) |
| 장르 | 시뮬레이션, 공포 |
| 그래픽 스타일 | 로우폴리 + 픽셀아트, 3D/2D 혼합 |
| 참고 작품 | 노 아임 낫 휴먼, 댓츠 낫 마이 네이버, 페이퍼스 플리즈, 디스 워 오브 마인 |

## 핵심 루프 (하루 일과)

**아침 → 점심 → 저녁(접객) → 새벽** 4단계로 하루가 진행된다 (`DayPhaseManager`). 각 단계는 지정 오브젝트를 상호작용해 넘긴다 — 게시판(아침→점심), 접객 테이블(점심→저녁), 침대(새벽→다음날). 전환마다 검정 페이드가 조명/앰비언스 스왑을 가려준다 (`ScreenFader` + `PhaseVisuals`).

1. **아침 — 방 청소**: 퇴실한 객실의 쓰레기 줍기(인벤토리) → 쓰레기통에 버림, 흐트러진 침대 정리
2. **점심 — 일과 처리**: 울타리 수리, 노숙자 내쫓기, 불법주차 신고 등 매일 다른 이벤트. 일차에 따라 점프스케어
3. **저녁 — 숙박객 모집 (접객)**: 저녁이 되면 접객 자리로 **자동 착석** → **UI 모드**(플레이어 고정, 마우스 표시) → 테이블 위 컴퓨터·신분증·모니터를 마우스로 조작하며 몽유병 환자 판별 → 수락(방 배정)/거절. 창문에 점프스케어·이상한 방문자
4. **새벽 — 자유시간**: 자율 이동 복귀. 배정한 방마다 숙박객과 대화(불가한 숙박객·문이 활짝 열리는 방·점프스케어 등). 이상한 점을 느끼면 인벤토리의 총으로 숙박객 처치 가능. 침대 취침 → 다음날

일과 중에는 게임 진행에 따른 분위기 변화를 체감할 수 있다 (예: 초반에 쫓아낸 노숙자가 후반엔 시체로 발견되거나, 불법주차 신고 시 경찰이 응답하지 않는 등).

## 전략 요소

모텔 운영 수입으로 인근 상점에서 아이템을 구매할 수 있다.
- 총기·탄약 (몽유병 환자 처치용)
- 강철 문, CCTV, 울타리 등 방어/경영 보조 장비

단, 게임이 중후반으로 진행되면 상점 이용이 제한된다.

## 엔딩

- **정상 엔딩**: 도시가 봉쇄된 상황에서 최대한 많은 비감염자를 모텔에 수용하고, 모텔을 봉쇄한 채 버티기로 결정
- **배드 엔딩**: 플레이어가 몽유병 환자에게 살해당할 경우, 며칠 후 모텔에는 아무도 남지 않는다는 결말로 종료

## 개발 환경

| 구분 | 내용 |
|---|---|
| 엔진 | Unity `6000.4.8f1` |
| 렌더 파이프라인 | Universal Render Pipeline (URP) 17.4.0 |
| 입력 | Unity Input System 1.19.0 (StarterAssets FirstPersonController 기반) |
| 길찾기 | AI Navigation (NavMesh) 2.0.12 |
| 기타 패키지 | Timeline 1.8.12, Visual Scripting 1.9.11, TextMesh Pro |
| 외곽선 | QuickOutline (로컬 패치됨 — `doc/0076`) |

## 프로젝트 구조

```
Assets/
├─ My/
│  ├─ Scripts/
│  │  ├─ Interaction/   # 상호작용 시스템 — Core(베이스), Effects(효과), Conditions(게이트),
│  │  │                 #   Drivers(입력), Modes(UI모드) + CartGroundAlign, ItemImpactSound
│  │  ├─ Inventory/     # 5슬롯 인벤토리 + 아이템 ID 연결(ItemId/HandItem/HandItemRegistry)
│  │  ├─ Game/          # DayPhaseManager(하루 4단계+페이드), ReceptionManager(저녁 접객),
│  │  │                 #   PhaseLabel(HUD), ActivateOnAwake
│  │  ├─ Environment/   # PhaseVisuals(4단계 라이팅/스카이박스/볼륨), ScreenFader(검정 페이드)
│  │  ├─ Audio/         # SoundManager (앰비언스 + 발소리)
│  │  └─ Player/        # FootstepSystem
│  ├─ InGame/           # 씬에 실제로 쓰는 프리팹/머티리얼/사운드/렌더텍스처
│  │  └─ Prefabs/       #   Item(문/커튼/침대/자판기/쓰레기통/쇼핑카트…), MotelRoom, 시간대별 조명
│  └─ Prefabs, Materials # 정리된 3rd-party 프롭(카테고리별, 중복 제거 완료 — doc/0072)
├─ Scenes/              # InGame(본편), SampleScene·TestScene(프로토타입 잔재)
├─ AssetsFolder/        # 3rd-party 원본 에셋 팩 (임포트 상태, HDRP→URP 변환 등)
├─ Editor/              # AssetOrganizer, InteractionMigrator (1회용 마이그레이션)
└─ TutorialInfo, TextMesh Pro/Examples  # Unity 템플릿/샘플 잔재 (게임에 미사용)

Docs/                    # 스크립트별 레퍼런스 문서 (역할/필드/동작) — 허브: Docs/Overview.md
doc/                     # 세션별 작업 로그 + 코드 변경 전/후 + 설계 노트, 0001~ 번호 통합
기획/                    # 기능정의서, 핵심컨셉 분석, 상호작용·미니게임 연동 설계안
```

> `Docs/`(복수)와 `doc/`(소문자)는 Windows 대소문자 미구분 때문에 이름을 분리한 것. `Docs/` = 스크립트 문서, `doc/` = 세션 로그.

## 상호작용 시스템

전체 개편 완료 ([`doc/0078`](doc/0078-interaction-system-redesign.md), [`doc/0079`](doc/0079-interaction-effects-reference.md), 노트/모니터·하루 흐름 [`doc/0100`](doc/0100-note-and-monitor-interactions-design.md), 허브 문서 [`Docs/InteractionSystem.md`](Docs/InteractionSystem.md)).

**구 방식** (`InteractionType` enum + 거대 switch에 케이스를 하나씩 추가) → **컴포넌트 조합 방식**으로 전환:

```
GameObject
├─ Interactable            ← 플레이어가 찾는 대상 (디스패처)
├─ InteractionEffect …     ← 실제 동작 (여러 개 스택)
└─ InteractionCondition …  ← 상호작용 가능 여부 게이트 (선택)
```

### 작업 흐름

큰 행동 카테고리를 `Interactable` 의 **Prompt Type** 으로 지정 → 컴포넌트 **우클릭 "Prompt Type에 맞게 효과 재설정"** →
해당 카테고리 표준 스크립트 자동 추가/정리(+콜라이더·Interaction 레이어·Outline·컴포넌트 순서) → 각 효과의 오브젝트/클립 필드 수동 연결 → 완성.

행동 카테고리에도 들어가기 힘든 특별한 작동만 새 `InteractionEffect` 서브클래스를 만들어 붙인다. 큰 틀은 건드리지 않는다. 기존 행동에 옵션을 추가하거나 새 행동이 필요하면 카테고리/효과를 추가한다.

### 행동 카테고리 & 효과

| 카테고리 | 붙는 효과 | 구 방식 |
|---|---|---|
| 여닫기 (열기/닫기) | `HingeEffect` + `SfxEffect` | `Door` |
| 켜고끄기 (켜기/끄기) | `ChangeObjectEffect` + `SfxEffect` | `Curtain` |
| 정리하기 | `ChangeObjectEffect` + `SfxEffect` | `TidyBed` |
| 줍기 | `PickupEffect` + `SfxEffect` + `ItemImpactSound` | `Pickup` / `Flashlight` |
| 사용 | `SpawnObjectEffect` + `SfxEffect` | `ItemDispenser` |
| 밀기 | `PushEffect` + `SfxEffect` + `ItemImpactSound` | `Push` |
| 걸기 | `HookEffect` + `SfxEffect` | (신규 — 열쇠고리) |
| 화면고정 | `EnterUIModeEffect` + `SfxEffect` | 구 `접객` (모니터·컴퓨터, 시간대 무관) |
| 읽기 | `ShowPanelEffect` + `SfxEffect` | (신규 — 노트/편지/사진) |
| 아침·점심·저녁·하루 종료 | `PhaseSwitchEffect` + `SfxEffect` + `PhaseCondition` | (신규 — 게시판/접객 테이블/침대) |
| 상호작용 / 조사 | `SfxEffect` | `Generic` + `UnityEvent` |

- **모든 상호작용에 효과음** — `SfxEffect` 는 항상 포함. `[RequireComponent(AudioSource)]` 로 자동 부착.
- **on/off 상호작용은 소리 2개** — 토글이면 `SfxEffect` 가 `onClip` / `offClip` 을 따로 재생.
- **획득 아이템은 ID로 연결** — 줍는 프리팹의 `PickupEffect.itemId` ↔ 플레이어 손 오브젝트의 `HandItem.id` (`HandItemRegistry` 가 매칭). 손전등=001, 소다=002.
- **하루종료 스위치 4종** — 재설정 시 `PhaseSwitchEffect.from/to` 와 `PhaseCondition.allowedPhases` 가 프롬프트에 맞춰 자동 세팅 (아침종료 = Morning→Noon … 하루종료 = Dawn→Morning). 전환은 `ScreenFader` 페이드 경유.

### 핵심 스크립트

각 스크립트 상세(필드·동작)는 [`Docs/`](Docs) 폴더 참조 (허브: [`Docs/Overview.md`](Docs/Overview.md)).

| 스크립트 | 역할 | 문서 |
|---|---|---|
| `Interactable` | 디스패처. promptType/isToggle/onInteracted, 효과 실행, 우클릭 재설정 메뉴 | [doc](Docs/Interactable.md) |
| `InteractionEffect` | 효과 추상 베이스 + `InteractionContext` 구조체 | [doc](Docs/InteractionEffect.md) |
| `InteractionCondition` | 게이트 추상 베이스 (`IsMet`) | [doc](Docs/InteractionCondition.md) |
| `SfxEffect` | 효과음. 토글이면 on/off 2클립, `interrupt` 시 이전 소리 끊고 교체 | [doc](Docs/SfxEffect.md) |
| `ChangeObjectEffect` | `onObjects`/`offObjects` SetActive 스왑 (침대·커튼) | [doc](Docs/ChangeObjectEffect.md) |
| `HingeEffect` | 경첩 회전 여닫기. `hinge` Transform·`axis` 직접 지정 (문·쓰레기통 뚜껑) | [doc](Docs/HingeEffect.md) |
| `PushEffect` | 부모 Rigidbody 를 주체 반대로 임펄스+토크 (쇼핑카트) | [doc](Docs/PushEffect.md) |
| `PickupEffect` | `InventorySystem.AddItem`. `itemId` 로 손 오브젝트 조회 | [doc](Docs/PickupEffect.md) |
| `SpawnObjectEffect` | 프리팹 생성, `maxCount` 제한 (자판기) | [doc](Docs/SpawnObjectEffect.md) |
| `HookEffect` | 손에 든 열쇠를 빈 고리에 걸어 고정 (열쇠고리) | [doc](Docs/HookEffect.md) |
| `EnterUIModeEffect` | `UIInteractionMode.Enter(anchor)` — `화면고정` (모니터/컴퓨터) | [doc](Docs/EnterUIModeEffect.md) |
| `ShowPanelEffect` | 오브젝트 켜고 플레이어 정지 — `읽기` (노트/편지/사진) | [doc](Docs/ShowPanelEffect.md) |
| `PhaseSwitchEffect` | 상호작용으로 하루 단계 `from→to` 전환 (게시판/테이블/침대) | [doc](Docs/PhaseSwitchEffect.md) |
| `PhaseCondition` | 지정 하루 단계에서만 상호작용 허용 | [doc](Docs/PhaseCondition.md) |
| `GazeInteractor` | 화면중앙 레이 + 아웃라인 + E, 벽 너머 차단 (구 `InteractionOutline`) | [doc](Docs/GazeInteractor.md) |
| `CursorInteractor` | 마우스 레이 + 좌클릭, UI 모드 전용, RenderTexture 커서 보정 + 가림 체크 | [doc](Docs/CursorInteractor.md) |
| `UIInteractionMode` | UI 모드 — 앵커 스택(접객/모니터/노트), 플레이어 고정, 커서, ESC 한 겹씩 | [doc](Docs/UIInteractionMode.md) |
| `DayPhaseManager` | 아침/점심/저녁/새벽 순환 + `ScreenFader` 페이드 전환, `OnPhaseChanged` | [doc](Docs/DayPhaseManager.md) |
| `ReceptionManager` | 저녁 접객 세션 골격 — `Evening` 자동 진입, `EndSession()`→새벽 | [doc](Docs/ReceptionManager.md) |
| `PhaseLabel` | HUD 시간대 텍스트 ("Day 3 · Evening") | [doc](Docs/PhaseLabel.md) |
| `ItemId` / `HandItem` / `HandItemRegistry` | 획득 아이템 프리팹 ↔ 손 오브젝트 번호 연결 | [doc](Docs/HandItemRegistry.md) |
| `InventorySystem` | 5슬롯, 줍기/장착/사용/던지기, 손전등 슬롯 특수 | [doc](Docs/InventorySystem.md) |
| `ItemImpactSound` | 물리 충돌 시 임팩트 사운드 (줍기·밀기 자동 추가) | [doc](Docs/ItemImpactSound.md) |
| `CartGroundAlign` | 쇼핑카트 4바퀴 레이캐스트로 바닥 기울기 정렬 (진행 중) | [doc](Docs/CartGroundAlign.md) |
| `SoundManager` | 앰비언스(밤/낮, `OnPhaseChanged` 구독) + 지면 레이어별 발소리 | [doc](Docs/SoundManager.md) |
| `FootstepSystem` | 이동 거리 누적 → 발소리 타이밍, 지면 레이어 판정 | [doc](Docs/FootstepSystem.md) |
| `PhaseVisuals` | 4단계 스카이박스/라이트/볼륨/fog 스왑 (구 `DayNightSwitcher`) | [doc](Docs/PhaseVisuals.md) |
| `ScreenFader` | 전체 화면 검정 페이드 (`FadeThrough`) | [doc](Docs/ScreenFader.md) |
| `ActivateOnAwake` | 시작 시 지정 오브젝트 활성화 (페이드 오버레이 등) | [doc](Docs/ActivateOnAwake.md) |

## 구현 완료 기능

### 플레이어 / 이동
- [x] 1인칭 이동/시야 (StarterAssets FirstPersonController)
- [x] 이동 거리 기반 발소리 + 지면 레이어(Wood/Concrete/Metal/Grass)별 클립 + 스프린트 피치
- [x] 손전등 (인벤토리 손전등 슬롯 특수 처리 — 켜져 있으면 휠 슬롯 전환 잠금)

### 상호작용 시스템 (개편)
- [x] `Interactable` + `InteractionEffect` 컴포넌트 조합 구조 — enum+switch 방식 폐기
- [x] 효과 10종(Sfx/ChangeObject/Hinge/Push/Pickup/SpawnObject/Hook/EnterUIMode/ShowPanel/PhaseSwitch) + 조건 1종 + 입력 드라이버 2종 + UI 모드 매니저
- [x] `GazeInteractor` — 화면중앙 레이, 아웃라인, E키, **벽 너머 상호작용 차단**(가림 2차 레이캐스트, `doc/0077`). `CursorInteractor` 도 동일 가림 체크
- [x] `HookEffect` — 손에 든 열쇠를 빈 고리에 걸기 (`걸기` 프롬프트, `doc/0087`~`0090`)
- [x] `Interactable` 우클릭 **"Prompt Type에 맞게 효과 재설정"** — 카테고리별 표준 효과 추가/제거, 콜라이더·Interaction 레이어·Outline 자동, 컴포넌트 순서 정렬(메쉬→콜라이더→스크립트→사운드→나머지)
- [x] 모든 상호작용 효과음 + 토글 상호작용 on/off 소리 분리
- [x] `HingeEffect` — `hinge` Transform·`axis` 지정으로 문/쓰레기통 뚜껑 등 임의 축 여닫기
- [x] 획득 아이템 ID 연결 — `ItemId` enum + `HandItem` + `HandItemRegistry` (프리팹 ↔ 손 오브젝트)
- [x] 구 프리팹 → 신 구조 1회용 마이그레이션 스크립트 (`Editor/InteractionMigrator.cs`)
- [x] 스왑되는 메쉬(침대/커튼)의 외곽선 유지 — QuickOutline 로컬 패치 (`doc/0076`)

### 인벤토리
- [x] 5슬롯, 아이템 줍기(`PickupEffect`)/슬롯 선택(1~5)/사용(좌클릭)/던지기(F)
- [x] 던질 때 원본 픽업 오브젝트 되살려 Rigidbody 부착 + 플레이어 콜라이더 충돌 무시 + 벽 관통 방지 스피어캐스트

### 하루 진행 / 환경
- [x] `DayPhaseManager` — 아침→점심→저녁→새벽 순환, `ScreenFader` **검정 페이드 전환**(암전 시 상태 갱신 + `OnPhaseChanged`, 페이드 인 후 `OnPhaseChangeFinished`), `Transitioning` 가드, 디버그 `N`/`Q` 키
- [x] `PhaseSwitchEffect` — 게시판(아침→점심)·접객 테이블(점심→저녁)·침대(새벽→아침) 상호작용으로 단계 전환. `from`/`to` + `PhaseCondition` 은 재설정 시 자동
- [x] `PhaseVisuals` — 4단계 스카이박스/라이트 묶음/URP 볼륨/포그 스왑 (`OnPhaseChanged` 구독, 구 `DayNightSwitcher` 대체·삭제)
- [x] `SoundManager` — `OnPhaseChanged` 구독, 저녁·새벽=밤 / 아침·점심=낮 앰비언스 (구 `Q` 토글 삭제), 발소리 재생
- [x] `PhaseLabel` — HUD 시간대/일차 텍스트

### UI 모드 / 접객
- [x] `UIInteractionMode` — **앵커 스택**: 플레이어를 `Player_Anchor` 로 이동·고정, 커서 표시, `Gaze`↔`Cursor` 전환. 접객(하위) 안에서 모니터(상위) 중첩, ESC 로 한 겹씩 벗김
- [x] `ReceptionManager` — 저녁 되면 접객 자리로 **자동 착석** + 세션 시작. `EndSession()`(임시 디버그 `K`) → 새벽 페이드. 손님 심사 로직은 미구현
- [x] `EnterUIModeEffect`(`화면고정`) — 모니터/컴퓨터 줌인. `CursorInteractor` 로 화면 버튼 클릭 (RenderTexture 커서 좌표 보정, `doc/0099`)
- [x] `ShowPanelEffect`(`읽기`) — 노트/편지 상호작용 시 오브젝트 켜고 플레이어 정지. ESC 계층에서 노트가 우선 소비
- [x] `ScreenFader` + `ActivateOnAwake` — Overlay 검정 페이드, 에디터에선 꺼두고 런타임에 켬

### 아트 / 에셋
- [x] 모텔방 프로토타입 모델링/텍스처링 (`Motel_Room` 프리팹), 시간대별 조명 프리팹
- [x] 3rd-party 프롭 정리 — 카테고리별 프리팹/머티리얼 분류, 중복 프롭 3,592개 제거 (`doc/0072`)
- [x] HDRP 전용 에셋 팩(Vintage Living Room 등) URP Lit 로 변환 — 마젠타 깨짐 해결
- [x] 신규 URP 머티리얼 Smoothness 기본 0 규칙, 알파 투명 머티리얼 스윕 절차 확립
- [x] 쇼핑카트 디테일 (진행 중, `CartGroundAlign` — 4바퀴 지면 정렬)

## 로드맵 (미구현)

`기획/기능정의서.md` 의 SYS-01~12 중 코어 루프의 접객·판별·경영 파트는 아직 코드가 없다.

- [ ] **접객 게임로직** — 손님 큐/대화, 신분증·서류 확인, TV 구별법 표시, 승인/거절 판단 (SYS-03~05). UI 모드 진입·자동 착석까지 완료, `ReceptionManager.EndSession()` 훅만 있고 손님 없음 (지금은 디버그 `K`)
- [ ] **일과 태스크 관리** — 아침 청소(쓰레기 줍기·침대 정리)·점심 일과의 "할일 완료" 게이트. 지금은 게시판/테이블/침대가 조건 없이 전환 (`TasksCompleteCondition : InteractionCondition` 훅 예정, SYS-02)
- [ ] **새벽 행동력** — 침대 취침 게이트, 숙박객 방별 대화/탐문 (SYS-10~11). 지금은 조건 없이 전환
- [ ] **객실 배정 UI**, **상점 UI/구매** (SYS-06, 08)
- [ ] **NPC** 이동/경로, 숙박객 대화, 새벽 총기 사용 (SYS-09)
- [ ] **몽유병 판별/살해 시뮬레이션** — 밤마다 감염자 수 대비 확률 처리, 게임오버 조건
- [ ] `StoryFlags` / `OverlayGate` / `DayPhaseManager` 3싱글톤으로 시스템 엮기 (`기획/상호작용-미니게임-연동-설계안.md`) — `DayPhaseManager` 만 존재
- [ ] 세계관 붕괴 연출 (도시 화재, 분위기 변화), 상점 제한, 엔딩 분기

## 스크립트 정리 분석

### 삭제 완료 (2026-08-27, `doc/0081` — 마이그레이션 완료 확인 후)

- `Interaction/Door.cs` → `HingeEffect`
- `Interaction/ItemDispenser.cs` → `SpawnObjectEffect`
- `Editor/InteractionMigrator.cs` (1회용)
- `Interactable.cs` 의 `LEGACY` 필드 블록 (`type`, `messyVisual`, `door` …) + `SetEquipTarget`
- `Environment/DayNightSwitcher.cs` → `PhaseVisuals` (2026-08-28, `doc/0100`)

### 삭제 검토 (Unity 템플릿/프로토타입 잔재)

| 대상 | 사유 |
|---|---|
| `Assets/TutorialInfo/` (`Readme.cs`, `ReadmeEditor.cs`) | Unity 템플릿 안내문. 게임 미사용 |
| `Assets/TextMesh Pro/Examples & Extras/` | TMP 샘플 씬/스크립트 40여 개. 게임 미사용 |
| `Assets/Scenes/SampleScene.unity`, `TestScene.unity` | 초기 프로토타입 씬. `InGame` 으로 대체됨 |
| `Assets/My/InGame/Editor/StripTestRoomProBuilder.cs` | 특정 테스트룸 ProBuilder 정리용 1회 유틸 — 역할 다했으면 삭제 |
| `Assets/Editor/AssetOrganizer.cs` | 프롭 분류 1회 실행 완료 (`doc/0072`). 재실행 안 하면 삭제, 남길 거면 문서화 |

### 수정 필요 / 개선 여지

| 스크립트 | 내용 |
|---|---|
| `SoundManager` | 게임 규모에 비해 너무 얇음 — BGM, SFX 카테고리 볼륨/뮤트, AudioSource 풀링, 3D 감쇠 등 없음. 접객/공포 연출 들어가기 전 확장 필요 |
| `ReceptionManager` | 접객 정상 종료가 디버그 `K` 뿐 (`ponytail:` 주석). 손님 큐(SYS-03~06/09) 나오면 "전원 처리 완료" 가 `EndSession()` 호출하도록 |
| `PhaseSwitchEffect` / 게시판·테이블·침대 | 할일 완료 여부와 무관하게 전환 — `TasksCompleteCondition` 훅 지점 |
| `UIInteractionMode.edgeLook` | 전역 플래그라 접객·모니터가 같은 값. 접객만 둘러보기 켜려면 `Enter(...)` 파라미터화 필요 |
| `InventorySystem.UpdateFlashlightHint()` | 매 호출 `GameObject.Find("Canvas")` + `transform.Find("HowToUse_Flashlight")` 문자열 탐색 — 직렬화 참조로 교체 |
| `InventorySystem` | 같은 `ItemId` 아이템 2개(소다 등)를 주우면 두 슬롯이 같은 손 오브젝트를 가리켜 `SelectSlot`/`SetActive` 충돌 — 소모품 다중 소지 규칙 정리 필요 |
| `CartGroundAlign` | `Quaternion.FromToRotation(transform.forward, targetUp)` — `forward` 를 지면 법선에 맞추면 카트가 앞으로 고꾸라짐. `transform.up` 이 맞을 가능성. (진행 중 표시된 기능) |
| `GazeInteractor` / `CursorInteractor` | `playerCamera` / `cam` null 시 NRE, 가드 없음 |
| `Interactor.Owner` | `?? gameObject` 폴백 + 탐색 실패 시 매번 `FindGameObjectWithTag` 재시도 — 씬에 Player 태그 없으면 조용히 오작동 |
| `PickupEffect` | `equipTargetOverride` 도 `itemId` 도 비면 조용히 `Destroy` — 연출용 의도지만 설정 실수 시 아이템이 사라짐 (경고 로그는 있음) |

## 시작하기

1. [Unity Hub](https://unity.com/download)에서 **Unity 6000.4.8f1** 설치.
2. 저장소를 클론한 뒤 Unity Hub에서 프로젝트 폴더를 엽니다.
3. `Assets/Scenes/InGame` 을 열어 Play.

## 기획 문서

- 원안: [`Git_Stuff/넉넉하우스키핑.md`](Git_Stuff/넉넉하우스키핑.md), 키비주얼 [`Git_Stuff/넉넉하우스키핑.png`](Git_Stuff/넉넉하우스키핑.png)
- 기능 정의서: [`기획/기능정의서.md`](기획/기능정의서.md) — SYS-01~12 기능 목록, 데이터 구조, 개발 우선순위
- 핵심 컨셉 분석 및 제언: [`기획/핵심컨셉-분석및제언.md`](기획/핵심컨셉-분석및제언.md) — 장르 차별화, 구별법/오판 시스템, 엔딩 분기
- 상호작용·미니게임 연동 설계안: [`기획/상호작용-미니게임-연동-설계안.md`](기획/상호작용-미니게임-연동-설계안.md) — SYS-01~06/10 통합 구조

## 개발 프로세스 메모

- 세션마다 사용자 요청과 변경 내역(코드 변경 전/후 포함)을 `doc/0001-...` 형식의 번호 매긴 마크다운으로 남깁니다. 번호는 세션이 바뀌어도 이어집니다. 특정 기능이 "왜" 지금 형태인지 궁금하면 `doc/` 의 관련 번호 문서를 먼저 확인하세요.
- `Docs/*.md` 는 세션 로그가 아니라 **스크립트별 코드 문서**입니다 (역할/필드/동작). 새 게임플레이 스크립트를 추가하면 `Docs/` 에 문서 1개 + `Docs/Overview.md` 표를 갱신합니다.
- 코드/에셋 변경은 먼저 `doc/` 제안서를 쓰고 승인받은 뒤 적용합니다.
