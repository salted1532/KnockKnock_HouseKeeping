# 0104 - 대화/NPC 데이터 구조 개선 + 접객 승인·거절 흐름 (제안)

날짜: 2026-08-29
관련: `doc/0102`(대화·NPC 구현), `doc/0100`(화면고정/모니터), `doc/0103`(2D NPC·연출 primitive), `기획/위협의-정체와-판별-설계안.md`(3층 판별)

## 요청 (이번 대화 누적)

1. **DayData**: 일차를 **리스트로 추가**할 수 있게. 접객 손님을 **번호 리스트**로 관리 (번호 입력란도 리스트).
2. **NpcData**: 리스트로 계속 추가. **번호 id (1~60)**, 60명 계획.
3. (참고: NPC→대화 프로필→조건 선택, 노드 그래프, CSV/시트, 로컬라이징, 단일 매니저 — 일반론 자료. "참고만")
4. **ScriptableObject 작동 방식 개선.**
5. 대사 읽고 넘어가는 것 외에 **대사 이후 플레이어 선택지**(분기) 추가.
6. **접객 승인/거절 흐름 재구성**:
   - 승인 = 모니터에서 방 지정 → 그 방 열쇠 줍기 → 숙박객 클릭 → 숙박객이 한마디 하고 주인방을 나감
   - 거절 = 대화에서 "거절" 선택 → 여러 번 더 조르는 손님도 있음 → 반복하면 나감 → 혹은 점프스케어

## 1. 현재 상태 (doc/0102 구현본)

| 조각 | 지금 |
|---|---|
| `NpcData` (SO, NPC당 1개) | `string id`("guest_01"), 이름, 초상화 2종, prefab, `isSleepwalker`/`visitorOnly`/`IdCard` |
| `DialogueDatabase` (SO 1개) | `List<DialogueEntry>` + `Query(npcId, situation, day, role)` 일차 폴백. CSV 임포터가 재생성 |
| `DialogueEntry` | `npcId, situation, day, role(Greeting/Question/Ambient), key, label, lines[]` |
| `DialogueRunner` | `Play(npc, bubble, situation)` → Greeting 재생 → QuestionPanel(질문 나열) → 답변 → 종료. **분기 없음** |
| `QuestionPanel` | `Question` 엔트리 전부를 버튼으로. `CanAsk`/`OnAsked` 훅 |
| `DayData` (SO, 일차당 1개) | `int day`, `List<NpcData> eveningGuests`(직접 참조), `nightNews` |
| `ReceptionManager` | `List<DayData> days`. 저녁 큐: 스폰→`WalkThrough`→`Play`→`Judge`(ApprovalPanel 2버튼)→`CheckIn`/`SetVerdict`→퇴장 |
| `ApprovalPanel` | 승인/거절 버튼 2개 (SYS-05 최소판) |
| `GuestManager` | `GuestState{npc, room, verdict, checkInDay}`, `CheckIn`/`SetVerdict`/`CheckOut` |
| 열쇠 | `PickupEffect`(줍기) + `HookEffect`(고리에 걸기) 이미 있음. 방 번호 개념은 없음 |
| 모니터 | `EnterUIModeEffect`(화면고정) — 앵커 스택으로 접객 위에 중첩 가능. 화면 버튼은 물리 콜라이더+`Interactable` |

## 2. 참고 자료 대조 — 뭘 채택하고 뭘 안 하나

| 자료 항목 | 판단 |
|---|---|
| 데이터 ↔ 실행 로직 분리 | **이미 됨** (`DialogueDatabase` / `DialogueRunner`) |
| 단일 대화 매니저 | **이미 됨** (`DialogueRunner` 싱글턴, NPC는 `Play()` 호출만) |
| CSV / 스프레드시트 원본 | **이미 됨** (`Tools > Dialogue > Import CSV`) |
| id 기반 관리 | **채택 강화** — 문자열 → 숫자 (요청 2) |
| 로컬라이징 준비 | **이미 됨** (CSV `text` 단일 열 → 후속에 `text_ko`/`text_en`) |
| 대사 이후 선택지 | **채택** (요청 5 — 4.C) |
| 조건부 대사 (조건/우선순위) | **훅만** — CSV `condition` 열 파싱만, 평가는 후속. 이 게임 분기는 얕음 |
| 비주얼 노드 그래프 에디터 / Ink / Yarn | **안 함** — doc/0102 §5 재확인. 분기 = "인사 → 선택지 → 목표 노드"뿐, 진짜 부담은 분량(=CSV). 그래프 툴은 학습·의존성 비용만 |
| 별도 `DialogueProfile` 중간 레이어 | **안 함** — `Query(npcId, situation, day, role)` 가 이미 그 역할 |
| 별도 `GameState`/퀘스트 매니저 | **안 함** — 이 게임 상태는 `GuestManager.Verdict` + 후속 `StoryFlags`(도시 붕괴/오판 집계)로 충분. 범용 퀘스트 시스템 아님 |
| 별도 Localization DB | **안 함(지금)** — CSV 열 확장으로 처리 |

