# 0074 - 문 여닫기 상호작용 (A안: 경첩 회전)

## 요청
상호작용(E) 시 문이 열리는 기능. 방식 논의 후 **A안** 채택:
경첩 피벗 오브젝트를 로컬 Y축 기준으로 회전(코루틴 lerp), 기존 `Interactable`에 `Door` 타입 추가로 트리거.
(스왑 방식·HingeJoint·Animator 등은 0073 대화에서 검토 후 제외 — 이유는 대화 로그 참조.)

## 조사
- `Assets/My/Scripts/Interaction/Interactable.cs`: `InteractionType` enum + `Interact(Vector3?)` switch 구조. `Flashlight`/`Push`처럼 전용 enum 타입 추가하는 것이 기존 패턴.
- `Assets/My/Scripts/Player/InteractionOutline.cs`: 카메라 중앙 레이캐스트 → `GetComponentInParent<Interactable>()` → E 키에 `Interact(hit.point)`. `type`으로 분기하는 코드는 `Interactable` 밖에 없음 → enum 값 추가는 자기완결적.
- 트윈 라이브러리(DOTween 등) 없음, 프로젝트 스크립트에 `Animator` 사용 없음 → 코루틴 회전이 코드베이스 스타일과 일치.

## 변경 내용

### 신규: `Assets/My/Scripts/Interaction/Door.cs`
경첩 피벗에 붙이는 스크립트. 문 메시를 자식으로 두면 피벗이 경첩 모서리에 옴.
- 필드(노브): `openAngle`(기본 90, 음수면 반대 스윙) / `openTime`(0.6s) / `ease`(AnimationCurve, 기본 EaseInOut) / `startOpen` / `openClip`,`closeClip`(선택 SFX)
- `Awake`: 현재 localRotation을 닫힘값으로 저장, `openRot = closedRot * Euler(0, openAngle, 0)`. SFX 클립 지정 시 AudioSource 자동 추가(3D).
- `Toggle()` / `SetOpen(bool)` public. `SetOpen`은 코루틴 `Swing`으로 `Quaternion.Slerp(from, target, ease.Evaluate(t/openTime))`. 스윙 중 재호출 시 기존 코루틴 stop 후 반대로 → 중간에 방향 전환 가능.
- `IsOpen` 프로퍼티 노출.

### 변경: `Assets/My/Scripts/Interaction/Interactable.cs`

```
 enum InteractionType { ... Push, }        →   { ... Push, Door, }

 [Header("Push ...")] ...
 [SerializeField] private float rotationForce = 2f;
+
+[Header("Door")]
+[SerializeField] private Door door;

 switch (type) {
     ...
     case InteractionType.Push:
         PushAwayFromPlayer(hitPoint ?? transform.position);
         break;
+    case InteractionType.Door:
+        if (door != null) door.Toggle();
+        break;
 }
```

## 씬/프리팹 세팅 (에디터 작업 — 사용자)
1. 문 프리팹: 루트(빈 GO) 밑에 `Hinge`(빈 GO, 위치를 경첩 모서리로) → 그 밑에 문 메시.
2. `Hinge`에 `Door.cs` 추가. `openAngle` 방향 확인(안쪽/바깥쪽).
3. 루트(또는 Hinge)에 `Interactable` 추가, `type = Door`, `door` 필드에 `Door` 컴포넌트 드래그.
4. 문 메시에 Collider(레이캐스트 대상). 아웃라인 원하면 `Outline` 컴포넌트.
5. (선택) `openClip`/`closeClip` 지정.

## 알려진 한계 / 후속
- `InteractionOutline`이 `Interact()` 후 `currentInteractable`을 즉시 비움 → 문을 다시 닫으려면 시선을 뗐다 다시 봐야 함. 기존 모든 상호작용과 동일 동작이라 이번 범위에선 그대로 둠.
- 잠금/열쇠, NavMesh(몬스터 통행)는 별도 요청 시 추가.
- `Door.cs.meta`는 Unity 에디터가 임포트 시 생성.

## 상태
2026-08-27 승인("A안으로 ... 진행해줘") → 스크립트 적용 완료. 에디터 세팅은 사용자 작업.

## 추가 (2026-08-27) — 스윙 중단 시 소리 교체
요청: "on/off 가 중간에 끊기면 소리가 중단되고 변경되도록".
- `Door.SetOpen`: 오디오 재생을 `PlayOneShot` → `audioSource.Stop(); .clip = 새 클립; .Play()` 로 변경.
- 스윙 도중 반대로 토글하면 코루틴 stop + 이전 소리 즉시 중단 + 새 방향 소리 재생.
