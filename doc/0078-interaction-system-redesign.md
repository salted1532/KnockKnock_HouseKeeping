# 0078 - 상호작용 시스템 개편 (제안)

## 요청
- 지금처럼 `InteractionType` enum + 거대한 switch에 케이스를 하나씩 추가하는 방식이 안 좋다.
- 재사용 가능한 조각(효과)들을 조합해서 상호작용을 구성하고 싶다:
  - 상호작용 → 소리 / 물체 변경 / 오브젝트 생성 / 밀기 / 열기 / 획득 ...
  - 모든 상호작용은 효과음이 난다. on/off 가능한 건 소리 2개로 분리.
  - `TidyBed` 같은 특수 이름 말고 `ChangeObject` 처럼 일반적인 틀.
  - 큰 틀을 먼저 만들고, 추가 작동이 필요하면 그때 별도 스크립트.
- 추가로: 접객(숙박객 모집) 시스템 — 아침→점심→저녁(접객)→새벽 4단계, 점심 마무리 후 책상 상호작용 시 "UI 모드"로 전환(플레이어/카메라 고정, 마우스 표시, 테이블 위 오브젝트를 마우스로 조작).

## 현재 상태 조사
| 스크립트 | 역할 | 개편 후 |
|---|---|---|
| `Interaction/Interactable.cs` | enum + switch 단일 클래스 (Pickup/TidyBed/Generic/Flashlight/Push/Door/Curtain) | **얇은 디스패처로 재작성** |
| `Interaction/Door.cs` | 경첩 회전 코루틴 + 자체 AudioSource | → `HingeEffect` + `SfxEffect` |
| `Interaction/ItemDispenser.cs` | 프리팹 생성 + `SetEquipTarget` | → `SpawnObjectEffect` |
| `Player/InteractionOutline.cs` | 화면 중앙 레이 + 아웃라인 + E키 (+가림체크 doc/0077) | → `GazeInteractor` (리네임/정리) |
| `Inventory/InventorySystem.cs` | `AddItem(...)` 슬롯 관리 | 그대로. `PickupEffect`가 호출 |
| `Interaction/ItemImpactSound.cs` | 충돌 소리 | 그대로 (상호작용 아님) |
| `Interaction/CartGroundAlign.cs` | 카트 물리 | 그대로 (무관) |
| `Audio/SoundManager.cs`, `Environment/DayNightSwitcher.cs` | Q키 디버그로 낮/밤 토글 | Phase 2에서 `DayPhaseManager`에 연결 |

`Interactable` 사용처: `Motel_Room.prefab`, `FlashLight_low-Poly.prefab`, `Can_Coke.prefab`, `InGame.unity` — **4개뿐** → 개편/마이그레이션 부담 작음.

---

## 제안 아키텍처 (Phase 1: 상호작용 코어)

### 컴포넌트 조합 방식
한 GameObject에 `Interactable` 1개 + `InteractionEffect` 여러 개를 붙인다. 상호작용하면 붙어 있는 효과가 순서대로 실행된다.

### 1. `Interactable` (얇은 디스패처)
```
[SerializeField] string prompt;        // "열기" "줍기" 등 프롬프트 문구
[SerializeField] bool isToggle;         // on/off 상호작용인가
[SerializeField] bool startOn;
[SerializeField] UnityEvent onInteracted; // 자유 연출용 (기존 Generic 대체)

public bool IsOn { get; private set; }
public bool CanInteract { get; }        // 조건 컴포넌트 전부 만족 + enabled

void Interact(Interactor source, Vector3 point):
    if (!CanInteract) return;
    if (isToggle) IsOn = !IsOn;
    var ctx = new InteractionContext(this, source, IsOn, point);
    foreach (effect in cachedEffects) effect.Play(ctx);
    onInteracted.Invoke();
```
- `effects`는 `Awake`에서 `GetComponents<InteractionEffect>()` 캐시.
- 조건: `GetComponents<InteractionCondition>()` (선택, 없으면 항상 가능). Phase 2에서 `PhaseCondition`/`RequireItemCondition` 추가.