## 3. 제안

### A. NpcData — 숫자 id + NpcCatalog

- `NpcData.id` : `string` → **`int` (1~60)**. `OnValidate` 로 범위 경고.
- **에셋은 NPC당 1개 유지.** 스프라이트·프리팹·플래그 참조는 개별 에셋이 맞다. 60명을 한 SO 인라인 리스트에 넣으면 인스펙터 편집이 지옥(스프라이트 슬롯 60×N개 스크롤).
- **신규 `NpcCatalog` (SO 1개)** — "리스트로 추가"의 실체:
  ```csharp
  [CreateAssetMenu(menuName = "KnockKnock/Npc Catalog")]
  public class NpcCatalog : ScriptableObject
  {
      public List<NpcData> npcs = new();          // 인스펙터에서 드래그 추가
      public NpcData Get(int id);                 // Dictionary 캐시
      // 에디터 버튼: "프로젝트의 NpcData 전부 수집" + 중복 id 검사
  }
  ```
  - `DialogueDatabase` 와 `CampaignData` 가 번호를 `NpcData` 로 바꿀 때 단일 창구.
  - doc/0102 가 지운 `NpcDirectory` 의 부활 — 그땐 `DayData` 가 직접 참조라 불필요했지만, 번호 관리로 가면 필요.

### B. DayData → CampaignData (단일 SO, 일차 리스트)

```csharp
[CreateAssetMenu(menuName = "KnockKnock/Campaign Data")]
public class CampaignData : ScriptableObject
{
    public List<DayPlan> days = new();
    public DayPlan Day(int day);                  // 없으면 null

    [Serializable]
    public class DayPlan
    {
        public int day;
        public List<int> eveningGuestIds = new(); // ← 번호 입력란 리스트 (요청 1)
        [TextArea] public string nightNews;
        // 후속: List<int> morningTaskIds ...
    }
}
```

- `ReceptionManager.days (List<DayData>)` → `campaign (CampaignData)` 1개 참조.
- 오늘 손님 = `campaign.Day(DayCount).eveningGuestIds` → `NpcCatalog.Get(id)` → `NpcData`.
- `OnValidate`: `day` 중복/누락, `NpcCatalog` 에 없는 id 경고.
- **기존 `DayData` 에셋 삭제** (지금 0~2개 수준, 수동 재입력 or 5줄짜리 변환 유틸).

### C. 대사 노드 + 선택지 분기 (요청 5)

`DialogueEntry` 를 "노드"로 일반화. **비주얼 그래프 아님 — 문자열 `goto`.**

```csharp
[Serializable]
public class DialogueEntry            // = 노드 하나
{
    public int npcId;
    public Situation situation;
    public int day;                   // 선택자 (0 = 공통 폴백)
    public EntryRole role;            // 진입점 구분 (Query 용)
    public string nodeKey;            // (npcId, situation) 안에서 유일. 기존 key
    public string label;             // 선택지/질문 버튼 문구
    public List<DialogueLine> lines = new();
    public List<Choice> choices = new();   // 대사 후 플레이어 선택 (비면 그냥 끝/다음)
    public string goToNode;          // choices 없을 때 자동으로 이어질 노드 (비면 종료)
    public string condition;         // 후속 훅 — 지금은 파싱만
}

[Serializable] public struct Choice { public string label; public string goToNode; }

public enum EntryRole { Greeting, Question, Ambient, Say }   // Say = 분기 중간 노드
```

**재생 흐름** (`DialogueRunner`, 재귀):
```
PlayNode(node):
    bubble.Show(node.lines)                 // 선형 읽기 (기존)
    if node.choices 있음:
        QuestionPanel.Open(choices)         // 기존 UI 재사용
        선택 → PlayNode(그 goToNode)
    elif node.goToNode 있음:
        PlayNode(node.goToNode)
    else:
        return  // 이 가지 끝
```

- 접객 "질문 목록"(현재 `role==Question` 전부 나열)은 **그대로 유지** = 특수한 선택지 허브. 스크립트 분기는 `choices` 로 아무 노드에나.
- 순환/끊긴 `goto` 는 임포터가 잡음(5).

