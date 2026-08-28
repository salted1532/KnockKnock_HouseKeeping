# 0092 - 접객: Player_Anchor 이동 + 제한 시야 UI 모드 (제안)

## 요청
> 접객 상호작용 시 해당 오브젝트 밑에 socket처럼 `Player_Anchor` 오브젝트가 있고 그 위치에
> 플레이어를 위치시키고 접객 모드로 넘어간다. 위치시킨 방향으로 화면도 고정하되 어느 정도
> 그 위치를 둘러볼 수 있음. 마우스 락 풀고 마우스 표시.
> 게임 매니저를 하나 둬서 하루 일과의 각 파트를 관리, 일단 접객부터.
> 흐름: 접객 상호작용 → UI 모드 + 플레이어 앵커로 이동 + 화면 고정(제한 시야) → 마우스 표시. 여기까지.

## 현재 상태 (doc 0078 에서 만든 것, 아직 씬 미배선)
| 조각 | 상태 |
|---|---|
| `Interaction/Effects/EnterUIModeEffect.cs` | `anchor` transform 참조 → `UIInteractionMode.Enter(anchor)` |
| `Interaction/Modes/UIInteractionMode.cs` | `Enter/Exit`: **카메라만** 앵커로 0.3s lerp, FPC+CharacterController 끔, 커서 표시, Gaze↔Cursor 전환, ESC 종료 |
| `Interaction/Conditions/PhaseCondition.cs` | 지정 단계(기본 Evening)에서만 상호작용 허용 |
| `Game/DayPhaseManager.cs` | 싱글턴, `Current`, `Advance()`, `OnPhaseChanged`, 디버그 N키 |

**부족한 것**: (1) 플레이어 루트 이동 없음(카메라만 움직임) (2) 제한 시야 없음(완전 고정) (3) 접객 파트 매니저 없음.

## 카메라 구조 (PlayerCapsule.prefab)
```
PlayerCapsule            ← FirstPersonController(yaw = transform.Rotate) + CharacterController(위치)
  PlayerCameraRoot       ← pitch (CinemachineCameraTarget), Cinemachine follow 타겟
Main Camera (씬, 별도)   ← Cinemachine 이 PlayerCameraRoot 를 따라감
```
→ **플레이어 루트만 옮기면 PlayerCameraRoot(자식)도 따라가고 Main Camera 도 Cinemachine 으로 붙어옴.**
현재 `UIInteractionMode` 가 raw 카메라를 직접 lerp 하는 건 Cinemachine 과 싸우는 구조라 이 참에 교체.

## 제안

### 1. `UIInteractionMode`: 이동 대상 = 카메라 → 플레이어 루트 + pitch 피벗
- 필드 교체: `cameraTransform` → `Transform playerRoot`(PlayerCapsule) + `Transform cameraPitchPivot`(PlayerCameraRoot).
- `Enter(anchor)`:
  1. 현재 `playerRoot` 위치/회전, `cameraPitchPivot.localRotation`, 커서 상태 저장.
  2. FPC 끔, CharacterController 끔(끈 뒤 transform 이동 가능).
  3. 0.3s lerp: `playerRoot` → `anchor.position` + `anchor` 의 yaw / `cameraPitchPivot` → `anchor` 의 pitch.
  4. `Cursor.lockState=None; visible=true`, Gaze suspend, Cursor 드라이버 on, exitHint 표시.
- `Exit()`: 역순. lerp 로 원위치 복귀 후 CC→FPC 재활성, 커서 복원.
- `anchor` = 접객 오브젝트 자식 빈 Transform, 이름 규칙 **`Player_Anchor`**. 위치+정면방향(+선택적 내려다보는 pitch)을 담음.

### 2. 제한 시야 (같은 `UIInteractionMode.Update` 안에서)
- Active 동안 마우스 이동으로 시야 회전, 단 앵커 정면 기준 클램프:
  - `yawRange`(기본 ±40°) → `playerRoot` yaw
  - `pitchRange`(기본 ±25°) → `cameraPitchPivot` pitch
- **입력 방식 결정 필요** (아래 확인 1).

### 3. 접객 파트 매니저
- `DayPhaseManager` 는 "지금 몇 단계"만 앎. "각 파트가 무엇을 하는가"는 **파트별 컨트롤러가 `OnPhaseChanged` 구독**하는 패턴 제안.
- 이번 "여기까지" 범위에선 접객 컨트롤러가 할 일이 거의 없음 — 책상 `Interactable` + `PhaseCondition(Evening)` 이 이미 "저녁에만 접객 시작 가능"을 처리.
- 제안: 얇은 `Game/ReceptionManager.cs` 스텁 — `bool InSession`, `event OnSessionStarted/Ended` 만. `UIInteractionMode.Enter/Exit` 가 저녁 단계일 때 이걸 토글. 손님 큐/신분증 심사/일일 정산은 다음 단계에서 이 위에 얹음.
- **확인 2**: 이 스텁을 지금 만들지, 손님 로직 나올 때 만들지.