### 2. `InteractionEffect` (추상 베이스)
```
public abstract class InteractionEffect : MonoBehaviour
{
    public abstract void Play(in InteractionContext ctx);
}

public readonly struct InteractionContext
{
    public readonly Interactable Interactable;
    public readonly GameObject Source;   // 상호작용한 주체(플레이어 등)
    public readonly bool IsOn;           // 토글 후 상태
    public readonly Vector3 Point;       // 레이 히트 지점
}
```

### 3. 기본 효과 (각각 작은 스크립트, 스택 가능)
| 효과 | 대체 대상 | 주요 필드 | 동작 |
|---|---|---|---|
| `SfxEffect` | Door/Curtain 오디오 로직 | `clip`, `onClip`, `offClip`, `interrupt` | 토글이면 `IsOn ? onClip : offClip`, 아니면 `clip`. `interrupt`면 `Stop()→clip→Play()` (스윙 중 재토글 시 소리 교체). AudioSource 자동 부착(3D). |
| `ChangeObjectEffect` | `TidyBed`, `Curtain` | `onObjects[]`, `offObjects[]` | 토글: `IsOn`→onObjects 켜고 offObjects 끔 / 반대. 비토글: 한 번만 onObjects 켜고 offObjects 끔. |
| `SpawnObjectEffect` | `ItemDispenser` | `prefab`, `spawnPoint`, `parent`, `maxCount` | 프리팹 생성. 획득 아이템은 생성 프리팹의 `PickupEffect.itemId` 가 연결 담당. |
| `PushEffect` | `Push` 케이스 | `pushForce`, `torqueForce`, `useSteerAxis` | 부모 Rigidbody를 주체 반대 방향으로 임펄스 + 토크. |
| `HingeEffect` | `Door.cs` | `openAngle`, `openTime`, `ease`, (피벗은 자기 transform) | 로컬 Y로 `closedRot↔openRot` Slerp 코루틴. `IsOn`이 방향. 인터럽트 안전. |
| `PickupEffect` | `Pickup`, `Flashlight` | `icon`, `equipTarget`, `useClip`, `consumeOnUse`, `isFlashlight` | `InventorySystem.Instance.AddItem(...)`, 성공 시 픽업 오브젝트 비활성/파괴. |

> "모든 상호작용에 소리" → `Interactable`이 `Awake`에서 `SfxEffect` 없으면 경고 로그(강제 아님, 조립 실수 방지용). 의견 필요.

### 4. 입력 드라이버 (`Interactor` 베이스)
| 드라이버 | 설명 |
|---|---|
| `GazeInteractor` (← `InteractionOutline`) | 화면 중앙 레이, Interaction 레이어, 가림 체크(doc/0077), E키 → `Interact`. 아웃라인 + 프롬프트. |
| `CursorInteractor` (신규) | 마우스 위치 레이, 호버 아웃라인, 좌클릭 → `Interact`. **UI 모드에서만 활성.** |

둘 다 같은 `Interactable.Interact()`를 호출 → 효과 조합은 입력 방식과 무관.

### 파일 구조 (신규)
```
Assets/My/Scripts/Interaction/
  Core/
    Interactable.cs        (재작성)
    InteractionEffect.cs   (베이스 + InteractionContext)
    InteractionCondition.cs (베이스, 구현체는 Phase 2)
    Interactor.cs          (베이스)
  Drivers/
    GazeInteractor.cs      (← InteractionOutline.cs)
    CursorInteractor.cs    (신규, Phase 2에서 사용)
  Effects/
    SfxEffect.cs
    ChangeObjectEffect.cs
    SpawnObjectEffect.cs
    PushEffect.cs
    HingeEffect.cs
    PickupEffect.cs
```
삭제: `Door.cs`, `ItemDispenser.cs`, `InteractionOutline.cs` (내용 이관).
`InteractionType` enum 제거.

