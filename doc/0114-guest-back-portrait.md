# 0114 - 손님 뒷모습 스프라이트 + 퇴장 시 전환

날짜: 2026-08-30
관련: `doc/0110`(손님 회전 고정), `doc/0113`(승인 흐름), `Docs/DialogueSystem.md`

## 요청 (원문)

> 접객의 뒷모습 스프라이트를 연결할수 있도록 추가해줘. 그리고 대화가 끝나고 나갈때는
> 뒷모습으로 변경해서 나가도록 해줄래. image폴더 안에 숙박객 안에 접객_뒷모습을 추가했어.
> 해당하는걸 현재 npc들에게도 연결해줘.

## 1. 현황

- 손님 회전은 항상 `faceYaw = -180` 고정(`doc/0110`) → "돌아서 나감"을 회전으로 표현 못 함 → **스프라이트 스왑**으로.
- `GuestView` : `body`(SpriteRenderer) 에 `npc.Portrait(Expression)` 적용 + `worldHeight` 로 균일 스케일 + 발/콜라이더 정렬.
- `NpcData` : `neutralPortrait` / `angryPortrait`.
- 신규 에셋 `Assets/My/image/숙박객/접객_뒷모습.png` (guid `1f1a2e2f575144d46bd033c99b611abc`, spriteMode Multiple, 서브스프라이트 `접객_뒷모습_0` 1개).
- NPC 5개(`Npc_`~`Npc_5`) 전부 공용 `접객.png`/`접객화남.png` 사용.

## 2. 설계

### A. `NpcData` — 필드 추가

```csharp
[Header("초상화 (말풍선 표정)")]
public Sprite neutralPortrait;
public Sprite angryPortrait;
[Tooltip("퇴장(나갈 때) 뒷모습. 비면 정면 유지")]
public Sprite backPortrait;
```

### B. `GuestView` — `ShowBack()` 추가

`SetExpression` 의 스프라이트 적용+스케일 로직을 `ApplySprite(Sprite)` 로 추출:

```csharp
public void ShowBack()
{
    if (npc != null && npc.backPortrait != null) ApplySprite(npc.backPortrait);
}
```

### C. `ReceptionManager.GuestQueue` — 퇴장 직전 호출

손님이 카운터를 떠나는 3(→4) 지점 모두에서 `view?.ShowBack()` 후 `WalkThrough`:
- `visitorOnly` → exitPath
- 첫 대화 거절 → exitPath
- 재대화 거절 → exitPath
- 열쇠 승인 → roomPath

`view?.Apply(npc)` 가 다음 손님마다 정면(Neutral)으로 리셋하므로 재활용 문제 없음.

### D. 에셋 연결

`Npc_`, `Npc_2`~`Npc_5` 의 `backPortrait` = `접객_뒷모습_0` 스프라이트 (uloop 로 `AssetDatabase` + `SerializedObject`, fileID 수기 계산 안 함).

## 3. 영향 파일

```
Assets/My/Scripts/Dialogue/NpcData.cs        backPortrait 필드
Assets/My/Scripts/Dialogue/GuestView.cs      ApplySprite 추출 + ShowBack()
Assets/My/Scripts/Game/ReceptionManager.cs   퇴장 직전 view.ShowBack()
Assets/My/Scripts/Dialogue/NPC_Data/Npc_*.asset  (5개) backPortrait 연결
Docs/DialogueSystem.md                        갱신
```

## 4. 구현 완료 (2026-08-30, 확인: 카운터를 떠나는 모든 경우)

| 파일 | 내용 |
|---|---|
| `Dialogue/NpcData.cs` | `public Sprite backPortrait;` 추가 (초상화 헤더) |
| `Dialogue/GuestView.cs` | `SetExpression` 의 스프라이트+스케일+발/콜라이더 정렬 로직을 `ApplySprite(Sprite)` 로 추출. `public void ShowBack()` → `backPortrait` 있으면 `ApplySprite` |
| `Game/ReceptionManager.cs` | `visitorOnly` / 첫 거절 / 재대화 거절 → `exitPath` 직전, 열쇠 승인 → "checkin" 대사 후 `roomPath` 직전에 `view?.ShowBack()`. 다음 손님은 `view.Apply` 가 정면으로 리셋 |
| `NPC_Data/Npc_`~`Npc_5.asset` (5개) | `backPortrait` = `접객_뒷모습_0` (`Assets/My/image/숙박객/접객_뒷모습.png`) |
| `Docs/DialogueSystem.md` | 갱신 |

### 검증
- `uloop compile` : Success, Error 0, Warning 0.
- Play 모드: `GuestView.Apply(Npc_3)` → body `접객_0` (scale 0.27) → `ShowBack()` → `접객_뒷모습_0` (scale 0.31, `worldHeight` 재적용) ✓
- 5개 에셋 `backPortrait` 연결 확인 (`Npc_3.asset` → guid `1f1a2e2f…`, fileID `1970465886`) ✓
- 실제 접객 세션에서 퇴장 타이밍 전환은 인게임 확인 요망.

## 상태

2026-08-30 구현 완료. 인게임 확인 대기.
