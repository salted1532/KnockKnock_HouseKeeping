# 대화 시스템 + NPC 관리

`Assets/My/Scripts/Dialogue/` · `Assets/My/Scripts/Game/`

숙박객 NPC 의 말풍선 대화와 접객 손님 큐. 설계·결정 이력은 [`doc/0102`](../doc/0102-dialogue-and-npc-system-design.md), 노드/선택지·번호 관리·거절 흐름은 [`doc/0104`](../doc/0104-dialogue-npc-data-and-reception-flow.md).

3축: **데이터**(CSV → SO) / **런타임**(말풍선·질문) / **NPC 관리**(손님 큐·판정). 표현은 데이터를 모르고, 데이터는 런타임을 모른다.

---

## 데이터

### CSV (대사 원본)

`Assets/My/Data/Dialogue/*.csv` — 구글시트/엑셀로 작성 후 내보내기. `sample.csv` 참고.

열: `npcId, situation, day, role, nodeKey, label, expression, text, goto, outcome`
(`goto`/`outcome` 은 없어도 됨)

| 열 | 값 |
|---|---|
| `npcId` | **숫자** (`NpcData.id`, 1~60) |
| `situation` | `Reception` / `Dawn` (대소문자 무관) |
| `day` | `0` = 모든 일차 공통(폴백), `N` = N일차 전용 |
| `role` | `Greeting`(자동 인사) / `Question`(허브 버튼) / `Node`(goto 로만 도달) / `Ambient` |
| `nodeKey` | `(npcId, situation)` 안에서 유일한 노드 이름. Greeting 은 보통 빈칸 |
| `label` | 질문/선택지 버튼 문구 |
| `expression` | `Neutral` / `Angry` — 줄별 표정 |
| `text` | 대사. `\n` → 줄바꿈 |
| `goto` | 다음 노드 nodeKey (대사 줄이면 자동 연결 / 선택지 행이면 그 선택의 목표). 빈 값 = 이 가지 종료(허브 복귀) |
| `outcome` | `Rejected` 면 이 노드를 다 읽었을 때 대화 전체 종료 + 거절 판정 |

같은 `(npcId, situation, day, role, nodeKey)` 연속 행 = 한 **노드**.
- `text` 채워진 행 = **대사 줄**. `label`/`goto` 는 노드 자체에 반영.
- `text` 비고 `label`/`goto` 있는 행 = **선택지**. 여러 개면 플레이어가 고름.

**재생**: lines 읽기 → `outcome==Rejected` 면 종료 → `choices` 있으면 플레이어 선택 → 없으면 `goto` 자동 → 둘 다 없으면 이 가지 끝(허브 복귀).

### `DialogueImporter` (에디터)

`Tools > Dialogue > Import CSV → DialogueDatabase` — 폴더의 모든 CSV 를 읽어 **`DialogueDatabase.asset`**(`Assets/My/Data/Dialogue/`) 을 통째로 재생성. 손으로 편집하지 않는다. 검사: 숫자 아닌 `npcId`·NpcData 없는 `npcId`·**끊긴 `goto`**(존재하지 않는 nodeKey) 경고. `Tools > Dialogue > Self-Check` 로 조회·분기 규칙 assert.

### `DialogueDatabase` (SO)

`List<DialogueEntry>` (= 노드들). 인덱스 2종(`RebuildIndex()`, 임포터가 호출):
- `Query(npcId, situation, day, role)` — 진입점(Greeting/Question). 오늘 일차 전용 → `day == 0` 폴백.
- `GetNode(npcId, situation, nodeKey, day)` — `goto` 대상 노드 1개. day 전용 → day 0 → 아무거나.

### `DialogueEntry` (노드 하나)

`npcId(int)`, `situation`, `day`, `role`, `nodeKey`, `label`, `lines[]`, `choices[] {label, goToNode}`, `goToNode`, `outcome(Verdict)`.

### `NpcData` (SO) — NPC당 1개, 손으로 작성

