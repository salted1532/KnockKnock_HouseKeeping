# 0102 - 대화 시스템 + NPC 관리 설계 (제안)

날짜: 2026-08-29

## 요청 (원문)

> 대화 시스템 만들기 / 숙박객 npc 말풍선 / 질문 하기
>
> **<대화 시스템>**
> - npc가 말하는 각 텍스트별로 애니메이션(표정, 스프라이트 변화)을 조정할 수 있게 해서 특정 대사에서 표정/이미지가 변하도록 함 (콤보박스로 조정)
> - npc별로 기본, 화남 2가지만 일단 존재
> - npc마다 상황별 대사집이 따로 존재 → 접객, 새벽 2개로 나뉘고 부가 상황이 더 있을 수 있음
> - 각 일차별 대사도 따로 존재
> - 각 질문에 대한 대사도 존재
> - 규모: 못해도 NPC 60명 + 몽유병 환자 구성까지
>
> 대화 시스템 구현하려는데 npc 관리도 같이 설계해야 할 것 같음. 설계안 만들어줘.

## 조사

- 기존 아키텍처: 싱글턴 매니저(`DayPhaseManager`, `ReceptionManager`, `UIInteractionMode`) + 컴포넌트 조합(`InteractionEffect` / `InteractionCondition`). ScriptableObject 는 아직 미사용이나 데이터가 많아지는 이 기능엔 자연스러움.
- `DayPhaseManager.DayCount` (int, 1부터), `Current`(DayPhase) 이미 존재 → 일차별/상황별 분기에 그대로 사용.
- `ReceptionManager` 는 저녁(Evening) 진입 시 `UIInteractionMode.Enter(receptionAnchor)` 로 착석 + 커서 모드. 접객 대화는 이 세션 안에서 돌아감. 손님 큐(SYS-03~06/09)는 아직 `EndSession()` API + 디버그 K 뿐 — **숙박객 데이터/큐가 이 설계의 담당 범위**.
- `InGame` 렌더 경로: 월드 → RenderTexture → 풀스크린 RawImage. **월드 스페이스 UI(말풍선)는 MainCamera 가 RT에 같이 그리므로 문제 없음.** 스크린 스페이스 UI(질문 패널)만 접객 오버레이 캔버스에 얹으면 됨 ([[project_ingame-rendertexture-pipeline]]).
- 기획: `기능정의서.md` SYS-03(접객 대화), SYS-10(새벽 탐문 — SYS-03 재사용 + 전용 질문셋), 4장 데이터 구조(GuestData / DayData). `핵심컨셉-분석및제언.md` 5·6·8장 — **"실제 감염 여부(내부 정답)"와 "플레이어 판단"을 별도 저장**, 대화는 구별법과 충돌하는 인간적 정보 제공.
- 아임낫휴먼(57+8) / 미드나잇쉬프트(70) / 도플갱어(7~9) → 실질 목표 NPC 60+ + 몽유병 환자.

---

## 1. 큰 그림

3개 축으로 분리한다. 대화 **데이터**(누가 뭐라고 하나) / 대화 **런타임**(말풍선·타이핑·진행) / NPC **관리**(누가 언제 오나, 배정, 판정).

```
NpcData (SO)            ── 정체성: 이름, 초상화(기본/화남), 신분증, isSleepwalker(내부 정답)
  └ NpcDialogue (SO)    ── 그 NPC의 대사 전부: 상황 × 일차 × 역할(인사/질문/기타)
DayData (SO)            ── 일차별: 저녁 방문 손님 목록, 새벽 체류 손님, 밤 뉴스 텍스트
─────────────────────────────────────────────────────────
DialogueRunner (싱글턴) ── NpcDialogue + 상황 받아 → 해당 일차 대사 뽑아 순서대로 재생
SpeechBubble (컴포넌트) ── NPC 프리팹에 부착. 월드 말풍선: 초상화 Image + TMP + 타이핑
QuestionPanel (UI)     ── 현재 (NPC, 상황, 일차) 의 질문 목록 버튼 → 클릭 시 답변 재생
─────────────────────────────────────────────────────────
GuestManager (싱글턴)   ── 이번 판 손님 상태: 배정 객실, 플레이어 판정(Verdict). 밤 로직이 읽음
```

핵심 원칙: **표현(말풍선)은 데이터를 모른다. 데이터(SO)는 런타임을 모른다.** 새 상황·새 표정·새 NPC 는 데이터만 늘리면 되고 코드는 안 건드린다.

