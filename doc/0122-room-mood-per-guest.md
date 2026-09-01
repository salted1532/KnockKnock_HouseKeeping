# 0122 - 배정 손님 컨셉에 맞춘 객실 분위기 (제안)

날짜: 2026-08-31
관련: `doc/0118`(RoomController/새벽 노크), `Docs/RoomController.md`, `Docs/ChangeObjectEffect.md`

## 요청

손님이 배정된 방은 그 손님 컨셉에 맞게 조정. 예: 불은 켜져 있는데 커튼은 닫힘 / 침대 하나는 정리, 하나는 어지럽힘 / 정문 말고 내부문이 열림·닫힘. **랜덤하게, 일단 5명 손님에 맞춰서.**

## 현재 방 조각 (프리팹 실측)

| 오브젝트 | Interactable | 상태 의미 | SetState 가능? |
|---|---|---|---|
| `Light_switch`, `Light_switch (1)` | Toggle | `IsOn` = 켜짐 | ✅ |
| `curtain` | Toggle | `IsOn` = 닫힘 | ✅ |
| 내부문 `Doorway Wall With Door 1/Door` | Toggle + Hinge | `IsOn` = 열림 | ✅ |
| `Bed`, `Bed (1)` | CleanUp (**비토글**) | 자식 `Bed_01`(흐트러짐) / `Bed_02`(정리됨) | ❌ (`SetState` 가 비토글에선 한 방향) → 자식 직접 토글 |

## 설계

### A. `NpcData` — `RoomMood` 블록 추가

```csharp
[Serializable]
public struct RoomMood
{
    public enum Setting { Keep, Off, On, Random }   // Keep = 안 건드림
    [Tooltip("방 불 — On=켜짐")]     public Setting light;
    [Tooltip("커튼 — On=닫힘")]      public Setting curtain;
    [Tooltip("내부문 — On=열림")]    public Setting innerDoor;
    [Range(0f,1f)] [Tooltip("각 침대가 흐트러져 있을 확률")] public float bedMessyChance;
}
// NpcData 필드
[Header("객실 분위기 (배정 시 방에 반영)")]
public RoomMood roomMood;
```

기본값 = 전부 `Keep`, `bedMessyChance` 0 → mood 설정 안 한 손님은 방 그대로.

### B. `RoomController` — 픽스처 참조 + Dawn 봉인 시 적용

```csharp
[Header("객실 분위기 (배정 손님 컨셉)")]
[SerializeField] private Interactable[] lights;
[SerializeField] private Interactable[] curtains;
[SerializeField] private Interactable[] innerDoors;   // 정문 제외
[SerializeField] private Transform[] beds;            // 자식[0]=흐트러짐, [1]=정리됨

// Apply(phase) 의 seal 분기에서, 봉인 직전:
if (seal) ApplyMood(NightGuest);

private void ApplyMood(NpcData npc)
{
    var m = npc.roomMood;
    var rng = new System.Random(npc.id * 73856093);   // 손님별 고정 시드
    Toggle(lights, m.light, rng);
    Toggle(curtains, m.curtain, rng);
    Toggle(innerDoors, m.innerDoor, rng);
    foreach (var b in beds)
    {
        if (b == null || b.childCount < 2) continue;
        bool messy = rng.NextDouble() < m.bedMessyChance;
        b.GetChild(0).gameObject.SetActive(messy);
        b.GetChild(1).gameObject.SetActive(!messy);
    }
}

private static void Toggle(Interactable[] arr, RoomMood.Setting s, System.Random rng)
{
    if (arr == null || s == RoomMood.Setting.Keep) return;
    foreach (var it in arr)
        if (it != null)
            it.SetState(s == RoomMood.Setting.On ||
                       (s == RoomMood.Setting.Random && rng.Next(2) == 0));
}
```

- `SetState` 는 `CanInteract`/`enabled` 무시하고 효과 재생 → 봉인(=enabled false)과 무관하게 동작. mood 를 봉인보다 **먼저** 적용.
- 시드 = `npc.id` 고정 → 같은 손님이면 어느 방이든 같은 배치 (재현 가능). 매 새벽 다르게 하려면 `+ DayNow()` (§확인).
- **아침에 원복 안 함** — mood 는 그대로 유지 (아침 방청소 SYS-01 이 치우는 대상). (§확인)

### C. 5명 mood 값 (컨셉 반영)

| id | 컨셉 | light | curtain | innerDoor | bedMessy |
|---|---|---|---|---|---|
| 1 떠돌이 | 지쳐 곯아떨어짐, 어둡게 | Off | On(닫힘) | Off | 0.5 |
| 2 외판원(불안) | 불 켠 채, 짐 어질러 | On | On | Random | 0.8 |
| 3 거만한 단골 | 불·문 열어둠, 무신경 | On | Random | On(열림) | 0.3 |
| 4 경계심 여성 | 어둠 선호, 깔끔, 다 닫음 | Off | On | Off | 0.0 |
| 5 노인 | 밤에 화장실 왕래, 불 켬 | On | Random | On | 0.5 |

"랜덤" = `Random` 설정 + `bedMessyChance` 굴림 (손님별 시드라 결정적이지만 손님마다 다름).

## 영향 파일

```
Dialogue/NpcData.cs        수정  RoomMood struct + roomMood 필드
Interaction/RoomController.cs  수정  lights/curtains/innerDoors/beds 필드 + ApplyMood (Dawn 봉인 시)
Motel_Room.prefab         픽스처 4종 배선 (전부 프리팹 내부)
Npc_1~5.asset             roomMood 값 입력 (위 표)
Docs/RoomController.md    갱신
```

## 확인 필요

1. **시드**: 손님별 고정 (권장 — 같은 손님 = 같은 방 모습) vs 매 새벽 랜덤 (`+ DayNow()`)
2. **아침 원복**: 안 함 (권장 — 방청소 대상) vs Dawn 끝나면 기본값 복귀
3. **5명 mood 값**: 위 표대로 내가 넣기 vs 전부 `Random` 으로 두고 나중에 조정
4. **내부문**: 지금 방에 내부문 1개(`Doorway Wall With Door 1/Door`) — 화장실 문. 이거 하나만 대상 OK?

## 스킵

- 방마다 다른 배치 (방 = 손님과 무관, mood 는 손님 소유)
- 소품 단위 세밀 배치 (컵, 신발 등) — 픽스처 4종(불/커튼/내부문/침대)만
- 체크아웃 시 방 리셋 — 아침 방청소 시스템(SYS-01) 나올 때

## 상태

제안. 확인 1~4 답변 대기.