**CSV 스키마 개정** — 열 2개 추가:
```
npcId, situation, day, role, nodeKey, label, expression, text, goto
```
- `role=Greeting/Question/Say` : `text` 줄들 (+ `goto` 선택).
- **선택지**는 `role=Choice` 행들: 같은 `nodeKey` 여러 행 = 한 선택지 그룹. `label`=버튼, `goto`=목표 노드, `text` 비움. (doc/0102 가 이미 `(npcId,situation,day,role,key)` 로 여러 줄 묶는 방식과 동일)
- `npcId` 는 숫자("1", "23"). 임포터가 `int` 파싱.

### D. 접객 승인/거절 흐름 재구성 (요청 6)

`ApprovalPanel` 2버튼 → **디게틱 흐름**으로 교체.

```
손님 입장 → DialogueRunner.Play(Reception)
   ├─ 대화 중 플레이어가 "거절" 선택지 클릭
   │     → 거절 응답 노드 재생
   │     → 조르는 손님: choices 로 선택지 재노출 (최대 insistCount 회)
   │     → 한도 초과 or 손님이 포기 → Verdict.Rejected → 손님 퇴장
   │     → (특정 손님) 거절 반복 시 goToNode 가 점프스케어 노드
   │
   └─ 대화가 거절 없이 끝남 → 플레이어가 직접:
        1. 모니터 클릭 (화면고정 스택) → RoomBoard 에서 빈 방 클릭 → 현재 손님에게 배정
        2. ESC 로 접객 복귀 → 열쇠꽂이에서 그 방 열쇠가 상호작용 가능해짐 → 줍기(PickupEffect)
        3. 손님 클릭 (CursorInteractor) → CheckInGuestEffect 가 "손에 든 열쇠 == 배정된 방?" 확인
             → 맞으면: 손님 퇴장 대사 1줄 → Verdict.Approved, room=배정 → 손님 퇴장
             → 방 미배정 / 열쇠 없음 / 다른 방 열쇠 → 손님이 넛지 대사 ("아직 방을...")
→ 다음 손님
```

**신규 조각:**

| 조각 | 내용 |
|---|---|
| `RoomData` (SO 리스트 or `MotelData`) | 방 번호, 층/위치, 상태(`Vacant/Assigned/Occupied/NeedsCleaning`). 모텔 전역 설정 (일차 무관) |
| `RoomBoard` (UI, 모니터) | 방 목록 버튼. 클릭 → `AssignRoom(currentGuest, room)`. 배정됨/청소필요 시각 표시 |
| `RoomKey` (컴포넌트) | `int roomNumber`. 씬의 열쇠 오브젝트(이미 `PickupEffect` 있음)에 추가. 배정 전엔 `Interactable.enabled=false` |
| `CheckInGuestEffect : InteractionEffect` | 손님 `Interactable`(줍기 아님, `상호작용`)에 부착. 활성 손 아이템의 `RoomKey.roomNumber` 대조 → 성공 시 `GuestManager.CheckIn` + 퇴장 대사 + 손님에게 `verdict=Approved` 신호 |
| `ReceptionManager` 큐 수정 | `Judge()` 삭제. 대화 후 `WaitUntil(GuestManager.Get(npc).verdict != None)` — 거절 선택 or 열쇠 체크인이 세팅 |

**거절 조르기**: `NpcData.insistCount`(int, 기본 0) — 거절 후 몇 번 더 조르는지. 대사는 CSV 의 `reject`/`insist`/`leave` 노드로 저작 (C 의 `choices`/`goto` 로 자연스럽게 표현). `insistCount=0` 이면 한 번에 나감.

**점프스케어**: 거절 반복의 마지막 `goToNode` 를 스케어 노드로. 스케어 자체는 `doc/0103` 의 `ScriptedEncounter` primitive(화면고정 + 연출 + 페이드) 재사용 — `DialogueLine` 또는 노드에 `onReached` UnityEvent 훅 하나. (별도 doc)

### E. ScriptableObject 작동 방식 개선 (세부)

- `DialogueDatabase`: 기존 `byNpc` 인덱스 + **`nodeKey → DialogueEntry` 딕셔너리** 추가 (goto 재생 시 O(1)). `RebuildIndex` 확장.
- `DialogueDatabase.Query`: 반환을 `IReadOnlyList` 로, 내부 리스트 재사용 (매 호출 `new List` 안 함).
- `NpcData.OnValidate`: `id` 1~60 범위 밖 경고.
- `NpcCatalog`: 에디터 "수집" 버튼 + 중복 id 검사 + null 항목 경고.
- `CampaignData.OnValidate`: `day` 중복/누락, 미등록 `eveningGuestIds` 경고.
- `DialogueImporter`:
  - 새 열(`goto`) + `role=Choice` + 숫자 `npcId` 대응.
  - **끊긴 goto**(대상 `nodeKey` 없음), **순환 참조**, **미등록 npcId** 경고.
  - `Self-Check` 에 분기 케이스 assert 추가.