---

## 2. 데이터 모델

### `Expression` (enum) — 요청한 "콤보박스"
```csharp
public enum Expression { Neutral, Angry }   // 지금은 2개. 끝에만 추가 (정수 직렬화)
```
각 대사 줄에 이 enum 필드 → 인스펙터에서 드롭다운(콤보박스)으로 선택. `Neutral`/`Angry` 초상화는 `NpcData` 에 있음.

### `Situation` (enum)
```csharp
public enum Situation { Reception, Dawn }   // 접객 / 새벽 탐문. 부가 상황은 끝에 추가 (Checkout, Event ...)
```

### `DialogueLine` (직렬화 struct) — 대사 한 줄
```csharp
[Serializable] public struct DialogueLine
{
    public Expression expression;        // ← 콤보박스
    [TextArea(2, 4)] public string text;
    // 후속 훅(지금 안 만듦): float holdSeconds; AudioClip voice; string animTrigger;
}
```

### `NpcData` (ScriptableObject) — NPC 1명 = 에셋 1개
```csharp
[CreateAssetMenu(menuName = "KnockKnock/Npc Data")]
public class NpcData : ScriptableObject
{
    public string displayName;
    public Sprite neutralPortrait;
    public Sprite angryPortrait;
    public Sprite Portrait(Expression e) => e == Expression.Angry ? angryPortrait : neutralPortrait;

    [Header("게임 로직 (밤 판정·신분증 확인용)")]
    public bool isSleepwalker;           // 내부 정답. 플레이어에겐 안 보임
    public bool visitorOnly;             // 숙박 안 하고 대화만 (메시지 전달 인물)
    public IdCard idCard;                 // 신분증 정보 + 위조 플래그 (SYS-04)
}

[Serializable] public struct IdCard
{
    public string name, birthDate;
    public Sprite photo;
    public bool forged;                  // 위조 여부(내부 정답)
}
```
초상화가 나중에 3개 이상이면 `Sprite[] portraits` + `Portrait(e) => portraits[(int)e]` 로 바꿈. 지금은 2개라 필드 2개가 더 읽기 쉬움.

### `NpcDialogue` (ScriptableObject) — 그 NPC의 대사 전부
```csharp
[CreateAssetMenu(menuName = "KnockKnock/Npc Dialogue")]
public class NpcDialogue : ScriptableObject
{
    public NpcData npc;
    public List<DialogueEntry> entries = new();
}

public enum EntryRole { Greeting, Question, Ambient }

[Serializable] public class DialogueEntry
{
    public Situation situation;          // Reception / Dawn
    public int day;                      // 0 = 모든 일차 공통(폴백). 3 = 3일차 전용
    public EntryRole role;               // Greeting=입장 인사, Question=질문 답변, Ambient=기타
    public string key;                   // 질문 식별자 ("family", "job" ...). Greeting/Ambient 는 비워둠
    public string label;                 // 질문 버튼 문구 (Question 전용)
    public List<DialogueLine> lines = new();
}
```

**조회 규칙** (`DialogueRunner` 가 사용):
```
Query(situation, day, role):
    exact = entries where situation & role 일치 && day == 오늘일차
    return exact 있으면 exact, 없으면 entries where ... && day == 0
```
→ "3일차 전용 인사"가 있으면 그걸, 없으면 "공통 인사". 질문도 동일 — 일차마다 질문 목록/답변이 달라질 수 있음.

### `DayData` (ScriptableObject) — 일차별 편성
```csharp
[CreateAssetMenu(menuName = "KnockKnock/Day Data")]
public class DayData : ScriptableObject
{
    public int day;
    public List<NpcData> eveningGuests;  // 이 날 저녁 접객에 오는 순서
    [TextArea] public string nightNews;  // 취침 시 출력 (SYS-11)
    // 아침/점심 태스크(SYS-02)는 태스크 시스템 나올 때 여기 추가
}
```
새벽 체류 손님은 "이전 일차에 승인되어 아직 체크아웃 안 한 손님" → `GuestManager` 가 런타임으로 관리(데이터에 중복 안 씀).

---

## 3. 런타임

### `SpeechBubble` (컴포넌트, NPC 프리팹에 부착)
- NPC 머리 위 앵커에 **World Space Canvas** 자식. 매 프레임 MainCamera 쪽으로 빌보드.
  (RT 경유라도 MainCamera 가 같이 그리므로 그대로 보임. 스크린 좌표 변환 불필요.)