### 마이그레이션 (4개 에셋)
1. `Can_Coke.prefab`: Interactable(Pickup) → Interactable + `PickupEffect` + `SfxEffect`.
2. `FlashLight_low-Poly.prefab`: Interactable(Flashlight) → Interactable + `PickupEffect(isFlashlight)` + `SfxEffect`.
3. `Motel_Room.prefab`: 침대 Interactable(TidyBed) → `ChangeObjectEffect`(비토글) + `SfxEffect`; 커튼 → `ChangeObjectEffect`(토글) + `SfxEffect(onClip/offClip)`; 문 Hinge → `HingeEffect` + `SfxEffect(onClip/offClip)`, `door` 필드 제거.
4. `InGame.unity`: `InteractionOutline` → `GazeInteractor` 컴포넌트 교체(필드 동일: interactDistance/interactMask/playerCamera/interactionText).

프리팹 재배선은 **에디터 작업(사용자)** 또는 **1회용 에디터 마이그레이션 스크립트**(내가 작성). 4개뿐이라 수동도 현실적. → 선택 필요.

---

## 제안 아키텍처 (Phase 2: 접객 / UI 모드) — 별도 승인

Phase 1 위에 얹는다. 게임 로직(손님 심사 등)은 이후 별도.

### `DayPhaseManager` (싱글턴)
- `enum Phase { Morning, Noon, Evening, Dawn }`, `CurrentPhase`, `AdvancePhase()`, `event OnPhaseChanged`.
- `DayNightSwitcher` / `SoundManager`의 Q키 디버그 토글을 이 이벤트 구독으로 교체.

### `UIInteractionMode` (매니저)
- `Enter(Transform anchor)`: `FirstPersonController` + 룩 비활성, 플레이어 루트/카메라를 `anchor` 포즈로 이동(스냅 or 짧은 lerp), `Cursor.lockState=None; visible=true`, `GazeInteractor` 끄고 `CursorInteractor` 켬, "나가기(ESC)" 표시.
- `Exit()`: 역순 복구.

### `EnterUIModeEffect` (효과)
- 책상 `Interactable`에 부착. `anchor` transform 참조. `Play` 시 `UIInteractionMode.Enter(anchor)`.
- 조건 `PhaseCondition(Evening)` 부착 → 저녁에만 책상 상호작용 활성.

### 테이블 위 오브젝트 (컴퓨터/신분증/벨)
- 각자 `Interactable`(Interaction 레이어) + 효과 조합. `CursorInteractor`가 UI 모드에서 구동.
- 예: 노트북 = `ChangeObjectEffect`(열림/닫힘) + `SfxEffect` + `onInteracted`로 서브 패널 오픈.

### 신규 파일 (Phase 2)
```
Assets/My/Scripts/Game/DayPhaseManager.cs
Assets/My/Scripts/Interaction/Modes/UIInteractionMode.cs
Assets/My/Scripts/Interaction/Effects/EnterUIModeEffect.cs
Assets/My/Scripts/Interaction/Conditions/PhaseCondition.cs
```

---

## 확인 필요
1. **범위**: 이번에 Phase 1(코어 개편)만? 아니면 Phase 2(단계 매니저 + UI 모드 스캐폴드)까지?
2. **마이그레이션**: 신규 스크립트는 내가 작성 → 프리팹 4개 재배선을 (a) 사용자가 에디터에서 (내가 정확한 단계 제공) vs (b) 내가 1회용 에디터 마이그레이션 스크립트 작성. 어느 쪽?
3. **효과 세트**: `SfxEffect / ChangeObjectEffect / SpawnObjectEffect / PushEffect / HingeEffect / PickupEffect` — 이 구성 OK? 이름 OK? (`HingeEffect` 대신 `OpenEffect`? 등)
4. **"모든 상호작용에 소리"**: `SfxEffect` 없으면 경고 로그 강제 vs 그냥 관례.
5. **UI 모드 진입 시 플레이어 이동**: 하드 스냅 vs 짧은 lerp. 나가기 키 = ESC?
6. **누락된 상호작용**: 현재 파악 = 획득 / 손전등획득 / 물체변경(침대·커튼) / 자유이벤트 / 밀기 / 열기(문) / 생성(디스펜서). 접객이면 추가로 = UI모드진입 / 컴퓨터사용 / 신분증확인 / 벨. 그 외 게임에 조명 on/off, 서랍, TV 등 상호작용 예정 있으면 알려주세요 (대부분 위 효과 조합으로 커버됨).

