# 0105 - 접객 수직 슬라이스 배선 (test_npc 1명 실동작)

날짜: 2026-08-29
관련: `doc/0104`(대화/NPC/접객 흐름 — 코드 구현 완료본), `doc/0102`

## 요청

> 접객 테이블 상호작용 → 저녁 전환 + 접객 모드 진입 → test_npc(id=0) 손님 스폰 →
> Entry Path 로 진입 → 도착 시 Dialogue_Panel 대사 출력 → 거절 버튼 누르면 거절 대사 +
> Exit Path 퇴장 후 삭제/재활용 → npc 클릭(=승인 판정) 시 Room Path 이동 후 삭제.
> 대사는 Claude 가 정해도 됨.

## 조사 결과 — 코드는 이미 다 있음 (doc/0104 §8·§8b)

`ReceptionManager.GuestQueue` 가 요청한 흐름을 그대로 수행한다:
스폰 → `WarpTo(guestSpawn)` → `WalkThrough(entryPath)` → `DialogueRunner.Play(Reception)` →
`visitorOnly`/`Rejected` 면 `exitPath` / 아니면 `AwaitingCheckIn` → 손님 클릭(`CheckInGuestEffect`)
→ `CheckIn` + `WalkThrough(roomPath)` → 다음 → 큐 끝 `EndSession()` → 새벽.

**막힌 건 전부 씬/에셋 배선 + 대사 데이터.** 신규 로직 없음.

| 조각 | 상태 (조사 시점) |
|---|---|
| 큐 로직 / 대화 재귀 / 거절 분기 / 클릭 체크인 | ✅ 코드 완성 |
| `Npc_.asset`(test_npc) · `NpcCatalog` · `Campaign`(day1) | ✅ 존재 |
| `DialogueRunner` + `QuestionPanel` 씬 오브젝트 | ✅ (`Dialogue_Panel`) |
| `ReceptionManager` **2개** (GameManager + 독립 오브젝트) | ⚠️ 싱글턴 충돌 |
| 배선된 `ReceptionManager` 의 guestPrefab/guestSpawn/entry·exit·roomPath/receptionAnchor | ❌ 전부 빔 |
| Guest = 씬 오브젝트, `GuestView`·자식 `SpeechBubble` 없음 | ❌ SpeechBubble 없으면 대사 통째 스킵 |
| `DialogueRunner.database` → 낡은 `Scripts/Dialogue/DialogueDatabase.asset` | ⚠️ 임포터는 `Data/Dialogue/` 에 씀 |
| npcId 대사 | sample.csv 는 npcId **1** (완결된 거절 분기 포함) |
| 접객 테이블 → 저녁 전환 상호작용 | ❌ 씬에 PhaseSwitchEffect/EnterUIModeEffect 없음 |

## 결정 (사용자 확인, 2026-08-29)

| # | 결정 |
|---|---|
| test_npc id | **0 → 1 로 변경** (규칙 1~60 유지). sample.csv 가 이미 npcId 1 |
| 중복 ReceptionManager | **GameManager 로 합치기** — 독립 "ReceptionManager" 오브젝트 삭제 |
| 씬 배선 | 체크리스트 → 사용자가 에디터에서 |
| 저녁 진입 | 이번엔 **디버그 N 키** (테이블 상호작용은 다음 doc) |

## Claude 가 한 것 (코드/에셋 파일)

| 파일 | 변경 |
|---|---|
| `Assets/My/Scripts/Dialogue/NPC_Data/Npc_.asset` | `id: 0` → `id: 1` |
| `Assets/My/Scripts/Dialogue/Campaign.asset` | `eveningGuestIds` `[0]` → `[1]` (`00000000`→`01000000`) |
| `Assets/Scenes/InGame.unity` | `DialogueRunner.database` 를 `Data/Dialogue/DialogueDatabase.asset`(guid 5d25ff99…) 로 repoint |
| `Assets/My/Scripts/Dialogue/DialogueDatabase.asset`(+.meta) | **삭제** (중복 — 임포터 대상이 아닌 낡은 사본, 아무도 참조 안 함) |