- 구성: 9-slice 배경 Image + 초상화 Image + TMP 텍스트.
- `IEnumerator Show(NpcData npc, IReadOnlyList<DialogueLine> lines)`:
  줄마다 → `portrait.sprite = npc.Portrait(line.expression)` → 타이핑 코루틴 → 클릭/E 대기 → 다음 줄.
- 타이핑: `Typewriter` 15줄짜리 코루틴(문자당 딜레이, 클릭하면 즉시 완성). TMP 예제 `TeleType` 는 채팅 시뮬용이라 안 씀.

### `DialogueRunner` (싱글턴)
```csharp
public void Play(NpcDialogue d, Situation s, Action onComplete = null);
```
1. `day = DayPhaseManager.Instance.DayCount`
2. `Query(s, day, Greeting)` 의 각 엔트리 → `bubble.Show(d.npc, entry.lines)` 순차
3. `Query(s, day, Question)` → 목록을 `QuestionPanel` 에 전달
4. 패널에서 질문 선택 → `bubble.Show(d.npc, 그 엔트리 lines)` → 패널 복귀
5. 패널 닫기 → `onComplete()`
- 이벤트: `event Action<NpcData> OnDialogueStarted / OnDialogueEnded`, `event Action<DialogueLine> OnLineShown` — SFX·카메라·퀘스트 플래그 훅.

### `QuestionPanel` (UI, 접객 오버레이 캔버스)
- 질문 엔트리 리스트 → 버튼 생성(`entry.label`). 클릭 → `DialogueRunner` 에 콜백.
- **새벽 탐문 행동력(SYS-10)**: 질문 1회 = 행동력 1 소모. 행동력 0이면 버튼 비활성.
  행동력 자체는 별도(`DawnManager` 등) 담당 — 패널은 `Func<bool> canAsk` / `Action onAsked` 훅만 받음.
- 접객엔 행동력 없음 → 훅 null.

---

## 4. NPC 관리

### `NpcDirectory` (싱글턴 or SO 레지스트리)
- 모든 `NpcDialogue` 에셋을 로드(폴더 스캔 or 직렬화 리스트) → `Dictionary<NpcData, NpcDialogue>`.
- `NpcDialogue For(NpcData npc)`. `DayData.eveningGuests` 는 `NpcData` 만 들고, 대사는 여기서 조회.

### `GuestManager` (싱글턴) — 이번 판 손님 상태
```csharp
public enum Verdict { None, Approved, Rejected, Killed }

public class GuestState
{
    public NpcData npc;
    public int room = -1;
    public Verdict verdict = Verdict.None;
    public int checkInDay;
}
```
- `List<GuestState> active` — 현재 체류 중. `ReceptionManager` 가 `DayData` 로 그날 손님을 큐잉, 판정/객실배정 결과를 여기 기록(SYS-05/06).
- **밤 판정 로직**(누가 죽나 — `핵심컨셉` 6·7장)은 별도 시스템. 여기선 데이터 훅만: `npc.isSleepwalker`(정답) vs `state.verdict`(플레이어). 이 괴리를 사후에 드러내는 게 오판 시스템.
- 몽유병 환자 = 별도 타입 아님. `NpcData.isSleepwalker = true` 인 일반 NPC. 도플갱어/스토리 인물도 전부 `NpcData` + 플래그/상황 차이.

### 접객 흐름 연결 (`ReceptionManager` 수정)
- `BeginSession()` → `DayData` 에서 오늘 `eveningGuests` 큐.
- 손님 1명: NPC 프리팹을 접객 자리에 스폰/활성 → `DialogueRunner.Play(dir.For(npc), Reception, onComplete: 심사UI 열기)`.
- 심사(신분증·승인/거절·객실배정) 후 다음 손님. 큐 비면 `EndSession()` (디버그 K 대체).

### 새벽 탐문 연결 (SYS-10)
- 체류 손님 방문(문/NPC 상호작용) → `DialogueRunner.Play(dir.For(npc), Dawn, ...)`.
- `QuestionPanel` 에 행동력 훅 연결.

---

## 5. 규모 & 대사 작성 워크플로 (결정 필요)

NPC 60+ × 상황 2 × 일차별 변형 × 질문 여러 개 = 대사량이 큼. **데이터 모델과 런타임 API 는 아래 두 방식 모두에서 동일** — 작성 프런트엔드만 다름.

