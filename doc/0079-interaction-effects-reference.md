# 0079 - 상호작용 효과 레퍼런스

새 상호작용 시스템(doc/0078)의 각 컴포넌트가 무슨 일을 하는지 정리. 조립용 레퍼런스.

---

## 모델 한눈에

```
GameObject (Interaction 레이어 + Collider + Outline[off])
├─ Interactable            ← 플레이어가 찾는 대상, 디스패처 (1개)
├─ InteractionEffect …     ← 실제 동작 (여러 개 스택 가능)
└─ InteractionCondition …  ← 상호작용 가능 여부 게이트 (0~N개, 선택)
```
Collider·Outline·레이어는 우클릭 "재설정" 이 자동으로 챙긴다.

- 플레이어 쪽 **Interactor**(`GazeInteractor` = 화면중앙+E, `CursorInteractor` = 마우스+클릭)가 대상을 찾아 `Interactable.Interact()` 호출.
- `Interactable`은 붙어 있는 모든 `InteractionEffect.Play(ctx)`를 **컴포넌트 순서대로** 실행 + `onInteracted` UnityEvent 발동.
- 효과는 `ctx`(상호작용 정보)를 읽는다: `Interactable`, `Source`(주체=플레이어), `IsOn`(토글 후 상태), `Point`(레이 히트 지점).

---

## Interactable (디스패처)

| 필드 | 설명 |
|---|---|
| `promptType` | 행동 카테고리 드롭다운 (`InteractionPrompt` enum). 아래 표 참조 |
| `customPrompt` | `promptType = 직접입력` 일 때만 쓰는 문자열 |
| `isToggle` | on/off 왕복 상호작용인가. 켜면 상호작용마다 `IsOn` 이 뒤집힘 |
| `startOn` | 시작 상태 |
| `onInteracted` | 자유 연출용 UnityEvent (구 `Generic` 대체) |

**런타임 프로퍼티**
- `IsOn` : 현재 토글 상태 (비토글이면 의미 없음, 항상 `startOn`)
- `IsToggle` : `isToggle` 값
- `Prompt` : 표시 문구. `여닫기`/`켜고끄기`는 `IsOn`에 따라 동적:
  - `여닫기` → 닫혀 있으면 "열기", 열려 있으면 "닫기"
  - `켜고끄기` → 꺼져 있으면 "켜기", 켜져 있으면 "끄기"
  - 그 외 → enum 이름 그대로 ("줍기", "정리하기" …)
  - `직접입력` → `customPrompt`
- `CanInteract` : `enabled` && 활성 상태 && 모든 `Condition.IsMet` → false면 상호작용/아웃라인/프롬프트 전부 무시

**Interact 흐름**
1. `CanInteract` 아니면 중단
2. `isToggle` 면 `IsOn = !IsOn`
3. `ctx` 생성 (`IsOn` = 토글이면 새 상태, 비토글이면 항상 true)
4. 활성화된 효과들 `Play(ctx)` 순서대로
5. `onInteracted.Invoke()`