- `[CreateAssetMenu]` 경로 `KnockKnock/...` 로 통일 (이미 대부분).
- "단일 에셋 + CSV 재생성 + 손편집 금지" 모델은 **유지** (좋은 구조).

## 4. 영향 파일

```
Assets/My/Scripts/Dialogue/
  NpcData.cs              수정  id string→int, insistCount, OnValidate
  NpcCatalog.cs           신규  SO: List<NpcData> + Get(int) + 수집 버튼
  DialogueLine.cs         수정  DialogueEntry: nodeKey/choices/goToNode/condition, EntryRole += Say/Choice, struct Choice
  DialogueDatabase.cs     수정  int 키, nodeKey 인덱스, Query 재사용
  DialogueRunner.cs       수정  재귀 PlayNode + choice 처리
  QuestionPanel.cs        수정  choices 도 받게 (거의 그대로 — label/onPick)
  Editor/DialogueImporter.cs  수정  새 열·Choice·숫자 id·goto 검증
Assets/My/Scripts/Game/
  DayData.cs             삭제  → CampaignData.cs 신규 (List<DayPlan>)
  ReceptionManager.cs    수정  campaign 참조, Judge() 삭제, verdict 대기
  ApprovalPanel.cs       삭제 or 디버그 폴백으로 축소
  GuestManager.cs        수정  약간 — AssignRoom(배정만, verdict 아직 None) 분리
  RoomData.cs / MotelData.cs  신규  방 목록·상태
  RoomBoard.cs           신규  모니터 방배정 UI
Assets/My/Scripts/Interaction/
  RoomKey.cs             신규  컴포넌트 (roomNumber)
  Effects/CheckInGuestEffect.cs  신규  손님 클릭 → 열쇠 대조 → 체크인
Assets/My/Data/
  Npc/Npc_*.asset        수정  id 숫자로
  NpcCatalog.asset       신규
  Campaign.asset         신규  (구 Day*.asset 대체)
  Dialogue/*.csv         수정  헤더 + 숫자 id + goto/Choice 행
Docs/  DialogueSystem.md, ReceptionManager.md 갱신 + RoomBoard/CheckInGuestEffect/NpcCatalog/CampaignData 신규
```

## 5. 확인 필요

1. **NpcData**: 개별 에셋 60개 + `NpcCatalog` (추천) vs 단일 SO 인라인 리스트?
2. **DayData → CampaignData** 단일 SO 교체 OK? 기존 `DayData` 에셋 삭제?
3. **id 타입**: `int`(1, 23) vs 표시용 zero-pad `"001"` 문자열. CSV·인스펙터 일관성 위해 `int` 추천.
4. **선택지 UI**: `QuestionPanel` 재사용(추천) vs 전용 `ChoicePanel`.
5. **선택 결과 저장**: 플레이어가 어떤 선택을 했는지 `GuestState` 에 기록? (나중에 대화 모순·엔딩 집계용) — 지금 훅만 vs 필드 추가.
6. **승인 = 열쇠 흐름 범위**:
   - (a) `RoomBoard` + `RoomKey` + `CheckInGuestEffect` 전부 이번에
   - (b) 이번엔 `choices` 분기 + 거절 흐름만, 승인은 임시로 "손님 클릭 = 자동 다음 빈 방" (열쇠·모니터는 후속 doc)
7. **열쇠 처리**: 손님에게 건네져 인벤토리에서 사라짐 vs 주인 마스터키(꽂이로 돌아옴).
8. **모니터 방배정 접근**: 접객 자리에서 모니터를 커서로 바로 누름 vs 모니터 화면고정으로 스택 진입 후 조작.
9. **거절 조르기**: `NpcData.insistCount` 필드 vs 전부 CSV `goto` 로만 표현(필드 없음).
10. **점프스케어 훅**: 노드에 `onReached` UnityEvent 하나 vs `doc/0103` `ScriptedEncounter` 컴포넌트 먼저 만들고 참조.
11. **RoomData**: SO 리스트 vs 씬 오브젝트(방문마다 `RoomMarker`) 스캔. 방 개수·"방 소진 시" 처리는 이번 범위?
12. **조건부 대사** `condition` 열: 파서 훅만(추천) vs 이번에 평가까지.