| | A. 인스펙터 (NpcDialogue SO 직접 편집) | B. CSV/스프레드시트 + 임포터 |
|---|---|---|
| 작성 | NPC당 에셋 1개, `entries` 리스트에 줄 추가, expression 드롭다운 | 구글시트/엑셀에 행 입력 (`npcId, situation, day, role, key, label, expression, text`), 내보내기 → 에디터 임포터가 `NpcDialogue` 들 생성/갱신 |
| 콤보박스 | 인스펙터 enum 팝업 (요청 그대로) | 텍스트 컬럼(임포트 시 검증). 팝업 아님 |
| 대량/번역 | 에셋 60개 클릭 이동 — 후반에 번거로움 | 시트에서 필터·복붙·일괄수정. 비프로그래머도 작성 가능 |
| 추가 코드 | 0 (Unity 기본) | 임포터 ~60줄 (의존성 없음) |
| 의존성 | 없음 | 없음 (Ink/Yarn 안 씀 — 분기가 얕음: 인사 + 평면 질문목록) |

**추천: A 로 시작.** 데이터 구조가 임포터 친화적으로 이미 잡혀 있어, 인스펙터 클릭이 실제로 병목이 될 때 B(반나절)를 얹으면 됨. 런타임은 안 바뀜. → **누가 대사를 쓰나?** 코더가 쓰면 A, 시트로 외주/분업하면 처음부터 B.

**Ink/Yarn Spinner 안 쓰는 이유**: 여기 분기는 "인사말 → 질문 목록(평면) → 답변 → 목록 복귀" 뿐. 노드 그래프·조건 스크립팅이 필요 없고, 진짜 부담은 분기가 아니라 **분량**(시트가 답). 의존성·학습비용만 늘어남.

---

## 6. 스킵 (YAGNI — 필요해지면 훅으로)

- **노드 그래프 에디터 / Ink / Yarn** — 5장 참고.
- **CSV 임포터** — 인스펙터 작성이 실제로 아플 때. 데이터 모델 그대로.
- **로컬라이제이션 레이어** — 2번째 언어가 실제로 잡힐 때. `text` 를 키로 바꾸는 건 나중.
- **줄별 음성(VO) / 애니메이터 트리거** — `DialogueLine` 에 필드만 예약, 배선은 나중.
- **초상화 트윈/연출** — 지금은 스프라이트 교체만. Animator 훅은 나중.
- **밤 판정·엔딩 집계·평판** — 별도 시스템. 여기선 `isSleepwalker` / `Verdict` 데이터만.
- **NPC 이동/경로(SYS-09)** — 접객·탐문은 정지 상태 대화. 이동은 별개.

---

## 7. 파일 목록 (제안)

```
Assets/My/Scripts/Dialogue/
  Expression.cs            신규  enum
  Situation.cs             신규  enum
  DialogueLine.cs          신규  직렬화 struct
  NpcData.cs               신규  SO (정체성 + 초상화 + IdCard + isSleepwalker)
  NpcDialogue.cs           신규  SO (DialogueEntry 리스트 + Query)
  NpcDirectory.cs          신규  싱글턴/레지스트리 (NpcData → NpcDialogue)
  DialogueRunner.cs        신규  싱글턴 (재생 오케스트레이션)
  SpeechBubble.cs          신규  컴포넌트 (월드 말풍선 + 빌보드)
  Typewriter.cs            신규  타이핑 코루틴 (~15줄)
  QuestionPanel.cs         신규  UI (질문 목록 + 행동력 훅)
Assets/My/Scripts/Game/
  DayData.cs               신규  SO (일차별 편성)
  GuestManager.cs          신규  싱글턴 (GuestState / Verdict)
  ReceptionManager.cs      수정  DayData 큐잉 + DialogueRunner 연결 (EndSession 자동화)
Assets/My/Data/
  Npc/*.asset              NpcData      (NPC당 1개)
  Dialogue/*.asset         NpcDialogue  (NPC당 1개)
  Days/Day01.asset ...     DayData
Docs/DialogueSystem.md     신규  스크립트 레퍼런스 (구현 후)
```

프리팹: NPC 프리팹(들)에 `SpeechBubble` + 머리 위 앵커. 접객/새벽 씬에 `DialogueRunner`·`GuestManager`·`NpcDirectory` 빈 GO.

---

## 8. 확인 필요