| 필드 | 설명 |
|---|---|
| `id` (int) | CSV `npcId` 와 일치. **1~60** (`OnValidate` 범위 경고) |
| `displayName` | 표시 이름 |
| `neutralPortrait` / `angryPortrait` | 말풍선 표정 스프라이트 |
| `modelPrefab` | NPC별 3D 모델. **현재 미사용** — 씬의 공용 손님 오브젝트 하나를 재사용 |
| `isSleepwalker` | **내부 정답** — 몽유병 환자인가 (플레이어에게 안 보임) |
| `visitorOnly` | 숙박 안 하고 대화만 (메시지 전달 인물) — 접객 판정 스킵 |
| `idCard` (`IdCard`) | 신분증 정보 + `forged` 위조 플래그 (SYS-04, 아직 미사용) |

`Portrait(Expression)` — Angry 이고 angryPortrait 있으면 그것, 아니면 neutral.

### `NpcCatalog` (SO, 1개) — 번호 → NpcData

`List<NpcData> npcs` + `Get(int id)`. `CampaignData` 와 `ReceptionManager` 가 번호를 NpcData 로 바꿀 때 쓴다.
에디터: 우클릭 **"프로젝트의 NpcData 전부 수집"** + 중복 id 경고.

### `CampaignData` (SO, 1개) — 캠페인 편성, 구 `DayData` 대체

`List<DayPlan>` — 일차를 리스트로 추가. `Day(int)` 로 오늘 것 조회.
`DayPlan { int day, List<int> eveningGuestIds(손님 번호 순서), string nightNews }`. `OnValidate` day 중복 경고.

---

## 런타임

### `SpeechBubble` (컴포넌트, NPC 프리팹)

NPC 머리 오른쪽 위 World Space Canvas. `root` 를 켜고 끄며 켜진 동안 `Camera.main`(비우면) 으로 빌보드. 타이핑 연출 내장 — 클릭/E/Space 로 즉시 완성 → 다음 줄.

`IEnumerator Show(NpcData npc, IReadOnlyList<DialogueLine> lines)` — 줄마다 초상화 교체 + `DialogueRunner.OnLineShown` 발동 + 타이핑 + 입력 대기. `Hide()`.

| 필드 | 설명 |
|---|---|
| `root` | 켤/끌 말풍선 루트 (시작 시 자동 off) |
| `label` (`TMP_Text`) | 대사 텍스트 |
| `portrait` (`Image`, 선택) | 줄별 표정 |
| `billboardTarget` | 비우면 `root.transform` |
| `faceCamera` | 비우면 `Camera.main` |
| `charInterval` | 글자당 딜레이(초) |

> InGame 은 월드가 RenderTexture 경유지만 MainCamera 가 같이 그리므로 좌표 변환 불필요 ([[ingame-rendertexture-pipeline]]).

### `DialogueRunner` (싱글턴)

`Play(NpcData npc, SpeechBubble bubble, Situation situation, Action<Verdict> onResult = null)`:
1. 오늘 일차 계산 (`DayPhaseManager.DayCount`, 없으면 1)
2. `Greeting` 노드들 순차 재생 (`PlayNode` — 재귀)
3. **질문 허브**: `Question` 노드로 버튼 목록 → 선택 시 그 노드 `PlayNode` → 끝나면 허브 재표시. "대화 종료" 까지 반복
4. `PlayNode(entry)`: `lines` 읽기 → `OnNodeReached(npc, nodeKey)` → `outcome==Rejected` 면 거절 플래그+종료 → `choices` 있으면 버튼→선택→`goto` 노드로 재귀 → 없으면 `goToNode` 자동 재귀
5. `bubble.Hide()` + `onResult(거절이면 Rejected, 아니면 None)`

`maxNodeVisits`(기본 40) — 이 횟수 넘게 노드 방문 시 순환으로 보고 중단.