대사는 기존 `Assets/My/Data/Dialogue/sample.csv`(npcId 1) 를 그대로 사용:
인사 2줄 → 질문 허브 `[이름 / 방문 목적 / 거절한다 / 대화 종료]` →
"거절한다" → 조르기(`reject_insist`, 화남) → `[다시 거절 → reject_final(outcome=Rejected) / 생각해보겠다 → 허브 복귀]`.
**"거절 버튼" = 질문 허브의 "거절한다" 항목** (항상 보이는 별도 버튼 아님).

## 사용자 체크리스트 (Unity 에디터)

### A. 중복 ReceptionManager 정리
1. Hierarchy 의 독립 **`ReceptionManager`** 오브젝트 선택 → 인스펙터에서 `campaign`(Campaign), `catalog`(NpcCatalog) 참조 확인/메모.
2. **`GameManager`** 의 `ReceptionManager` 컴포넌트에 그 `campaign`/`catalog` 드래그.
3. 독립 `ReceptionManager` 오브젝트 **삭제**. (이후 배선은 전부 GameManager 의 것에)

### B. Guest → 프리팹
씬 `Guest` 오브젝트에:
1. **`GuestView`** 추가. 자식으로 스프라이트용 오브젝트(Quad 또는 SpriteRenderer) 하나 만들고 `body` 에 그 SpriteRenderer 연결. (카메라 향하게 회전, 캡슐 메시는 유지하든 지우든)
2. 자식 **`SpeechBubble`** 캔버스:
   - 빈 자식 GO → `Canvas`(Render Mode = **World Space**) + `TMP_Text` 자식
   - `SpeechBubble` 컴포넌트 추가: `root` = 그 캔버스 GO, `label` = TMP_Text, `billboardTarget` 비우면 root 사용, `faceCamera` 비우면 Camera.main
   - 위치는 머리 오른쪽 위, 스케일 0.01 근처
3. `Interactable` 이 이미 있음(+ `CheckInGuestEffect`). 오브젝트 레이어를 **Interaction** 으로. (선택) `Outline` 컴포넌트.
4. `Guest` 를 `Assets/My/` 아래로 드래그해 **프리팹**화 → 씬의 `Guest` 인스턴스는 **삭제** (큐가 세션마다 1개 Instantiate 해서 재활용).

### C. 경로 오브젝트 (빈 GO)
- `Guest_Spawn` — 손님이 나타날/리셋될 위치 (문 밖 등)
- `EntryPath` 아래 웨이포인트 몇 개 (스폰 → 카운터 앞). `Guest_Pos1~4` 재활용 가능
- `ExitPath` — 카운터 → 밖 (거절·방문객)
- `RoomPath` — 카운터 → 방 쪽

### D. GameManager ▸ ReceptionManager 필드
- `receptionAnchor` = 접객 테이블의 Player 착석 앵커 Transform
- `guestPrefab` = B 에서 만든 Guest 프리팹
- `guestSpawn` = `Guest_Spawn`
- `entryPath` = EntryPath 웨이포인트들 (순서대로)
- `exitPath` = ExitPath 웨이포인트들
- `roomPath` = RoomPath 웨이포인트들

### E. 대사 임포트
`Tools > Dialogue > Import CSV → DialogueDatabase` 실행. 콘솔에 "goto 검사 통과", "NpcData 없는 npcId" 경고 없어야 정상.

### F. 검증
1. 플레이 → **N 3번** → 저녁 진입 (착석 연출).
2. `enterDelay`(0.6s) 후 Guest 가 `guestSpawn` 에서 나타나 `entryPath` 로 걸어옴.
3. 도착 → 말풍선 인사 2줄 (클릭/E/Space 로 넘김).
4. `QuestionPanel` 에 `이름 / 방문 목적 / 거절한다 / 대화 종료`.
   - **"거절한다"** → 화남 대사 → `다시 거절` 선택 → 거절 대사 → Guest 가 `exitPath` 로 퇴장 → (손님 없으면) 새벽.
   - **"대화 종료"** → 패널 닫힘 → **Guest 클릭** → `roomPath` 로 이동 → 새벽.
5. 콘솔에 `bubble 없음`·`catalog 없음`·`DialogueRunner 없음` 경고 없어야 함.

## 알려진 한계 / 다음 doc