1. **작성 워크플로 (5장)**: A(인스펙터, 추천) 로 시작 vs 처음부터 B(CSV 임포터). 대사 작성 주체는?
2. **`Situation` 초기값**: `Reception`, `Dawn` 2개로 시작 OK? 부가 상황(체크아웃/이벤트)은 나중 추가.
3. **`EntryRole`**: `Greeting`(입장 인사) / `Question`(질문 답변) / `Ambient`(그 외) 3분류 OK? `Ambient` 는 지금 쓸 데 없으면 빼도 됨.
4. **말풍선 형태**: 월드 스페이스 말풍선(초상화 작게 내장) OK? 아니면 접객은 페이퍼스플리즈式 화면 하단 대화창 + 큰 초상화 패널을 별도로?
5. **초상화 소스**: 2D 스프라이트 교체 확정? (NPC 3D 모델 얼굴을 바꾸는 방식 아님)
6. **`NpcData` 에 게임 로직(신분증/isSleepwalker) 동거** vs 대사용/판별용 SO 분리. 지금 규모엔 동거 추천 — 확인.
7. **`DayData` vs 코드**: 일차 편성을 SO 에셋으로 (`Day01.asset`...) OK? 아니면 하나의 `Campaign` SO 에 배열.
8. **행동력(SYS-10)**: 이 설계는 훅(`canAsk`/`onAsked`)만 둠. 행동력 시스템 자체는 별도 doc 확인.
9. **범위**: 이번 구현에 어디까지? (a) 대사 데이터 모델 + 런타임(말풍선/질문) 만, 접객·새벽 연결은 더미 트리거 / (b) `ReceptionManager` 큐잉 + `GuestManager` 까지.

---

## 9. 확정 (2026-08-29, 확인 답변 반영) — 이 섹션이 최종

**1. 대사 데이터는 처음부터 외부 파일(CSV).** 인스펙터 직접편집 안 함 — 나중에 번역·일괄수정이 어려워지므로.
- 대사 원본 = `Assets/My/Data/Dialogue/*.csv` (구글시트/엑셀로 작성 → CSV 내보내기).
- `Tools > Dialogue > Import CSV → DialogueDatabase` 실행 → 폴더의 모든 CSV 를 읽어 **단일 `DialogueDatabase.asset`** 을 통째로 재생성. 이 에셋은 손으로 안 고침.
- 번역: 나중에 `text` 대신 `text_ko` / `text_en` 열 추가하거나 로케일별 CSV 폴더. 데이터 구조·런타임 안 바뀜.
- `NpcData` (초상화 스프라이트·플래그) 는 CSV 에 못 담으므로 SO 로 유지. NPC당 1개 에셋, `id` 필드가 CSV 의 `npcId` 와 매칭.

**2. 말풍선 = 월드 스페이스 캔버스, NPC 오른쪽 위.** 접객 모드에서 NPC 가 플레이어 앞으로 걸어온 뒤, NPC 머리 오른쪽 위 World Space Canvas 에 텍스트로 대사 출력. 매 프레임 MainCamera 로 빌보드. 초상화 Image 는 말풍선 안에 배치(선택) + `OnLineShown` 이벤트로 3D 표정 교체 등 확장 여지.

**3. 전체 시스템 구축** — 데이터 모델 + 런타임(말풍선/질문) + `ReceptionManager` 손님 큐 + `GuestManager` + **손님 입장 이동**까지.

**4. 손님 입장 이동 (신규 `GuestMover`).** "AI" 라기보단 웨이포인트 직선 이동. NavMesh·경로탐색 없음.
- `WalkThrough(웨이포인트 목록)` 코루틴 — 각 지점으로 직선 이동 + 진행방향 회전 + (있으면) Animator `Walking` bool.
- ponytail 주석으로 상한 표시: 장애물 회피 필요하면 `NavMeshAgent` 로 교체.

### 최종 데이터 스키마 (CSV)

열: `npcId, situation, day, role, key, label, expression, text`

| 열 | 설명 |
|---|---|
| `npcId` | `NpcData.id` 와 매칭 |
| `situation` | `Reception` / `Dawn` (대소문자 무관) |
| `day` | `0` = 모든 일차 공통(폴백), `3` = 3일차 전용 |
| `role` | `Greeting`(입장 인사) / `Question`(질문 답변) / `Ambient` |
| `key` | 질문 식별자 (`name`, `purpose` …). Greeting/Ambient 는 빈칸 |
| `label` | 질문 버튼 문구 (Question 전용) |
| `expression` | `Neutral` / `Angry` — 줄별 표정(요청한 "콤보박스"의 데이터 형태) |
| `text` | 대사. `\n` 은 줄바꿈으로 변환 |