이벤트: `OnDialogueStarted(npc)` / `OnDialogueEnded(npc)` / `OnLineShown(npc, line)` / **`OnNodeReached(npc, nodeKey)`** — SFX·카메라·표정·**점프스케어** 훅 (거절 반복의 마지막 노드에 씬 컴포넌트가 구독해 연출, doc/0103).

필드: `database`, `questionPanel`, `maxNodeVisits`.

### `QuestionPanel` (UI, 접객 오버레이 캔버스)

버튼 목록 하나로 **질문 허브**(반복 선택)와 **선택지**(1회) 둘 다 그린다. `DialogueRunner` 가 구동.

- `Show(labels, onPick(int), showDone)` — 버튼 생성. `onPick(-1)` = "대화 종료"(showDone 일 때만)
- `CanAsk` (`Func<bool>`, null=항상 가능) — 새벽 탐문 행동력 체크
- `OnAsked` (`Action`) — 질문 1건 답변 후 (행동력 차감)
- `SetInteractable(bool)` · `Close()`

행동력 시스템 자체는 별도 — 패널은 훅만 받는다.

---

## NPC 관리

### Guest 프리팹 (접객 중 NPC)

접객 큐가 세션당 **1개 인스턴스**를 만들어 손님마다 재활용한다. 프리팹 구성:
- `GuestMover` — 이동. `WarpTo(Transform)` + `WalkThrough(IReadOnlyList<Transform>)` 코루틴. 경로는 **씬(ReceptionManager)이 넘겨줌** (프리팹이라 씬 참조 못 가짐). 직선 이동 + 진행방향 회전 + (있으면) Animator `Walking` bool. `speed`/`turnSpeed`/`arriveDistance`.
- `GuestView` — 겉모습. `body` (`SpriteRenderer`) 를 `NpcData` 초상화로 교체. `Apply(npc)` 시 Neutral + `DialogueRunner.OnLineShown` 구독(줄별 표정), `Clear()` 시 해제.
- 자식 `SpeechBubble` — 말풍선.
- `Interactable`(상호작용) + `CheckInGuestEffect` + Collider(Interaction 레이어) — 클릭 체크인용.

> ponytail: NavMesh·경로탐색 없음. 장애물 회피 필요하면 `NavMeshAgent` 로 교체.

### `GuestManager` (싱글턴)

이번 판 손님 상태. `GuestState { npc, room, verdict, checkInDay }`, `enum Verdict { None, Approved, Rejected, Killed }`.

`CheckIn(npc, room, day)` · `SetVerdict(npc, v, day)` · `CheckOut(npc)` · `Get(npc)` · `Active` (읽기 전용).

밤 판정(누가 죽나)은 별도 시스템 — 여기선 `npc.isSleepwalker`(정답) vs `state.verdict`(플레이어) 데이터만 보관. 이 괴리가 오판 시스템의 근거.

### `CheckInGuestEffect` (`InteractionEffect`, 손님 오브젝트)

접객 중 손님을 클릭하면 체크인(승인). `ReceptionManager.AwaitingCheckIn` 일 때만 `ConfirmCheckIn()` 호출. 그 외엔 로그만.
> (b) 테스트판 — 열쇠·방배정 검사 없음. 손님 오브젝트에 `Interactable`(상호작용) + Collider(Interaction 레이어) 필요. 후속 doc 에서 `RoomKey` 대조 추가.

### `ReceptionManager` (재작성)

`Evening` 진입 → `BeginSession()`: `UIInteractionMode.Enter(receptionAnchor)` + `OnSessionStarted` + (오늘 편성 있으면) 손님 큐 코루틴.