- 접객 **테이블 상호작용 → 저녁 전환**: 테이블에 `Interactable` + `PhaseSwitchEffect(from=Noon, to=Evening)` + Interaction 레이어 콜라이더. (이번엔 N 키로 대체)
- 승인 = "손님 클릭 = 다음 방번호 자동" (열쇠·모니터 방배정 없음 — doc/0104 §7 (b))
- Guest 겉모습: `GuestView` 스프라이트 교체만. 3D 모델 스왑은 `NpcData.modelPrefab` 예약됨
- 잘못된 위치 에셋(`Scripts/Dialogue/` 의 `Campaign.asset`·`NpcCatalog.asset`·`NPC_Data/`) → `Assets/My/Data/` 로 이동 권장 (guid 유지되므로 참조 안 깨짐). 선택 사항

---

## 8. uloop 실플레이 테스트 (2026-08-29)

사용자가 씬 배선(Guest 프리팹·경로·필드) 완료 후 `대사는 Dialogue_Panel 로` 결정 →
`SpeechBubble` 을 월드 말풍선이 아닌 스크린 패널로 쓰도록 코드/배선 수정하고 uloop 로 저녁 진입~거절/승인까지 구동 검증.

### 추가 코드 변경
| 파일 | 변경 |
|---|---|
| `Dialogue/SpeechBubble.cs` | `[SerializeField] bool billboard = true` 추가. false 면 `LateUpdate` 회전 스킵 → 스크린 스페이스 패널로 사용 가능 |
| `Game/ReceptionManager.cs` | `[SerializeField] SpeechBubble speechBubble` 추가. 있으면 그걸, 없으면 기존대로 손님 프리팹 자식에서 탐색 |
| `Dialogue/QuestionPanel.cs` | `Show()` 에서 `buttonPrefab` 이 씬 오브젝트(레이아웃 안 템플릿 버튼)면 원본 비활성 + 복제본 `SetActive(true)`. 프로젝트 프리팹/씬 템플릿 둘 다 지원 |
| `Data/Dialogue/sample.csv` | Reception 질문을 `거절한다` 하나로 축소 (버튼 UI 2칸에 맞춤). name/purpose 행 삭제 |

### 씬 배선 (uloop 로 적용, 저장됨)
- `DialogueRunner` : `Dialogue_Panel` → **`GameManager`** 로 이동. (패널이 비활성이라 Awake 미실행 → 싱글턴 등록 안 되던 문제)
- `SpeechBubble` : **`GameManager`** 에 추가. `root=Dialogue_Panel`, `label=Dialogue_Text`, `billboard=false`. (컴포넌트가 자기 root 를 Awake 에서 끄는 자기충돌 방지 위해 패널 밖에 둠)
- `QuestionPanel.root` : `Dialogue_Panel` → **`Button_Horizontal`** (버튼 줄만 토글, 패널 전체는 SpeechBubble 이 토글)
- `ReceptionManager.speechBubble` = GameManager 의 SpeechBubble
- `Dialogue_Panel`·`Button_Horizontal` 시작 시 비활성
- 중복 `ReceptionManager` 는 사용자가 이미 GameManager 것으로 정리함

### 한글 폰트
프로젝트에 한글 폰트 없음 → 대사가 □□□ 로 렌더됨. `C:/Windows/Fonts/malgun.ttf` → `Assets/My/Fonts/malgun.ttf` 복사, `TMP_FontAsset.CreateFontAsset` **Dynamic** 모드로 `Assets/My/Fonts/Malgun SDF.asset` 생성. TMP 전역 폴백에 추가 + `Dialogue_Text`/버튼 TMP 에 직접 지정.

### 검증 결과 — 로직 전부 통과
| 단계 | 결과 |
|---|---|
| 저녁 진입 → `BeginSession` / `InSession=true` | ✅ |
| Guest 스폰 + `entryPath`(Pos1→2→3) 보행 | ✅ |
| `GuestView.Apply` 초상화 교체 (`접객_0`) | ✅ |
| `DialogueRunner.Running`, `Dialogue_Text` 에 대사 출력 (한글) | ✅ |
| `QuestionPanel` 허브 + 선택지 (`거절한다`→`reject_insist`→선택지→`reject_final`) | ✅ |
| **거절**: `GuestManager` `verdict=Rejected` → 퇴장 → 손님 파괴 → `EndSession` → 새벽 | ✅ |
| **승인**: 대화 종료 → 손님 클릭(`CheckInGuestEffect`) → `verdict=Approved, room=101` → `roomPath` 이동 → 파괴 → 새벽 | ✅ |