같은 `(npcId, situation, day, role, key)` 연속 행 = 한 대사 묶음의 여러 줄 (순서대로 재생).

**조회 규칙**: `Query(npcId, situation, day, role)` → 오늘 일차 전용이 있으면 그것, 없으면 `day == 0` 폴백.

### 최종 접객 흐름 (`ReceptionManager` 재작성)

```
저녁 진입 → BeginSession → UIInteractionMode.Enter(receptionAnchor) + 손님 큐 코루틴 시작
  DayData(오늘 일차) 없으면 → 기존 동작(디버그 K 로만 종료)
  foreach 손님 in DayData.eveningGuests:
     NpcData.prefab(없으면 fallbackGuestPrefab) 을 guestSpawn 에 생성
     GuestMover.WalkThrough(guestPath + guestStand)
     DialogueRunner.Play(npc, bubble, Reception)
       → Greeting 재생 → QuestionPanel(질문 목록) → "대화 종료" 클릭까지
     visitorOnly 아니면 → ApprovalPanel(승인/거절)
       승인 → GuestManager.CheckIn(npc, 다음 객실번호), 거절 → SetVerdict(Rejected)
     GuestMover.WalkTo(guestExit) → NPC 파괴
  큐 끝 → EndSession() (ExitAll + OnSessionEnded + Advance → 새벽)
```

- 디버그 K = `EndSession()` 즉시 호출(큐 중단) 로 유지.
- 신분증 확인(SYS-04), 객실 배치도 UI(SYS-06 풀버전) 는 이번 범위 밖 — `ApprovalPanel` 은 승인/거절 2버튼 + 객실번호 자동 증가만.

### 최종 파일 목록

```
Assets/My/Scripts/Dialogue/
  Expression.cs           신규  enum { Neutral, Angry }
  Situation.cs            신규  enum { Reception, Dawn }
  DialogueLine.cs         신규  struct DialogueLine + enum EntryRole + class DialogueEntry
  NpcData.cs              신규  SO: id, displayName, 초상화 2종, prefab, isSleepwalker, visitorOnly, IdCard
  DialogueDatabase.cs     신규  SO: List<DialogueEntry> + Query (일차 폴백) + RebuildIndex
  SpeechBubble.cs         신규  컴포넌트: root 토글 + 빌보드 + 타이핑(내장) + Show(npc, lines)
  QuestionPanel.cs        신규  UI: 질문 버튼 생성, CanAsk/OnAsked 훅(행동력), SetInteractable
  DialogueRunner.cs       신규  싱글턴: Play(npc, bubble, situation, onComplete), 이벤트 3종
  GuestMover.cs           신규  컴포넌트: WalkThrough/WalkTo 코루틴 (웨이포인트 직선)
  Editor/DialogueImporter.cs  신규  Tools 메뉴: CSV 폴더 → DialogueDatabase.asset 재생성
Assets/My/Scripts/Game/
  DayData.cs              신규  SO: day, eveningGuests(List<NpcData>), nightNews
  GuestManager.cs         신규  싱글턴: GuestState/Verdict, CheckIn/SetVerdict/CheckOut
  ApprovalPanel.cs        신규  UI: 승인/거절 2버튼, Open(onApprove, onReject)
  ReceptionManager.cs     수정  손님 큐 코루틴, guest 스폰/이동/판정, EndSession 자동
Assets/My/Data/
  Dialogue/sample.csv     신규  샘플 대사 (형식 예시)
  Dialogue/DialogueDatabase.asset   임포터가 생성
  Npc/Npc_*.asset         사용자 작성 (NPC당 1개)
  Days/Day01.asset …      사용자 작성
Docs/DialogueSystem.md    신규  스크립트 레퍼런스 (구현 후)
```

`NpcDirectory` / `Typewriter` 별도 파일 폐기 — Directory 는 불필요(DayData 가 `NpcData` 직접 참조, Runner 는 `DialogueDatabase` 직접 참조), 타이핑은 `SpeechBubble` 에 내장.

### 사용자 씬/에셋 작업 (구현 후)