## 6. 스킵 (YAGNI — 필요해지면 훅)

- 비주얼 노드 그래프 에디터 / Ink / Yarn.
- 범용 `GameState`/퀘스트 매니저 (참고자료 4·5장).
- `DialogueProfile` 중간 레이어.
- Localization DB 분리 — CSV 열 확장으로.
- 줄별 음성(VO)/애니메이터 트리거 — `DialogueLine` 필드만 예약.
- 밤 판정·엔딩 집계 — `GuestState.verdict` vs `isSleepwalker` 데이터만.
- 객실 청소·상태 순환(SYS-01 아침) 의 풀 구현 — `RoomData.state` 필드만 두고 아침 태스크 시스템에서.

---

## 7. 확정 (2026-08-29, 확인 답변 반영) — 이 섹션이 최종

**범위 = (b) 최소판.** 이번엔 **선택지 시스템 + 거절 흐름**만. 승인은 테스트용으로 "손님 클릭 = 자동 다음 방번호". `RoomBoard`·`RoomKey`·`CheckInGuestEffect`·`RoomData`·모니터 방배정·열쇠 처리는 **전부 후속 doc**.

### 답변 요약
| # | 결정 |
|---|---|
| 1 | NpcData **개별 에셋** 유지 + `NpcCatalog` SO |
| 2 | `DayData` → `CampaignData` 단일 SO 교체, 기존 에셋 삭제 (지금 `Day.asset` 1개뿐) |
| 3 | `id` = **`int`** (1~60) |
| 4 | 선택지 UI = `QuestionPanel` **재사용** (오버로드 추가) |
| 5 | 선택 결과 로깅 = **스킵**. 거절 조르기는 `DialogueRunner` 런타임에서. 훅만 남김 |
| 6 | **(b)** — 위 참조 |
| 7·8·11 | 열쇠·모니터·RoomData = **후속 doc** |
| 9 | `insistCount` 필드 **안 만듦**. 조르기 = CSV `goto` 로 저작 (노드가 선택지로 되돌아옴). 무한루프 방지 런타임 가드(대화당 노드 방문 상한) |
| 10 | 점프스케어 = `DialogueRunner.OnNodeReached(npc, nodeKey)` **이벤트만** 추가. 씬에서 nodeKey→연출 매핑은 나중 (doc/0103 `ScriptedEncounter` 나올 때) |
| 12 | `condition` 열 = **이번엔 아예 안 넣음** (아무것도 안 하는 열은 클러터). 조건이 실제 필요할 때 추가 |

### 최종 데이터 스키마

**`NpcData`** (수정): `string id` → `int id` (1~60). `OnValidate` 범위 경고. (다른 필드 그대로)

**`NpcCatalog`** (신규 SO 1개):
```csharp
[CreateAssetMenu(menuName = "KnockKnock/Npc Catalog", fileName = "NpcCatalog")]
public class NpcCatalog : ScriptableObject
{
    public List<NpcData> npcs = new();
    public NpcData Get(int id);              // Dictionary 캐시, 없으면 null + 경고
#if UNITY_EDITOR
    [ContextMenu("프로젝트의 NpcData 전부 수집")] void CollectAll();   // + 중복 id 검사
#endif
}
```

**`CampaignData`** (신규, `DayData` 대체):
```csharp
[CreateAssetMenu(menuName = "KnockKnock/Campaign Data", fileName = "Campaign")]
public class CampaignData : ScriptableObject
{
    public List<DayPlan> days = new();
    public DayPlan Day(int day);             // 없으면 null

    [Serializable] public class DayPlan
    {
        public int day;
        public List<int> eveningGuestIds = new();
        [TextArea(2,6)] public string nightNews;
    }
}
```
`OnValidate`: `day` 중복 경고.

**`DialogueEntry`** (수정 — 노드화):
```csharp
[Serializable] public class DialogueEntry
{
    public int npcId;
    public Situation situation;
    public int day;                         // 0 = 공통 폴백
    public EntryRole role;                  // Greeting | Question | Node | Ambient
    public string nodeKey;                  // (npcId, situation) 안에서 유일 (구 key)
    public string label;                    // 질문/선택지 버튼 문구
    public List<DialogueLine> lines = new();
    public List<Choice> choices = new();    // 비면 선형
    public string goToNode;                 // choices 없을 때 자동 연결 (비면 이 가지 종료)
    public Verdict outcome;                 // 기본 None. Rejected 면 대화 전체 종료 + onResult(Rejected)
}
[Serializable] public struct Choice { public string label; public string goToNode; }
public enum EntryRole { Greeting, Question, Node, Ambient }
```