### 남은 것 — 전부 비주얼/레이아웃 (사용자 에디터 작업)
- **`Dialogue_Panel` 이 화면에 거의 안 보임**: `Dialogue_Text` RectTransform 이 200×50 뿐 (fontSize 36, 한글 20자 → 박스 밖). `Dialogue_Panel` anchoredPos (-450,-450). → 텍스트를 부모(`Dialogue`)에 스트레치 + word wrap + auto-size, 패널을 화면 안 위치로.
- **Guest 스프라이트가 큰 흰 사각형**: 프리팹 자식 `Square` SpriteRenderer scale (1.5,2), 스프라이트 임포트/알파 확인. 초상화(`접객_0`)가 실제로 들어가는지, 크기 조정.
- `ReceptionManager.receptionAnchor` 미할당 → 플레이어가 접객 자리로 안 앉음. 접객 테이블의 Player 앵커 Transform 연결.
- done 버튼(`Dialogue_Button2`) 텍스트가 "Exit" → "대화 종료" 등으로.
- `Application.runInBackground` 는 uloop 로 저녁 전환(ScreenFader 페이드)이 에디터 비포커스 시 멈춰서 런타임에 켰던 것 — 원하면 Project Settings > Player 에 영구 설정.

---

## 9. 비주얼 정리 (2026-08-30)

사용자 결정: **대사는 영어로** (한글 폰트 안 보임), Guest 스프라이트 크기 조정.

| 변경 | 내용 |
|---|---|
| `Data/Dialogue/sample.csv` | 전부 영어로 재작성 + 재임포트. 거절 분기 구조·nodeKey 동일 |
| 한글 폰트 제거 | `Assets/My/Fonts/`(malgun.ttf + Malgun SDF) 삭제, TMP 전역 폴백에서 제거, `Dialogue_Text`/버튼 → TMP 기본 폰트 |
| `Dialogue_Panel` 레이아웃 | 화면 하단중앙 앵커, 900×300, 배경 어둡게(0.06,0.06,0.07,0.92) |
| `Dialogue_Text` | 부모(`Dialogue`)에 스트레치(offset 22/16) + `enableWordWrapping` + `enableAutoSizing`(16~34) + TopLeft |
| `Button_Horizontal` | 패널 하단 스트립(anchor 0~1 / 0~0.32) |
| `Dialogue/GuestView.cs` | `[SerializeField] float worldHeight = 1.9` 추가. `SetExpression` 에서 `body.transform.localScale = worldHeight / sprite.bounds.size.y`. 초상화 원본 해상도 제각각(896×1200 / 765×1024 …)이어도 균일 높이. **SpriteRenderer 엔 크기 필드가 없음 → Transform 스케일 or 스프라이트 PPU 로 조정하는데, 이 코드가 자동으로 함** |
| 씬 정리 | 프리팹 안 만들고 남아 있던 `Guest` 씬 인스턴스(-13.1,1,15.5) 삭제 — 이게 화면의 큰 흰 사각형이었음. 큐는 `guestPrefab` 을 세션마다 Instantiate |

### 재검증 (uloop)
- Dialogue_Panel 하단중앙에 영어 대사 정상 렌더 ✅
- `Guest(Clone)` 스프라이트 스케일 (0.16,0.16) → 월드 높이 1.90 (기존 12.0) ✅
- 허브 버튼 `[Turn them away][Exit]`, 거절 분기 진입 ✅

### 여전히 사용자 몫
- `ReceptionManager.receptionAnchor` — 접객 테이블 Player 앵커. 없으면 플레이어가 손님 쪽을 안 봄
- 버튼 비주얼(`Dialogue_Button2` 텍스트 "Exit" → "대화 종료" 등, 크기/색)
- Guest 초상화 아트 자체 (`접객_0` 스프라이트)
- `Application.runInBackground` (uloop 테스트용으로 런타임에 켰음 — 영구화하려면 Project Settings)

---

## 10. 손님 5종 + 승인 대사 + 이름표 (2026-08-30)

사용자 요청: 스프라이트 조금 크게 / 손님 클릭 승인 시 "고맙다" 대사 후 방으로 / 손님 5종 (스프라이트는 공용, `npc_name` 라벨로 id 구별) / 5명 대사 다양하게 + 선택지 여러 개 / 손님 순차 진행 확인.