> **`promptType` 자체는 기능이 없다** — 화면에 뜨는 글자일 뿐. 실제 동작은 붙인 Effect 컴포넌트가 전부.
> 대신 컴포넌트 **우클릭 → "Prompt Type에 맞게 효과 재설정"** 이 promptType 에 맞춰 구성을 동기화한다:
> - 필요한 효과 **추가** + 필요 없는 managed 효과 **제거** (전부 Undo 가능)
> - `SfxEffect` 는 항상 포함 → `[RequireComponent(AudioSource)]` 로 AudioSource 자동 부착 (3D/논플레이온어웨이크로 초기화)
> - 콜라이더 없으면 `BoxCollider` 추가(메시 bounds 로 크기) + `Interaction` 레이어 설정. 콜라이더가 이미 있으면 레이어만 맞춤. 자식에 있으면 경고
> - `Outline` 없으면 추가 → **enabled=off** (Interactor 가 볼 때만 켬), 모드 `OutlineVisible`
> - **컴포넌트 순서 정렬** (메쉬·콜라이더 → 스크립트 → 사운드 → 나머지):
>   `Transform → MeshFilter → Renderer → Collider → Rigidbody → Interactable → Condition → 일반 Effect → SfxEffect → Outline·기타 .cs → AudioSource → 그 외`. 기존 아이템도 재설정하면 이 순서로 재배치됨
> - 콘솔에 추가/제거 내역 로그
>
> | promptType | 효과 구성 | isToggle |
> |---|---|---|
> | 여닫기 | `HingeEffect` + `SfxEffect` | ✓ |
> | 켜고끄기 | `ChangeObjectEffect` + `SfxEffect` | ✓ |
> | 정리하기 | `ChangeObjectEffect` + `SfxEffect` | ✗ |
> | 줍기 | `PickupEffect` + `SfxEffect` + `ItemImpactSound` | — |
> | 사용 | `SpawnObjectEffect` + `SfxEffect` | — |
> | 밀기 | `PushEffect` + `SfxEffect` + `ItemImpactSound` | — |
> | 접객 | `EnterUIModeEffect` + `SfxEffect` + `PhaseCondition`(추가만) | — |
> | 상호작용/조사/직접입력 | `SfxEffect` | — |
>
> managed 효과 = `Sfx / ChangeObject / Hinge / Push / Pickup / SpawnObject / EnterUIMode` (이것만 자동 제거).
> **`ItemImpactSound`·`PhaseCondition`·커스텀 효과·`onInteracted` 은 추가만 하고 제거 안 함** — 다른 프롬프트로 바꿔도 남으니 필요 없으면 수동 제거. `ItemImpactSound` 는 `impactClips` 수동 지정 필요, 충돌 감지엔 Rigidbody 도 있어야 함(밀기=카트에 Rigidbody, 줍기=던질 때 자동 부착).
> 재설정 후 각 효과의 오브젝트/클립 필드는 수동으로 채운다 (Unity가 알 수 없는 값).

### InteractionPrompt (행동 카테고리)
| 값 | 문구 | 비고 |
|---|---|---|
| `상호작용` | "상호작용" | 기본값 |
| `여닫기` | "열기"/"닫기" | 토글. 문·뚜껑·서랍 |
| `켜고끄기` | "켜기"/"끄기" | 토글. 조명·커튼·TV |
| `줍기` | "줍기" | 인벤토리 획득 |
| `사용` | "사용" | 디스펜서·기계 조작 |
| `조사` | "조사" | 관찰·읽기 |
| `정리하기` | "정리하기" | 청소 (비토글 1방향) |
| `밀기` | "밀기" | 물리 밀기 |
| `접객` | "접객" | UI 모드 진입 |
| `직접입력` | `customPrompt` | 위에 없는 일회성 문구 |

---

## InteractionEffect (동작 효과)

### SfxEffect — 효과음
| 필드 | 설명 |
|---|---|
| `clip` | 비토글 상호작용용 단일 클립 |
| `onClip` / `offClip` | 토글 상호작용용. 켜질 때 / 꺼질 때 |
| `interrupt` (기본 on) | 재생 중 다시 상호작용하면 이전 소리를 끊고 교체 (문 스윙 도중 재토글 등). off면 겹쳐 재생 |

- 동작: `Interactable.IsToggle` 면 `IsOn ? onClip : offClip`, 아니면 `clip` 재생.
- `AudioSource` 없으면 자동 부착 (3D 사운드, playOnAwake off). 볼륨/믹서는 그 AudioSource에서 조정.
- **모든 상호작용에 하나씩 붙이는 걸 권장.** 없으면 `Interactable`이 경고 로그(강제 아님).

### ChangeObjectEffect — 오브젝트 켜기/끄기 스왑
| 필드 | 설명 |
|---|---|
| `onObjects[]` | "on" 상태에서 활성화할 오브젝트들 |
| `offObjects[]` | "on" 상태에서 비활성화할 오브젝트들 (off 상태에선 반대) |

- 토글 상호작용: `IsOn` 이면 onObjects 켜고 offObjects 끔 / 아니면 반대.
- 비토글 상호작용: 상호작용 시 항상 onObjects 켜고 offObjects 끔 (되돌리기 없음 — 침대 정리 등).
- 구 `TidyBed`(비토글), `Curtain`(토글) 을 하나로 대체.
- 외곽선: 스왑되는 두 메쉬가 공통 부모 아래 있고 부모에 `Outline` 이 있으면 QuickOutline 패치(doc/0076)로 유지됨.