- **line vs choice 는 행 모양으로 구분** (별도 role 안 만듦): `text` 채워짐 = 대사 줄 / `label`+`goto` 채워지고 `text` 빔 = 선택지. 한 `nodeKey` 그룹이 [대사 줄들] + [선택지 행들] 을 섞어 가짐.
- `Greeting` = 대화 시작 시 자동. `Question` = `QuestionPanel` 허브에 나열. `Node` = `goto` 로만 도달.

**CSV 스키마** (열 2개 추가):
```
npcId, situation, day, role, nodeKey, label, expression, text, goto, outcome
```
- 대사 줄: `expression`+`text` (+ 마지막 줄에 `goto` 선택, `outcome` 선택).
- 선택지 행: `label`+`goto`, `text` 빔.
- `npcId` 숫자. 헤더 첫 열 `npcId` 로 시작하면 스킵 (기존).

### 최종 재생 흐름 (`DialogueRunner` 재작성)

```
Play(npc, bubble, situation, Action<Verdict> onResult)
  day = DayCount
  Greeting 엔트리들 → PlayNode 순차
  Question 엔트리 있으면 → QuestionPanel.Open(그것들) → "대화 종료" 까지
  onResult(rejected ? Rejected : None)

PlayNode(entry, ref bool rejected):        // 노드 방문 상한(예: 40) 가드
  bubble.Show(entry.lines)
  if entry.outcome == Rejected: rejected = true; return   // 대화 전체 중단
  OnNodeReached(npc, entry.nodeKey)                        // 점프스케어 등 훅
  if entry.choices.Count > 0:
      QuestionPanel.OpenChoices(entry.choices, pick => PlayNode(FindNode(pick.goToNode)))
      선택 대기
  elif !string.IsNullOrEmpty(entry.goToNode):
      PlayNode(FindNode(entry.goToNode))
```

`QuestionPanel`: `OpenChoices(List<Choice>, Action<Choice> onPick)` 오버로드 추가 — 버튼 생성 코드 공유(`BuildButtons`).

### 최종 접객 큐 (`ReceptionManager` 수정)

```
foreach id in campaign.Day(DayCount).eveningGuestIds:
    npc = catalog.Get(id);  if null → 경고, 스킵
    스폰 → WalkThrough(guestPath + guestStand)
    Verdict result = None
    if DialogueRunner & bubble: Play(npc, bubble, Reception, v => result = v); WaitUntil(끝)
    if !npc.visitorOnly:
        if result == Rejected:
            GuestManager.SetVerdict(npc, Rejected, DayNow())
        else:
            // (b) 테스트용: 손님 Interactable 활성 + 프롬프트 "체크인"
            손님 클릭 대기 → GuestManager.CheckIn(npc, nextRoom++, DayNow())
    WalkTo(guestExit) → Destroy → 다음
큐 끝 → EndSession()
```

- `Judge()` / `ApprovalPanel` 삭제. `ApprovalPanel.cs` 삭제.
- `days (List<DayData>)` → `campaign (CampaignData)` + `catalog (NpcCatalog)` 필드.
- 손님 클릭 체크인: 손님 프리팹에 `Collider`(Interaction 레이어) + `Interactable`(상호작용) + 신규 **`CheckInGuestEffect`**(이번엔 열쇠 검사 없이 `ReceptionManager` 에 "이 손님 승인" 통지만). 후속 doc 에서 열쇠 대조 추가.

### SO 작동 방식 개선

- `DialogueDatabase`: `byNpc`(int 키) + **`byNode` (`(npcId,situation) → nodeKey → DialogueEntry`)** 인덱스. `Query` 결과 리스트 재사용(매 호출 new 안 함).
- `DialogueImporter`: 숫자 `npcId`, 새 열, line/choice 행 구분, **끊긴 `goto`·순환·미등록 id 경고**. `Self-Check` 에 분기 assert.
- `[CreateAssetMenu]` 경로 `KnockKnock/` 통일.
- **에셋 위치 정리**: 현재 `Assets/My/Scripts/Dialogue/` 에 잘못 생성된 `Day.asset`·`Npc_.asset`·`DialogueDatabase.asset` → `Assets/My/Data/` 밑으로 (임포터 경로도 이미 `Assets/My/Data/Dialogue/`). [[project_my-folder-output-location]]

### 최종 파일 목록