### 코드
| 파일 | 변경 |
|---|---|
| `Dialogue/GuestView.cs` | `worldHeight` 1.9 → **2.3** |
| `Dialogue/SpeechBubble.cs` | `[SerializeField] TMP_Text nameLabel` 추가. `Show()` 에서 `nameLabel.text = npc.displayName` |
| `Dialogue/DialogueRunner.cs` | `public void SayNode(npc, bubble, situation, nodeKey, onDone)` 추가 — 한 노드 대사 줄만 재생(허브·분기 없음) |
| `Game/ReceptionManager.cs` | 승인 흐름: `CheckIn` 직후 `DialogueRunner.SayNode(npc, bubble, Reception, "checkin", ...)` 로 고맙다 대사 → 완료 후 `roomPath` |

### 에셋
- `NPC_Data/Npc_2~5.asset` 신규 (id 2~5, displayName `test2`~`test5`, 초상화는 test1 과 공용)
- `NpcCatalog.asset` — 5개 등록
- `Campaign.asset` day1 `eveningGuestIds` = [1,2,3,4,5]
- `Data/Dialogue/sample.csv` — npcId 1~5, 각기 다른 성격/대사 (Weary Traveler / Nervous Salesman / Rude Regular / Quiet Woman / Talkative Old Man). 선택지 노드: reject 조르기(2택), 질문 내 분기(2~3택, test5 는 trip 3택 + need 3택). 각 손님 `checkin` 노드(승인 대사). 74행 → 49노드, goto 검증 통과
- 씬: `Dialogue_Panel/Dialogue/npc_name` (TMP) 생성 + `SpeechBubble.nameLabel` 연결

### 검증 (uloop)
- 5명 순차: npc1 승인(room 101) → roomPath → **npc2 스폰 → entryPath** → 102 → … → npc5 → 105 → 세션 종료 → Dawn ✅
- `GuestManager` verdict/room: npc1~5 = Approved / 101~105 ✅
- 각 손님 그리팅·`checkin` 대사 개별 확인 (test1: "Thank you. You won't hear a sound from me." / test5: "Bless you son. Sleep well now." …) ✅
- `npc_name` 라벨이 손님마다 test1~test5 로 갱신 ✅
- 스프라이트 스케일: 클론 worldHeight 2.30 ✅
- (대사 입력 넘김은 uloop 로 자동화가 안 돼서 — 에디터 비포커스 시 InputSystem 이 이벤트 무시 — 선택지 클릭 흐름은 이전 세션에서 사용자 클릭으로 검증됨)

### 여전히 사용자 몫
- `ReceptionManager.receptionAnchor` (플레이어 착석)
- 버튼 5개까지 늘 수 있으니(test5) `Button_Horizontal` 폭·`Dialogue_Button` 스타일 조정
- `Dialogue_Button2`("Exit") → "대화 종료"
- 손님별 실제 스프라이트 아트 (지금은 전부 `접객_0` 공용)

---

## 11. 체크인 프롬프트 (2026-08-30)

승인 트리거 = 대화 끝(허브 "Exit") → `AwaitingCheckIn` → **손님 클릭**. 대화 중엔 클릭해도 무시되고 아무 표시도 없어서 헷갈림 → 조건부 프롬프트 추가.

| 파일 | 변경 |
|---|---|
| `Interaction/Conditions/AwaitingCheckInCondition.cs` | 신규 `InteractionCondition`. `IsMet = ReceptionManager.Instance.AwaitingCheckIn`. 손님 프리팹 Interactable 에 부착 → 체크인 대기 중이 아니면 `CanInteract=false` (아웃라인·프롬프트·클릭 전부 막힘) |
| `Interaction/Drivers/CursorInteractor.cs` | `promptRoot`(GameObject) + `promptLabel`(TMP_Text) 필드 추가. 호버 대상 바뀔 때 `promptRoot` 토글 + `promptLabel.text = hovered.Prompt`. `ClearOutline` → `ClearHover` 로 확장 |
| Guest 프리팹 | `AwaitingCheckInCondition` + `Outline`(OutlineVisible, 기본 off) 추가. `Interactable.promptType` = `직접입력`, `customPrompt` = "Check in" |
| 씬 | `Canvas/Cursor_Prompt` (상단중앙 라벨 "Check in") 생성 + `CursorInteractor.promptRoot/promptLabel` 연결 |