## 파일
- `Interaction/Modes/UIInteractionMode.cs` — 수정 (이동 로직 교체 + 제한 시야)
- `Game/ReceptionManager.cs` — 신규 (확인 2 에서 yes 면)
- 씬 작업(사용자): 접객 책상에 `Player_Anchor` 자식 배치, `EnterUIModeEffect.anchor` 연결, `UIInteractionMode` 필드(playerRoot / cameraPitchPivot / firstPersonController / characterController / gazeInteractor / cursorInteractor) 배선.

## 확인 필요
1. **제한 시야 입력 방식**:
   - (a) 항상 마우스 이동 = 시야 이동 (FPS 처럼) — 커서 클릭 조작과 충돌
   - (b) **우클릭 누르고 있을 때만** 시야 이동, 평소엔 커서 자유 (추천)
   - (c) 커서가 화면 가장자리에 닿으면 그쪽으로 천천히 팬
2. **`ReceptionManager` 스텁**: 지금 만든다 / 나중에.
3. **시야 클램프 각도**: yaw ±40°, pitch ±25° 기본값 OK?
4. **anchor pitch**: `Player_Anchor` 의 X 회전으로 "약간 내려다보기"까지 담을지, 아니면 항상 수평 정면으로 리셋할지.

## 리스크
- `UIInteractionMode` 는 씬에 아직 안 붙어서(doc 0078 미검증) 지금 이동 로직 교체 비용 작음.
- Cinemachine brain 이 Main Camera 에 실제로 붙어 있는지 씬에서 확인 필요 — 안 붙어 있으면 카메라가 PlayerCameraRoot 를 안 따라옴(별도 처리).

## 결정 (2026-08-28)
1. 시야 = **가장자리 팬** 방식. 기본은 앵커 정면 고정, 커서를 화면 가장자리로 가져가면 그 방향으로 조금 더 돌아봄.
2. `ReceptionManager` 지금 구현.
3. 클램프 yaw ±40 / pitch ±25 로 시작, 플레이 후 조정.
4. `Player_Anchor` 는 **수평 정면만** (yaw 만 사용, pitch/roll 무시).

## 구현 완료 (코드, 2026-08-28)
| 파일 | 내용 |
|---|---|
| `Interaction/Modes/UIInteractionMode.cs` | **재작성**. `cameraTransform` → `playerRoot`(PlayerCapsule) + `cameraPitchPivot`(PlayerCameraRoot). Enter: 저장 → FPC/CC 끔 → 0.3s `Transition` 코루틴으로 `Player_Anchor` 위치 + yaw, pitch 0 으로. 진입 끝나면 `lookActive` on. `EdgeLook()`: 커서 화면 정규화(-1..1), `edgeDeadZone`(0.25) 밖만 재매핑 → `yawRange`/`pitchRange` 목표 각도, `lookLerp` 로 수렴. yaw→playerRoot, pitch→cameraPitchPivot. Exit: 역순 복구. `event Entered/Exited` 추가. |
| `Game/ReceptionManager.cs` | 신규 싱글턴. `UIInteractionMode.Entered/Exited` 구독. 저녁 단계일 때만 `InSession=true` + `OnSessionStarted`. Exit 시 `OnSessionEnded`. 손님 로직은 이 위에 얹음. |

pitch 부호: `EdgeLook` 에 `ponytail:` 주석 — 커서 위→위 보기 가정, 반대면 부호만 반전.

## 사용자 작업 (에디터)
1. 컴파일 확인.
2. 접객 책상 오브젝트에 자식 빈 GameObject **`Player_Anchor`** 배치 — 플레이어가 설 위치 + 바라볼 정면(yaw). 높이는 플레이어 발 기준.
3. 빈 GO + `DayPhaseManager`, `UIInteractionMode`, `ReceptionManager` 컴포넌트.
4. `UIInteractionMode` 필드 배선: playerRoot(PlayerCapsule), cameraPitchPivot(PlayerCameraRoot), firstPersonController, characterController, gazeInteractor, cursorInteractor(PlayerCapsule 에 `CursorInteractor` 추가), exitHint(선택).
5. 책상 `Interactable`: promptType=접객 → 우클릭 "Prompt Type에 맞게 효과 재설정" → `EnterUIModeEffect` + `PhaseCondition(Evening)` 자동. `EnterUIModeEffect.anchor` = `Player_Anchor`.
6. **Cinemachine brain 확인**: Main Camera 에 CinemachineBrain 이 있고 vcam 이 PlayerCameraRoot 를 follow 하는지. 그래야 playerRoot 이동 시 카메라가 붙어옴.
7. 검증: N키로 저녁 → 책상 E → 플레이어가 `Player_Anchor` 로 이동, 화면 그쪽 고정, 마우스 표시, 커서 가장자리에서 살짝 둘러봄, ESC 복귀.

## 수정 (2026-08-28) — 1차는 완전 고정
가장자리 둘러보기를 `edgeLook` bool (기본 false) 뒤로 이동. 지금은 화면 완전 고정으로 검증하고,
동작 확인되면 인스펙터에서 켜기. 진입/앵커 고정/커서 해제 부분은 그대로.

## 상태
2026-08-28 코드 완료 (1차 = 완전 고정). 에디터 배선/검증 대기.