```
Dialogue/NpcData.cs            수정  id int, OnValidate
Dialogue/NpcCatalog.cs         신규  SO
Dialogue/DialogueLine.cs       수정  DialogueEntry 노드화, Choice, EntryRole += Node
Dialogue/DialogueDatabase.cs   수정  int 키 + byNode 인덱스
Dialogue/DialogueRunner.cs     재작성  재귀 PlayNode + choices + onResult(Verdict) + OnNodeReached
Dialogue/QuestionPanel.cs      수정  OpenChoices 오버로드
Dialogue/Editor/DialogueImporter.cs  수정  숫자 id·새 열·goto 검증
Game/DayData.cs                삭제  → CampaignData.cs 신규
Game/ReceptionManager.cs       수정  campaign+catalog, Judge 삭제, 클릭 체크인
Game/ApprovalPanel.cs          삭제
Interaction/Effects/CheckInGuestEffect.cs  신규  손님 클릭 → ReceptionManager 승인 통지
Data/  Npc_*.asset(id 숫자), NpcCatalog.asset, Campaign.asset, Dialogue/*.csv(개정)
Docs/  DialogueSystem.md·ReceptionManager.md 갱신 + NpcCatalog·CampaignData·CheckInGuestEffect 신규
```

### 사용자 씬/에셋 작업 (구현 후)
1. 컴파일 확인.
2. `Assets/My/Data/Npc/` 에 `NpcData` 에셋, `id` 1,2,3… 부여. 잘못된 위치의 구 에셋 정리.
3. `NpcCatalog.asset` 생성 → "전부 수집" 버튼.
4. `Campaign.asset` 생성 → `days` 에 일차별 `eveningGuestIds` (번호) 입력.
5. CSV 개정(숫자 id + `goto`/`outcome` 열) → `Tools > Dialogue > Import CSV`.
6. `DialogueRunner` 에 `database`·`questionPanel`, `ReceptionManager` 에 `campaign`·`catalog` 배선.
7. 손님 프리팹에 `Interactable`(상호작용) + `CheckInGuestEffect` + Collider(Interaction 레이어).
8. 검증: 저녁 진입 → 손님 → 인사 → 질문/선택지 → "거절" 고르면 조르기 후 퇴장(Rejected) / 대화 끝내고 손님 클릭하면 체크인(Approved, 방번호 자동) → 다음.

---

## 8. 구현 완료 (코드, 2026-08-29)

| 파일 | 내용 |
|---|---|
| `Dialogue/NpcData.cs` | `id` `string`→`int`. `OnValidate` 1~60 범위 경고. (`modelPrefab` 은 사용자가 이미 rename 해둔 것 유지) |
| `Dialogue/NpcCatalog.cs` | 신규 SO. `List<NpcData>` + `Get(int)` Dictionary 캐시 + 중복 경고. 에디터 `[ContextMenu]` "프로젝트의 NpcData 전부 수집" |
| `Dialogue/DialogueLine.cs` | `DialogueEntry` 노드화: `nodeKey`(구 `key`), `choices(List<Choice>)`, `goToNode`, `outcome(Verdict)`. `struct Choice{label, goToNode}`. `EntryRole += Node` |
| `Dialogue/DialogueDatabase.cs` | `byNpc` 키 `(int, Situation)`. 신규 `byNode` 인덱스 + `GetNode(npcId, situation, nodeKey, day)` (day 폴백). `Query` int 시그니처 |
| `Dialogue/DialogueRunner.cs` | 재작성. `Play(..., Action<Verdict> onResult)`. `Run`→Greeting `PlayNode` 들 → `QuestionHub`(while 루프, Show/-1 종료) . `PlayNode` 재귀: lines→`OnNodeReached`→`outcome==Rejected` 종료→`choices`(패널) or `goToNode`(자동)→`GoTo`. `maxNodeVisits`(40) 순환 가드. 신규 이벤트 `OnNodeReached(npc, nodeKey)` |
| `Dialogue/QuestionPanel.cs` | `Open`/`OpenChoices` → 단일 `Show(labels, onPick(int), showDone)`. `onPick(-1)` = 대화 종료. 허브·선택지 공용 |
| `Dialogue/Editor/DialogueImporter.cs` | 10열(`+goto,outcome`), 숫자 `npcId` 파싱, line/choice 행 구분(text 유무), `ValidateGotos`(끊긴 goto 경고), Self-Check 에 `GetNode`/`outcome` assert |
| `Game/DayData.cs` | **삭제** → `Game/CampaignData.cs` 신규 (`List<DayPlan>{day, List<int> eveningGuestIds, nightNews}`, `Day(int)`, `OnValidate` day 중복) |
| `Game/ApprovalPanel.cs` | **삭제** |
| `Game/ReceptionManager.cs` | `days/approvalPanel` → `campaign(CampaignData)` + `catalog(NpcCatalog)`. `Judge()` 삭제. 큐: id→`catalog.Get`→대화→거절이면 `WalkReject` / 아니면 `AwaitingCheckIn=true` → 클릭 대기 → `CheckIn`+`WalkToRoom`. 신규 `AwaitingCheckIn` 프로퍼티 + `ConfirmCheckIn()` |
| `Interaction/Effects/CheckInGuestEffect.cs` | 신규 `InteractionEffect`. 손님 클릭 → `AwaitingCheckIn` 이면 `ReceptionManager.ConfirmCheckIn()` |
| `Data/Dialogue/sample.csv` | 개정: 숫자 id, `goto`/`outcome` 열, 거절 분기 예시(`reject`/`reject_insist`/`reject_final`) |
| `Assets/My/Scripts/Dialogue/Day.asset` | 삭제 (구 DayData 인스턴스) |
| Docs | `DialogueSystem.md`·`ReceptionManager.md`·`Overview.md` 갱신 |