1. 컴파일 확인.
2. `Assets/My/Data/Npc/` 에 `NpcData` 에셋 생성 (우클릭 Create > KnockKnock > Npc Data). `id` = CSV 의 `npcId`, 초상화 2종, (선택) `prefab`.
3. `Assets/My/Data/Dialogue/` 에 CSV 작성 (sample.csv 참고) → `Tools > Dialogue > Import CSV → DialogueDatabase`.
4. `Assets/My/Data/Days/` 에 `DayData` 에셋, `day` 와 `eveningGuests` 채우기.
5. 씬: 빈 GO `DialogueRunner`(+`database` 연결), `GuestManager`. 접객 오버레이 캔버스에 `QuestionPanel`·`ApprovalPanel` UI + `DialogueRunner.questionPanel` 연결.
6. 손님 프리팹: `GuestMover` + `Animator`(선택) + 자식에 `SpeechBubble`(World Space Canvas, 머리 오른쪽 위). `SpeechBubble.root` = 그 캔버스, `label`/`portrait` 연결.
7. `ReceptionManager`: `days[]`, `guestSpawn`/`guestPath[]`/`guestStand`/`guestExit`, `fallbackGuestPrefab`, `approvalPanel` 연결.
8. 검증: 저녁 진입 → 손님이 걸어옴 → 말풍선 인사 → 질문 버튼 → 답변 → 대화 종료 → 승인/거절 → 손님 퇴장 → 다음 손님 → 큐 끝나면 새벽. 새벽엔 체류 손님에게 `DialogueRunner.Play(npc, bubble, Dawn)` (트리거는 후속).

---

## 10. 구현 완료 (코드, 2026-08-29)

| 파일 | 내용 |
|---|---|
| `Dialogue/Expression.cs` | enum `{ Neutral, Angry }` |
| `Dialogue/Situation.cs` | enum `{ Reception, Dawn }` |
| `Dialogue/DialogueLine.cs` | struct `DialogueLine{expression,text}` + enum `EntryRole{Greeting,Question,Ambient}` + class `DialogueEntry{npcId,situation,day,role,key,label,lines}` |
| `Dialogue/NpcData.cs` | SO. `id`/`displayName`/초상화 2종/`prefab`/`isSleepwalker`/`visitorOnly`/`IdCard`. `Portrait(Expression)` |
| `Dialogue/DialogueDatabase.cs` | SO. `List<DialogueEntry>` + `Query(npcId,situation,day,role)` 일차 폴백 + `RebuildIndex()` |
| `Dialogue/SpeechBubble.cs` | 컴포넌트. `root` 토글 + `LateUpdate` 빌보드 + 타이핑 내장(`TypeLine`) + `Show(npc,lines)`/`Hide()`. 클릭/E/Space 로 즉시완성·다음줄 |
| `Dialogue/QuestionPanel.cs` | UI. 버튼 생성, `CanAsk`/`OnAsked` 훅, `SetInteractable`, `Open/Close` |
| `Dialogue/DialogueRunner.cs` | 싱글턴. `Play(npc,bubble,situation,onComplete)` 코루틴 → Greeting → QuestionPanel → Answer. 이벤트 3종 |
| `Dialogue/GuestMover.cs` | 컴포넌트. `WalkThrough(waypoints)`/`WalkTo(t)` 직선 이동 + Animator `Walking` bool. ponytail 주석 |
| `Dialogue/Editor/DialogueImporter.cs` | `Tools > Dialogue > Import CSV → DialogueDatabase` (따옴표 파서, `\n` 변환, 미등록 npcId 경고) + `Self-Check Query` (assert) |
| `Game/DayData.cs` | SO. `day`/`eveningGuests`/`nightNews` |
| `Game/GuestManager.cs` | 싱글턴. `enum Verdict`, `GuestState`, `CheckIn`/`SetVerdict`/`CheckOut`/`Get`/`Active` |
| `Game/ApprovalPanel.cs` | UI. `Open(onApprove,onReject)` 2버튼 |
| `Game/ReceptionManager.cs` | **수정**. `days`/스폰 지점/`fallbackGuestPrefab`/`approvalPanel`/`firstRoomNumber`/`enterDelay` 필드. `BeginSession` → 오늘 `DayData` 있으면 `GuestQueue` 코루틴. 큐: 스폰→`WalkThrough`→`DialogueRunner.Play`→`Judge`(ApprovalPanel)→`WalkTo(exit)`→파괴→다음. 큐 끝 `EndSession()`. `StopQueue()` 가 K/ESC 시 코루틴·손님 정리. 기존 API(`Instance`/`InSession`/`OnSessionStarted/Ended`/`EndSession`) + `receptionAnchor`/`debugEndKey` 유지 → 씬 참조 안 깨짐 |
| `Data/Dialogue/sample.csv` | 형식 예시 (guest_01, 접객/새벽, 일차별, 질문) |
| `Docs/DialogueSystem.md` | 신규 레퍼런스 |
| `Docs/ReceptionManager.md` | 손님 큐 반영 |