## 결정 (2026-08-27)
- 범위: **Phase 1 + 2**
- 마이그레이션: **에디터 스크립트** (`Tools > Interaction > Migrate Legacy Interactables`)
- 효과 이름: 제안대로. 소리 없으면 경고 로그(비강제). UI 모드 0.3s lerp + ESC.

## 구현 완료 (코드)

### Phase 1 코어
| 파일 | 내용 |
|---|---|
| `Interaction/Core/InteractionEffect.cs` | `InteractionEffect` 추상 + `InteractionContext` struct |
| `Interaction/Core/InteractionCondition.cs` | 조건 추상 (`IsMet`) |
| `Interaction/Core/Interactor.cs` | 입력 드라이버 베이스 (`Owner`, `TryInteract`) |
| `Interaction/Interactable.cs` | **재작성**. `prompt`/`isToggle`/`startOn`/`onInteracted` + `GetComponents<InteractionEffect/Condition>` 디스패치. `onInteract`→`onInteracted` 는 `[FormerlySerializedAs]` 로 자동 이관. 하단에 LEGACY 필드(마이그레이션용, 나중 삭제) |
| `Interaction/Drivers/GazeInteractor.cs` | 구 `InteractionOutline.cs` (git mv, GUID 유지 → 씬 참조 안 깨짐). `Interactor` 상속, 가림 체크 포함, `Suspended` 플래그 |
| `Interaction/Drivers/CursorInteractor.cs` | 신규. 마우스 레이 + 좌클릭. 기본 비활성 |
| `Interaction/Effects/SfxEffect.cs` | 토글이면 on/off 클립, 아니면 단일. `interrupt` 시 Stop→clip→Play |
| `Interaction/Effects/ChangeObjectEffect.cs` | `onObjects[]`/`offObjects[]` SetActive. 토글/비토글 |
| `Interaction/Effects/SpawnObjectEffect.cs` | 프리팹 생성, `spawnPoint`, `parent`, `maxCount` |
| `Interaction/Effects/PushEffect.cs` | 부모 Rigidbody 임펄스+토크 |
| `Interaction/Effects/HingeEffect.cs` | 경첩 회전 코루틴 (구 Door.cs 로직). 자기 transform 이 피벗 |
| `Interaction/Effects/PickupEffect.cs` | `InventorySystem.AddItem`. `isFlashlight` 시 손전등 자동 탐색 |

### Phase 2
| 파일 | 내용 |
|---|---|
| `Game/DayPhaseManager.cs` | `DayPhase{Morning,Noon,Evening,Dawn}`, 싱글턴, `OnPhaseChanged`, `Advance()`, 디버그 N키 |
| `Interaction/Conditions/PhaseCondition.cs` | 지정 단계에서만 상호작용 허용 (기본 Evening) |
| `Interaction/Modes/UIInteractionMode.cs` | 싱글턴. `Enter(anchor)`/`Exit()`: FPC+CC 비활성, 카메라 lerp, 커서 표시, Gaze↔Cursor 전환, ESC |
| `Interaction/Effects/EnterUIModeEffect.cs` | 상호작용 시 `UIInteractionMode.Enter(anchor)` |

### 마이그레이션
| 파일 | 내용 |
|---|---|
| `Editor/InteractionMigrator.cs` | `Tools > Interaction > Migrate Legacy Interactables`. 프리팹 3개(Can_Coke/FlashLight/Motel_Room) + 로드된 씬의 구 Interactable 을 새 컴포넌트로 변환. Door 는 Interactable 을 Hinge(Door.cs 붙은 GO)로 옮기고 Door 컴포넌트 제거 |