### 미처리 / 사용자 작업
- **`Assets/My/Scripts/Dialogue/Npc_.asset`·`DialogueDatabase.asset`** — 잘못된 위치. 사용자가 `Assets/My/Data/` 로 옮기거나 삭제 후 재생성. (importer 는 `Data/Dialogue/` 에 씀)
- `.meta` 3개(`NpcCatalog`/`CampaignData`/`CheckInGuestEffect`) 는 Unity 가 임포트 시 생성.
- 씬 배선: `ReceptionManager` 의 `campaign`/`catalog`, `DialogueRunner` 는 그대로. 구 `ApprovalPanel` 오브젝트는 missing script → 제거.
- 9장 "사용자 씬/에셋 작업" 참조. NpcData id 숫자 부여 → `NpcCatalog` 수집 → `Campaign.asset` 편성 → CSV 재임포트.

### 점프스케어 훅
`DialogueRunner.OnNodeReached(npc, nodeKey)` 만 추가됨. 씬에서 특정 nodeKey(예: `reject_final`) 구독 → 연출 발동하는 컴포넌트는 doc/0103 `ScriptedEncounter` 때 만든다.

### 8b. 손님 = 프리팹 인스턴스 재활용 (추가 요청, 2026-08-29)

> 요청: 접객 시작 시 일차 NPC 순서대로 스폰, 1마리씩 (대화→퇴장 완료 후) 다음. 재활용이면 스프라이트 교체. "Guest" 프리팹 만들었으니 연결.

| 파일 | 내용 |
|---|---|
| `Dialogue/GuestMover.cs` | 씬 참조 필드(`entryPath`/`rejectPath`/`roomPath`) + `WalkEntry/WalkReject/WalkToRoom/WarpToStart` **제거** — 프리팹이라 씬 참조 못 가짐. `WarpTo(Transform)` + `WalkThrough(list)` 만 유지. 경로는 `ReceptionManager` 가 넘김 |
| `Dialogue/GuestView.cs` | 신규. `body(SpriteRenderer)` 를 `NpcData` 초상화로 교체. `Apply(npc)`(Neutral + `OnLineShown` 구독) / `Clear()`. 대화 중 줄별 표정 반영 |
| `Game/ReceptionManager.cs` | `guest(GuestMover 씬 참조)` → `guestPrefab(GameObject)` + `guestSpawn`/`entryPath`/`exitPath`/`roomPath`(씬 Transform). `GuestQueue`: 세션 시작 시 `Instantiate` 1개 → 손님마다 `view.Apply`+`WarpTo`+`WalkThrough(entryPath)` → 대화 → 거절/방문객 `exitPath` / 승인 `roomPath` → `view.Clear()` → 다음. 큐 끝 `Destroy` |
| `Docs/DialogueSystem.md`·`ReceptionManager.md`·`Overview.md` | 갱신 |

**씬 작업**: Guest 프리팹 = `GuestMover` + `GuestView`(body=SpriteRenderer) + 자식 `SpeechBubble` + `Interactable`(상호작용) + `CheckInGuestEffect` + Collider(Interaction 레이어). `ReceptionManager` 에 프리팹 + `guestSpawn` + `entryPath`/`exitPath`/`roomPath`(웨이포인트 빈 GO) 연결.

## 상태

2026-08-29 구현 완료 (코드 + 8b). Unity 컴파일 확인 + 에디터 배선/검증 대기.