`InteractionPrompt` enum 에 `체크인` 값 추가는 **안 함** (한글 폰트 없음 → `직접입력`+영문). 커서 모드엔 원래 프롬프트 UI 가 없었어서(아웃라인만) 이번에 추가.

### 검증 (uloop)
- 손님 보행/대화 중: `conditions=[AwaitingCheckInCondition]`, `CanInteract=False` → 아웃라인·프롬프트 안 뜸 ✅
- `AwaitingCheckIn=True` (대화 끝): `CanInteract=True`, `Prompt="Check in"`, Outline 존재 → 클릭 시 승인 ✅
- 승인 후 방으로 걸어가는 동안: 다시 `CanInteract=False` (재클릭 방지) ✅

---

## 12. 손님이 클릭 안 되던 원인 (2026-08-30)

증상: 대화 끝(Exit) 후에도 손님에 마우스 올려도 아웃라인 안 뜨고 클릭 무반응.

**원인 2개:**
1. **`Player_Anchor` 가 위를 보고 있었음** — euler (270,0,0), 손님과 94° 어긋남. 접객 진입 시 플레이어가 손님 반대쪽을 봄 → 커서 레이가 손님을 못 맞힘 (`AwaitingCheckIn`·`CanInteract` 는 정상이었음).
2. **CapsuleCollider 가 바닥에 있었음** — center (0,0,0). 사용자가 스프라이트 자식(`Square`)을 localPos.y=2 로 올려놔서, 보이는 스프라이트를 클릭해도 콜라이더(발밑)를 빗나감.

**수정:**
| 대상 | 변경 |
|---|---|
| 씬 `Player_Anchor` | pos → `(-28.48, 0.15, -3.42)`, 회전 → 손님(+Z) 바라보게 `LookRotation`. 손님이 화면 중앙에서 9° 안에 들어옴 |
| Guest 프리팹 `CapsuleCollider` | center `(0, 2, 0)`, height 2.4, radius 0.6 — 올려둔 스프라이트에 맞춤 |

**검증 (uloop):** 착석 → 손님 화면상 위치 (639,203) 에 보임 → 그 지점 레이캐스트 → `Guest(Clone)` 히트, `CanInteract=True` → `Interact()` → `npc1=Approved/101` ✅

> `Player_Anchor` 위치는 대충 맞춘 값이라 실제 접객 데스크에 맞게 미세조정 필요. 손님이 서는 지점(`entryPath` 마지막 웨이포인트, 현재 `(-28.5, 0, 0.6)`)과 앵커가 마주보게.
> 콜라이더 center.y 는 프리팹의 스프라이트 자식 localPos.y 와 맞춰야 함 — 스프라이트 위치 바꾸면 콜라이더도 같이.

---

## 13. 포커스 복귀 시 커서 사라짐 (2026-08-30)

증상: 에디터에서 게임뷰 밖으로 나갔다 오면 접객 모드에서 마우스 커서가 사라져 클릭 불가.

**원인:** `StarterAssetsInputs.OnApplicationFocus(bool)` → `SetCursorState(cursorLocked)` — 창 포커스 복귀 시 커서를 다시 잠근다(`Locked` + 숨김). 이 Unity 메시지는 컴포넌트가 `enabled=false` 여도(UI 모드가 FPC 를 끈 상태) 활성 GameObject 면 계속 발생.

**수정:** `Interaction/Modes/UIInteractionMode.cs` — `Update()` 에서 `Active` 인 동안 매 프레임 `Cursor.lockState != None` 이면 다시 풀어줌 (4줄). 메시지 순서와 무관하게 확실.

**검증 (uloop):** 접객 모드 진입(`UIMode.Active=True, lockState=None`) → 강제로 `Cursor.lockState=Locked` → 다음 프레임 `Update` 가 `None` 으로 복구 ✅

(참고: `InteractionPrompt` enum 은 이 시점에 사용자가 한글→영문으로 rename 함. Guest 프리팹 promptType 인덱스 9 = 구 `직접입력` → 신 `Custom`, `customPrompt="Check in"` 그대로 동작.)

## 상태

2026-08-30 — 손님 5종 + 승인 대사 + 이름표 + 체크인 프롬프트 + 클릭 히트 + 포커스 커서 수정 완료. 접객 흐름 + 클릭 승인 + 커서 유지까지 uloop 로 검증됨. 남은 건 Player_Anchor 미세조정·버튼 스타일·손님 아트.