### 알려진 한계 / 후속
- **새벽 탐문 트리거·행동력 시스템** 미구현 — `DialogueRunner.Play(npc, bubble, Situation.Dawn)` + `QuestionPanel.CanAsk/OnAsked` 훅만 준비됨.
- **신분증 확인(SYS-04)·객실 배치도(SYS-06 풀버전)** 없음 — `ApprovalPanel` 은 승인/거절 + 객실번호 자동증가만.
- **밤 판정·엔딩 집계** 별도 — `GuestState.verdict` vs `NpcData.isSleepwalker` 데이터만.
- **로컬라이제이션** — CSV `text` 단일 열. 나중에 `text_ko`/`text_en` 열 or 로케일 폴더 (임포터만 수정).
- **손님 프리팹** — NPC별 모델은 `NpcData.prefab`, 없으면 공용 `fallbackGuestPrefab`. 60+ 모델 준비 전까진 fallback 하나로.
- **GuestMover** — 웨이포인트 직선. NavMesh 아님.

### 사용자 씬/에셋 작업
9장 "사용자 씬/에셋 작업" 참조. 요약: NpcData 에셋 → CSV 작성 → 임포트 → DayData 에셋 → 씬에 `DialogueRunner`/`GuestManager` GO + `QuestionPanel`/`ApprovalPanel` UI → 손님 프리팹에 `GuestMover`+`SpeechBubble` → `ReceptionManager` 필드 배선 → 저녁 진입 검증.

---

## 11. 수정 (2026-08-29) — 손님 이동 앵커를 GuestMover 인스펙터로

요청: "GuestMover 에서 인스펙터에 직접 앵커를 지정 — 플레이어한테 오는 부분 / 거절당해 나가는 부분 / 모텔방 쪽으로 가는 앵커 라인".

**스폰 방식 폐기 → 씬의 공용 손님 오브젝트 재사용.** 프리팹은 씬 트랜스폼을 참조 못 하므로 앵커 라인을 인스펙터에 직접 두려면 씬 오브젝트여야 함.

| 파일 | 변경 |
|---|---|
| `Dialogue/GuestMover.cs` | 앵커 라인 3종 `Transform[]` 인스펙터 필드: `entryPath`(스폰→접객 자리, `[0]`=시작), `rejectPath`(거절 퇴장), `roomPath`(승인 입실). 메서드 `WalkEntry()`/`WalkReject()`/`WalkToRoom()` + `WarpToStart()`. `WalkTo` 제거, `WalkThrough`(public) 유지 |
| `Game/ReceptionManager.cs` | `guestSpawn`/`guestPath[]`/`guestStand`/`guestExit`/`fallbackGuestPrefab` **제거** → `[SerializeField] GuestMover guest` (씬 오브젝트) 하나. 큐: `guest` 활성 + `WarpToStart()` → `WalkEntry` → 대화 → 승인 시 `CheckIn` + `WalkToRoom` / 거절·방문자 `WalkReject` → `guest` 비활성. `guestBubble` = `guest` 자식 SpeechBubble 캐시. `guest` 없으면 큐 스킵(디버그 K) |
| `Dialogue/NpcData.cs` | `prefab` → `modelPrefab` 로 개명 + "현재 미사용, 모델 스왑용" 명시 (공용 손님 오브젝트 하나 재사용 중) |
| `Docs/DialogueSystem.md`, `Docs/ReceptionManager.md` | 반영 |

에디터: 씬에 손님 오브젝트 1개(GuestMover + Animator + 자식 SpeechBubble 캔버스) → `entryPath`/`rejectPath`/`roomPath` 에 빈 Transform 들 배치(경로점) → `ReceptionManager.guest` 연결. 큐 사이엔 자동 비활성.

## 상태

2026-08-29 구현 완료 (코드) + 손님 이동 앵커 개편. Unity 컴파일 확인 + 에디터 배선/검증 대기.