### HingeEffect — 경첩 회전 (여닫기)
| 필드 | 설명 |
|---|---|
| `hinge` | 회전시킬 Transform. **비우면 이 오브젝트 자신**. 문/뚜껑 피벗을 직접 지정 가능 |
| `axis` (기본 `(0,1,0)`) | `hinge` 로컬 기준 회전축. 문=위쪽 Y, 쓰레기통 뚜껑=옆쪽 X 등 |
| `openAngle` (기본 90) | 열림 각도. 음수면 반대 방향 |
| `openTime` (기본 0.6) | 여닫는 시간(초) |
| `ease` | 회전 보간 커브 (기본 EaseInOut) |

- `hinge` 지정 안 하면: 이 컴포넌트 붙은 오브젝트가 피벗 → 문/뚜껑 메시를 자식으로 둠.
- `hinge` 지정: `Interactable`+`HingeEffect` 는 본체(쓰레기통 몸통 등)에 두고, 뚜껑 피벗 Transform 만 연결.
- `Awake`: `hinge` 현재 로컬 회전을 "닫힘"으로 기억, `openRot = 닫힘 * AngleAxis(openAngle, axis)`.
- `Start`: `Interactable.IsOn` 에 맞춰 초기 회전 적용.
- 상호작용 시 목표 회전으로 코루틴 Slerp. 스윙 도중 재상호작용하면 중단 후 반대로 (인터럽트 안전).
- `isToggle` 켜서 사용. 구 `Door.cs` 대체.

### PushEffect — 물리 밀기
| 필드 | 설명 |
|---|---|
| `pushForce` (기본 6) | 주체 반대 방향 임펄스 크기 |
| `torqueForce` (기본 2) | 히트 지점 기준 회전 토크 크기 |
| `useSteerAxis` (기본 on) | 토크를 로컬 Z(조향) 축으로만 제한 — 쇼핑카트용. off면 자유 회전 |

- 부모의 `Rigidbody`를 찾아 `ctx.Source`(플레이어, 없으면 Player 태그) 반대 방향으로 밀기.
- 수평(`y=0`)만. `Rigidbody`가 kinematic이면 무시.
- 토크 = `Cross(Point - 무게중심, 방향) * torqueForce`. `useSteerAxis` 면 body.forward 성분만.
- 구 `Push` 케이스 대체.

### PickupEffect — 인벤토리 획득
| 필드 | 설명 |
|---|---|
| `icon` | 슬롯 아이콘 스프라이트 |
| `itemId` | 아이템 번호 (`ItemId` enum). 플레이어 손의 `HandItem` 과 매칭 (손전등=001, 소다=002) |
| `equipTargetOverride` | 씬에 직접 배치한 경우의 손 오브젝트 오버라이드. 비우면 `itemId` 로 조회 |
| `useClip` | 좌클릭 사용 시 재생할 소리 (InventorySystem이 재생) |
| `consumeOnUse` | 사용 시 소모(1회용) 아이템인가 |

- 손에 드는 오브젝트 = `equipTargetOverride` 있으면 그것, 없으면 `HandItemRegistry.Resolve(itemId)`.
- 손전등 여부는 손 오브젝트에 `Flashlight` 컴포넌트가 있으면 자동 인식.
- 조회 실패(둘 다 없음) → 그냥 `Destroy` (연출용 줍기). `itemId` 가 지정돼 있는데 못 찾으면 경고 로그.
- `InventorySystem.AddItem(...)` 성공 시 이 오브젝트 `SetActive(false)`.
- 구 `Pickup` + `Flashlight` 케이스 대체.

#### 아이템 ID 연결 (프리팹 ↔ 손 오브젝트)
바닥에서 줍는 아이템은 프리팹이라 손 오브젝트(씬)를 직접 참조 못 함 → **번호로 연결**.

| 스크립트 | 위치 | 역할 |
|---|---|---|
| `ItemId` (enum) | — | `None=0, Flashlight=1(001), Soda=2(002)`. 새 아이템은 여기 추가 |
| `HandItem` | 플레이어 손에 드는 오브젝트마다 | `id` 지정 |
| `HandItemRegistry` | 손 루트(항상 활성인 부모) 1개 | 시작 시 자식 `HandItem` 들을 `id`로 색인, `Resolve(id) → GameObject` |

세팅: 손전등 손 오브젝트에 `HandItem(id=Flashlight)`, 소다 손 오브젝트에 `HandItem(id=Soda)`, 그 공통 부모에 `HandItemRegistry`. 줍는 프리팹의 `PickupEffect.itemId` 를 맞춰줌.

