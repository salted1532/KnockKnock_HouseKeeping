# 0089 - "걸기" 프롬프트 카테고리 추가 (HookEffect 승격)

## 요청
> 이거 hook도 "걸기"나 "저장" 등으로 상호작용 프롬포트 선택란에 추가해줘 해당하는 스크립트 추가도 해주고

지금까지 `HookEffect`는 [[project_interaction-system-redesign]] 관례대로 "특별한 작동"이라 `Interactable`의 큰 스위치를 안 건드리고 수동으로만 붙이는 커스텀 효과였음(doc/0087). 이번엔 반대로 **정식 프롬프트 카테고리로 승격**해서 다른 것들(줍기/밀기 등)처럼 "재설정" 메뉴로 자동 추가되게 해달라는 요청.

## 조사
- `Interactable.cs`의 `InteractionPrompt` enum은 Unity가 **선언 순서(int 인덱스)** 로 직렬화함. 현재 프리팹들이 쓰는 값 확인(`grep promptType:`): 0~4, 6~8 사용 중, **9(직접입력)·그 이상은 아직 아무도 안 씀**.
- 따라서 새 값을 중간에 끼워넣으면(예: `접객`과 `직접입력` 사이) 그 뒤 값들의 인덱스가 밀려서 기존에 저장된 `promptType` 숫자의 의미가 바뀌는 위험이 있음 → **반드시 enum 맨 끝에 추가**해야 기존 프리팹 안 깨짐.
- 이름은 "걸기"로 함 — "저장"은 인벤토리/창고 저장 같은 다른 기능과 헷갈릴 수 있고, "걸기"가 실제 동작(고리에 걸다)과 더 직접적으로 맞음.

## 계획

### 1. `Interactable.cs`
```csharp
public enum InteractionPrompt { 상호작용, 여닫기, 켜고끄기, 줍기, 사용, 조사, 정리하기, 밀기, 접객, 직접입력, 걸기 }
```
(맨 끝에 추가 — 기존 값 인덱스 안 건드림)

`ManagedEffects`에 `HookEffect` 추가:
```csharp
static readonly System.Type[] ManagedEffects =
{
    typeof(SfxEffect), typeof(ChangeObjectEffect), typeof(HingeEffect),
    typeof(PushEffect), typeof(PickupEffect), typeof(SpawnObjectEffect), typeof(EnterUIModeEffect),
    typeof(HookEffect),
};
```

`SyncEffectsToPrompt()` 스위치에 케이스 추가:
```csharp
case InteractionPrompt.걸기: wanted.Add(typeof(HookEffect)); break;
```
(다른 카테고리처럼 `SfxEffect`는 `wanted` 초기값에 이미 포함되므로 별도 추가 불필요)

`EnsureRigidbody()` 호출은 `줍기`에서만이라 그대로 둠 — 고리 자체는 물리 아이템이 아니라서 Rigidbody 불필요.

### 2. `Key_hook.prefab`
`promptType: 0`(상호작용) → `promptType: 10`(걸기)로 변경. 나머지(HookEffect·SfxEffect·Outline·AudioSource·콜라이더·레이어)는 이미 다 있으니 그대로 유지.

### 3. `Docs/InteractionSystem.md`
- 표에 `걸기` 행 추가.
- "관리되는 효과 목록"에 `HookEffect` 추가.
- 스크립트 목록의 `HookEffect` 설명에서 "managed 목록 밖 커스텀 효과" 문구 제거(이제 관리 대상임).

### 4. `Docs/HookEffect.md`
"특정 프롬프트 카테고리에 안 맞는 특수 동작" 문구를 "걸기 프롬프트 표준 효과"로 수정.

## 리스크
- 낮음. enum 끝에 추가라 기존 데이터 안전. `Key_hook` 하나만 promptType 값 변경.

## 결과 (2026-08-28, 승인 후 적용)
계획대로:
- `Interactable.cs`: `InteractionPrompt`에 `걸기` 를 **맨 끝**에 추가(인덱스 10), `ManagedEffects`에 `HookEffect` 추가, `SyncEffectsToPrompt()` 스위치에 `case 걸기: wanted.Add(HookEffect)` 추가.
- `Key_hook.prefab`: `promptType: 0` → `promptType: 10`(걸기).
- `Docs/InteractionSystem.md`(표·managed 목록·스크립트 설명) · `Docs/HookEffect.md` 갱신.

## 검증
- 정적 확인만 완료. 기존 프리팹 19개 중 `promptType` 9(직접입력) 이상 쓰는 곳 없음 확인(`grep promptType:`) → 인덱스 밀림 없음.
- Unity에서 `Key_hook` 인스펙터에 `걸기` 프롬프트로 표시되는지, 우클릭 "재설정"으로 `HookEffect`가 관리되는지 확인 필요.

## 상태
2026-08-28 완료.