**손님 큐** (`GuestQueue`): 세션 시작 시 `guestPrefab` 을 1개 `Instantiate` (재활용). `campaign.Day(DayCount).eveningGuestIds` 순회 →
번호 → `catalog.Get(id)` → `view.Apply(npc)` (스프라이트 교체) → `mover.WarpTo(guestSpawn)` → `mover.WalkThrough(entryPath)` →
`DialogueRunner.Play(npc, bubble, Reception, onResult)` →
- `visitorOnly` → `WalkThrough(exitPath)` (대화만)
- `onResult == Rejected` (대화에서 거절 노드) → `SetVerdict(Rejected)` + `WalkThrough(exitPath)`
- 그 외 → **`AwaitingCheckIn = true`** → 플레이어가 손님 클릭(`CheckInGuestEffect` → `ConfirmCheckIn`) → `CheckIn(npc, nextRoom++)` + `WalkThrough(roomPath)`
→ `view.Clear()` → 다음. 큐 끝 → 인스턴스 `Destroy` + `EndSession()`.

| 필드 | 설명 |
|---|---|
| `receptionAnchor` | 플레이어 착석 위치/정면 |
| `campaign` (`CampaignData`) | 캠페인 편성 (일차 리스트) |
| `catalog` (`NpcCatalog`) | 번호 → NpcData |
| `guestPrefab` (`GameObject`) | 접객 NPC 프리팹 (GuestMover + GuestView + 자식 SpeechBubble + Interactable/CheckInGuestEffect). 세션당 1개 인스턴스 재활용 |
| `guestSpawn` (`Transform`) | 스폰/리셋 위치 |
| `entryPath` / `exitPath` / `roomPath` (`Transform[]`) | 스폰→카운터 / 카운터→밖 / 카운터→방. 씬 트랜스폼, GuestMover 에 넘김 |
| `firstRoomNumber` (기본 101) | 승인 시 자동 증가 |
| `enterDelay` (기본 0.6) | 착석/페이드 후 첫 손님까지 |
| `debugEndKey` (K) | 즉시 종료 (큐 중단 → 새벽) |

프로퍼티: `InSession`, **`AwaitingCheckIn`**. 메서드: `ConfirmCheckIn()`(CheckInGuestEffect 용), `EndSession()`.
편성/`guestPrefab`/`catalog` 없으면 큐 없이 디버그 K 로만 종료. ESC 로 접객 완전 탈출(`HandleUIExit`) 시 큐 중단 + 인스턴스 파괴 + 세션 정리, 하루 전환 없음.

**거절 흐름**: 대화 CSV 에서 저작. "거절한다" `Question` → 조르기 `Node`(선택지로 되돌아옴) → 마지막 `Node` 에 `outcome=Rejected`. `insistCount` 같은 코드 필드 없음 — 노드 체인 깊이로 조절. `sample.csv` 에 예시(`reject` / `reject_insist` / `reject_final`).

> 손님 겉모습은 2D 스프라이트(`GuestView.body`). NpcData 초상화 2종을 재사용 — 대화 중 표정도 반영. 3D 모델 스왑은 후속(`NpcData.modelPrefab`).

---

## 새벽 탐문 (SYS-10)

체류 손님(`GuestManager.Active`)에게 접근 → `DialogueRunner.Play(npc, bubble, Situation.Dawn)` + `QuestionPanel.CanAsk/OnAsked` 에 행동력 연결. 트리거(문/NPC 상호작용)와 행동력 시스템은 후속.

## 스킵 (YAGNI)

비주얼 노드 그래프 에디터/Ink/Yarn (분기는 CSV `goto` 로 충분) · `condition` 열(조건부 대사 — 필요할 때 추가) · 로컬라이제이션 레이어(`text_ko`/`text_en` 열은 나중) · 줄별 음성/애니 트리거 · 밤 판정·엔딩 집계 · 선택 결과 로깅 · 신분증 확인 UI(SYS-04) · 모니터 방배정·`RoomKey` 대조·객실 배치도(SYS-06 풀버전) — doc/0104 후속.

## 관련

[ReceptionManager](ReceptionManager.md) · [DayPhaseManager](DayPhaseManager.md) · [UIInteractionMode](UIInteractionMode.md) · [`doc/0102`](../doc/0102-dialogue-and-npc-system-design.md)