### SpawnObjectEffect — 프리팹 생성
| 필드 | 설명 |
|---|---|
| `prefab` | 생성할 프리팹 |
| `spawnPoint` | 생성 위치/회전 (비우면 이 오브젝트 transform) |
| `parent` | 생성물이 자식으로 들어갈 Transform. 비우면 씬 최상위 (보통 비움) |
| `maxCount` (기본 0) | 동시 존재 최대 개수. 0 = 무제한(기본). 초과 시 생성 안 함 |

- 이미 생성한 것들을 추적, null(파괴됨)은 카운트에서 제외.
- 획득형 아이템은 생성 프리팹의 `PickupEffect.itemId` 가 손 오브젝트를 연결하므로 여기서 따로 안 함.
- 구 `ItemDispenser` 대체.

### EnterUIModeEffect — UI 모드 진입 (Phase 2)
| 필드 | 설명 |
|---|---|
| `anchor` | 카메라가 이동할 위치/방향 (앉은 시점). 비우면 이 오브젝트 transform |

- 상호작용 시 `UIInteractionMode.Instance.Enter(anchor)`.
- 보통 같은 오브젝트에 `PhaseCondition`(저녁) 을 같이 붙임.

---

## InteractionCondition (게이트)

### PhaseCondition — 하루 단계 제한
| 필드 | 설명 |
|---|---|
| `allowedPhases[]` (기본 `{Evening}`) | 이 단계들에서만 상호작용 허용 |

- `DayPhaseManager.Instance` 없으면 항상 통과.
- 조건 불만족 시 `CanInteract=false` → 아웃라인·프롬프트도 안 뜸.

---

## Interactor (입력 드라이버, 플레이어 쪽)

### GazeInteractor (구 InteractionOutline)
| 필드 | 설명 |
|---|---|
| `interactDistance` (3) | 사거리 |
| `interactMask` | 상호작용 레이어 마스크 (씬에서 "Interaction"=11 만) |
| `playerCamera` | 레이 기준 카메라 |
| `interactionText` | 프롬프트 UI 오브젝트 (있을 때 SetActive) |

- 화면 중앙 레이 → `Outline` 켜기 + `interactionText` 표시, `E` 로 상호작용.
- **가림 체크**(doc/0077): 대상 앞에 막는 콜라이더(Interaction·Ignore Raycast 제외) 있으면 무시 → 벽 너머 상호작용 차단.
- `Suspended = true` 면 잠시 꺼짐 (UI 모드에서 사용).

### CursorInteractor (UI 모드용)
| 필드 | 설명 |
|---|---|
| `interactDistance` (5) | 사거리 |
| `interactMask` | 레이어 마스크 |
| `cam` | 레이 기준 카메라 (비우면 `Camera.main`) |

- 마우스 위치 레이 → 호버 시 `Outline`, 좌클릭으로 상호작용.
- `UIInteractionMode` 가 진입 시 켜고 해제 시 끈다 (평소 비활성).

---

## Phase 2 매니저

### DayPhaseManager (싱글턴)
- `DayPhase { Morning, Noon, Evening, Dawn }`. `Current`, `DayCount`, `event OnPhaseChanged`.
- `Advance()` : 다음 단계 (새벽→아침 시 `DayCount++`).
- 디버그: `N` 키로 `Advance` (`debugAdvanceKey`).

### UIInteractionMode (싱글턴)
| 필드 | 설명 |
|---|---|
| `cameraTransform` | 이동시킬 카메라 |
| `firstPersonController` | 진입 시 비활성화할 이동 컨트롤러 |
| `characterController` | 진입 시 비활성화 (transform 이동 충돌 방지) |
| `gazeInteractor` / `cursorInteractor` | 진입 시 Gaze 정지 + Cursor 켬 |
| `exitHint` | "ESC 나가기" UI (선택) |
| `moveTime` (0.3) | 카메라 이동 시간 |

- `Enter(anchor)` : 카메라 포즈/커서 상태 저장 → FPC·CC 끔, Gaze 정지, 커서 표시 → 카메라를 anchor로 lerp → 도착 시 CursorInteractor 켬.
- `Exit()` : 역순 복구. `Active` 중 `ESC` 로 자동 호출.

---

## 조립 레시피