**유지(마이그레이션 후 삭제 예정)**: `Door.cs`, `ItemDispenser.cs`, `Interactable` 의 LEGACY 필드. `ItemDispenser` 는 아직 `SetEquipTarget` 로 동작 — 나중에 `SpawnObjectEffect` 로 수동 교체.
**변경 안 함**: `SoundManager`/`DayNightSwitcher` 의 Q키 낮/밤 토글 — `DayPhaseManager` 연결은 선택적 후속(단계→시각 매핑).

## 사용자 작업 (에디터)
1. Unity 컴파일 통과 확인 (에러 시 알려줄 것).
2. `InGame` 씬 연 상태로 `Tools > Interaction > Migrate Legacy Interactables` 실행 → 콘솔 로그 확인 → **씬 저장**.
3. **문 콜라이더 확인**: 마이그레이션 후 Interactable 이 Hinge 로 이동함. 문 BoxCollider 가 문 루트에 있으면 Hinge(또는 그 자식)로 옮겨야 레이가 잡음 (doc/0076 참조). Interaction 레이어(11)인지도 확인.
4. Phase 2 씬 세팅:
   - 빈 GO `DayPhaseManager` + 컴포넌트.
   - 빈 GO `UIInteractionMode` + 컴포넌트, 필드 연결: cameraTransform(플레이어 카메라), firstPersonController, characterController, gazeInteractor, cursorInteractor(플레이어에 `CursorInteractor` 추가), exitHint(선택).
   - 책상: `Interactable`(prompt="접객 시작", isToggle=off) + `EnterUIModeEffect`(anchor=앉은 시점 빈 Transform) + `SfxEffect` + `PhaseCondition`(Evening).
   - 테이블 오브젝트(컴퓨터/신분증/벨): 각 `Interactable`(Interaction 레이어) + 효과 조합.
5. 검증 끝나면 알려주기 → cleanup 커밋(Door.cs/ItemDispenser.cs/LEGACY 필드/Migrator 삭제).

## 수정 (2026-08-27) — prompt 를 enum(게임 전체 행동 카테고리)으로
자유 문자열 대신 게임의 큰 행동 카테고리를 정리한 enum:
`InteractionPrompt { 상호작용, 여닫기, 켜고끄기, 줍기, 사용, 조사, 정리하기, 밀기, 접객, 직접입력 }`
- 토글 상호작용의 `여닫기`/`켜고끄기`는 하나로 통일 — `Prompt` 프로퍼티가 `IsOn` 상태에 따라 "열기"↔"닫기", "켜기"↔"끄기" 로 반환. 커튼도 `켜고끄기`.
- `직접입력` 선택 시만 `customPrompt` 문자열 사용.
- 새 카테고리 필요하면 enum에 추가.
- 마이그레이터: Door→여닫기, Curtain→켜고끄기, TidyBed→정리하기, Push→밀기, Pickup/Flashlight→줍기.

## 추가 (2026-08-27) — 획득 아이템 ID 연결
줍는 아이템은 프리팹이라 손 오브젝트(씬)를 직접 못 참조 → 번호로 연결.
- `Inventory/ItemId.cs` (enum: None/Flashlight=1/Soda=2), `Inventory/HandItem.cs`(손 오브젝트마다), `Inventory/HandItemRegistry.cs`(손 루트에 1개, id→GameObject 조회).
- `PickupEffect`: `equipTarget` → `itemId` + `equipTargetOverride`. 손전등 여부는 `Flashlight` 컴포넌트 유무로 자동 인식 (`isFlashlight` 필드 제거).
- 마이그레이터: Flashlight→`itemId=Flashlight`, 일반 Pickup→`None`(수동 지정 경고).
- 에디터 세팅: 손전등/소다 손 오브젝트에 `HandItem`, 공통 부모에 `HandItemRegistry`. 줍는 프리팹 `PickupEffect.itemId` 맞춤.

## 상태
2026-08-27 Phase 1+2 코드 + 마이그레이터 + 아이템 ID 시스템 작성 완료. 에디터 작업/검증 대기.