| 대상 | Interactable | 효과 / 조건 |
|---|---|---|
| **문** (Hinge 피벗 GO) | 여닫기, isToggle✓, startOn=닫힘 | `HingeEffect`(hinge 비움, axis=Y, openAngle) + `SfxEffect`(onClip/offClip). 콜라이더는 Hinge나 그 자식, Interaction 레이어 |
| **쓰레기통 뚜껑** (통 몸통 GO) | 여닫기, isToggle✓ | `HingeEffect`(hinge=뚜껑 피벗 Transform, axis=X 등, openAngle=±80) + `SfxEffect`. 콜라이더는 몸통, Interaction 레이어 |
| **커튼** (커튼 루트) | 켜고끄기, isToggle✓ | `ChangeObjectEffect`(on=열린커튼, off=닫힌커튼) + `SfxEffect`(onClip/offClip). Outline은 루트 |
| **침대 정리** | 정리하기, isToggle✗ | `ChangeObjectEffect`(on=정리된침대, off=흐트러진침대) + `SfxEffect`(clip) |
| **조명 스위치** | 켜고끄기, isToggle✓ | `ChangeObjectEffect`(광원 오브젝트) 또는 `onInteracted`로 Light 토글 + `SfxEffect`(딸깍 on/off) |
| **아이템 줍기** (아이템 프리팹) | 줍기 | `PickupEffect`(icon, itemId) + `SfxEffect`(clip) + `ItemImpactSound`(impactClips). 손 오브젝트엔 `HandItem`(같은 id) |
| **손전등** | 줍기 | `PickupEffect`(itemId=Flashlight) + `SfxEffect` + `ItemImpactSound`. 손전등 손 오브젝트에 `HandItem`(Flashlight) + `Flashlight` 컴포넌트 |
| **아이템 디스펜서** | 사용 | `SpawnObjectEffect`(prefab, spawnPoint) + `SfxEffect`. 생성 프리팹에 `PickupEffect`(itemId) |
| **쇼핑카트 밀기** (카트, Rigidbody) | 밀기, isToggle✗ | `PushEffect`(pushForce, useSteerAxis✓) + `SfxEffect` + `ItemImpactSound`(벽 부딪힘 소리) |
| **접객 책상** | 접객, isToggle✗ | `EnterUIModeEffect`(anchor) + `SfxEffect` + `PhaseCondition`(Evening) |
| **버튼/자유 연출** | 사용 | `onInteracted` UnityEvent 만 (효과 없이) |
| **테이블 위 오브젝트**(컴퓨터/신분증) | 조사/사용 | 아무 효과 조합 + Interaction 레이어. UI 모드에서 `CursorInteractor` 가 구동 |

## 새 동작이 필요하면
위 효과 조합으로 안 되는 특수 작동만 새 `InteractionEffect` 서브클래스를 만든다 (`Play(in InteractionContext ctx)` 하나 구현, `[RequireComponent(typeof(Interactable))]` 자동 상속). 큰 틀은 건드리지 않는다.

---

## 2026-08-28 갱신: 노트/모니터 + 하루 시간대 전환 (doc/0100)

### 프롬프트 변경
- `접객` → **`화면고정`** 으로 rename (enum index 8 유지 → 기존 프리팹 자동 매핑). 효과 = `EnterUIModeEffect` + `SfxEffect`. **`PhaseCondition` 자동추가 없음** (모니터 등 시간대 무관 재사용).
- 신규 `읽기` → `ShowPanelEffect` + `SfxEffect`.
- 신규 하루종료 스위치 4종: `아침종료`/`점심종료`/`저녁종료`/`하루종료` → `PhaseSwitchEffect` + `SfxEffect` + `PhaseCondition`. 재설정 시 `PhaseSwitchEffect.from/to` 와 `PhaseCondition.allowedPhases[0]` 가 **자동 설정** (아침종료 = Morning→Noon, 점심종료 = Noon→Evening, 저녁종료 = Evening→Dawn, 하루종료 = Dawn→Morning). 문구: 아침·점심 "일과 종료", 저녁 "영업 종료", 하루종료 "취침".
- `ManagedEffects` 에 `ShowPanelEffect`, `PhaseSwitchEffect` 추가.

### 신규 효과
**ShowPanelEffect — "읽기" (노트/편지/사진)**
| 필드 | 설명 |
|---|---|
| `content` | 상호작용 시 켤 오브젝트. UI 이미지·패널이든 별도 Canvas 든 3D 오브젝트든 아무 GameObject. `Awake` 에서 자동 비활성화 |

- 상호작용 시 `content` 토글 + `UIInteractionMode.FreezeForOverlay(true)` (이동/시야 정지 + 커서 표시, 앵커 이동 없음). ESC 또는 재상호작용으로 닫음. UI면 닫기 버튼에서 `ShowPanelEffect.Close()` 호출 가능 (`SetActive(false)` 직접 호출 금지 — 퍼즈 안 풀림).
- 필드명 `panel` → `content` 로 변경 (`[FormerlySerializedAs]` 로 기존 연결 유지).

**PhaseSwitchEffect — 하루 단계 전환 스위치**
| 필드 | 설명 |
|---|---|
| `from` | 이 단계일 때만 전환 (현재가 `from` 이 아니면 무시 — 안전장치) |
| `to` | 전환 목표 단계 |

- `Play()` → 현재 == `from` 이면 `DayPhaseManager.TransitionTo(to)` (ScreenFader 페이드 경유).
- 프롬프트/아웃라인 표시는 같은 오브젝트의 `PhaseCondition(from)` 이 게이팅. 둘 다 재설정으로 자동 세팅.
- `Advance()`(순환상 다음)는 디버그 N/Q 와 `ReceptionManager` 접객 종료에서만 사용.

### 신규/변경 매니저
| 스크립트 | 역할 |
|---|---|
| `Environment/ScreenFader` (신규, 싱글턴) | 풀스크린 검정 Image+CanvasGroup. `FadeThrough(atBlack, done)` — 암전→콜백→밝아짐 (out 0.4 / hold 0.1 / in 0.6). 없으면 콜백 즉시 |
| `Environment/PhaseVisuals` (신규, 구 `DayNightSwitcher` 대체) | `OnPhaseChanged` 구독 → 4단계 `PhaseLook{skybox,lightRoot,volume,fog}` 배열 스왑 (암전 중이라 안 보임) |
| `Game/DayPhaseManager` (변경) | `Advance()` 가 `ScreenFader` 경유. 암전 시 `Current`/`DayCount`/`OnPhaseChanged`, 페이드 인 후 신규 `OnPhaseChangeFinished`. `Transitioning` 가드. 디버그 **N + Q** 로 Advance |
| `Game/ReceptionManager` (변경) | heuristic 삭제. `OnPhaseChanged`→`Evening` 시 `UIInteractionMode.Enter(receptionAnchor)` + 세션 시작 + `SuppressEscExit`. `EndSession()` (손님 전원 처리 시 호출 예정, 임시 디버그 K) → `UIInteractionMode.Exit()` + `Advance()`(→새벽) |
| `Interaction/Modes/UIInteractionMode` (변경) | **앵커 스택**: `Enter` 가 Active 면 위에 쌓음(접객→모니터), `Exit`=한 단계 pop(비면 `Teardown`+`Exited`), `ExitAll`=전부. ESC = 항상 한 겹 벗김 (노트가 `ConsumesEsc` 로 우선). `FreezeForOverlay(bool)` (이동 없이 정지+커서, 노트용). `crosshair` 필드 |
| `Audio/SoundManager` (변경) | Q 토글 삭제 → `OnPhaseChanged` 구독, 저녁·새벽=밤 / 아침·점심=낮 앰비언스 |

### 조립 레시피 추가
| 대상 | promptType | 재설정 자동 | 수동 |
|---|---|---|---|
| **게시판** (아침→점심) | `아침종료` | `PhaseSwitchEffect`+`SfxEffect`+`PhaseCondition(Morning)` | 콜라이더/레이어 |
| **접객 테이블** (점심→저녁) | `점심종료` | `PhaseSwitchEffect`+`SfxEffect`+`PhaseCondition(Noon)` | 자식 `Player_Anchor` → `ReceptionManager.receptionAnchor`. 저녁 접객은 자동 진입 |
| **침대** (새벽→아침) | `하루종료` | `PhaseSwitchEffect`+`SfxEffect`+`PhaseCondition(Dawn)` | — |
| **모니터** | `화면고정` | `EnterUIModeEffect`+`SfxEffect` | anchor. 화면버튼 = Quad+BoxCollider+Interaction 레이어+`Interactable` (월드 UI Canvas 금지, `CursorInteractor` 는 물리 레이) |
| **노트** | `읽기` | `ShowPanelEffect`+`SfxEffect` | `content` = 보여줄 오브젝트(UI/Canvas/3D 등, Awake 가 자동 비활성) |

## 상태
2026-08-27 작성. 2026-08-28 갱신 (doc/0100). 코드 기준 doc/0078 + doc/0100.
