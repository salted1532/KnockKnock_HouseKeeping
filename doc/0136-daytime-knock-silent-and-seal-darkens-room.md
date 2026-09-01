# 0136 - 아침 노크 무응답 통일 + 잠금 시 커튼·전등 off

날짜: 2026-09-02
관련: `doc/0132`(체크아웃/아침 청소·새벽 아닌 노크 거절), `doc/0131`(새벽 refuse 노드), `doc/0122`(객실 분위기 — 미구현), `Docs/RoomController.md`·`Docs/KnockEffect.md`

## 요청 (원문)

> 여성 npc에 경우 아침에 문을 노크하면 새벽에 나오는 대사 그대로 나오는데 다른 대사가 나오든 거만한 남성처럼 그냥 응답이 없도록 해줘
> 그리고 각 방이 잠기면 커튼도 off로 바뀌도록 해주고 방에 전등 light switch들도 off로 바뀌도록 해줘 방 안을 못 보도록

## A. 아침·점심 노크 = 무응답 (대사 없음)

**증상**: npc 4(경계심 여성)는 `Dawn/refuse` 노드("...어두워지면 두드리지 말랬죠...")가 있어서
아침에 노크해도 그 새벽 대사가 그대로 재생됨 — 낮 시간대엔 문맥이 안 맞음.

**수정** — `KnockEffect` 거절 분기: `refuse` 노드 재생을 **`isDawn` 일 때만**.

| 시간대 | 거절 시 |
|---|---|
| 새벽 | `ScreenMessage` 문구 + (`Dawn/refuse` 노드 있으면) 문 너머 한 마디 — 기존 |
| 아침·점심·저녁 | `ScreenMessage` 문구만 + `refuseReadTime`(2.5s) 대기 → 물러남. **대사 없음** |

→ npc 4 아침 노크 = npc 3(거만, refuse 노드 없음) 아침 노크와 동일: "노크가 거절됐다" 류 화면 문구만.

```csharp
// 기존: if (dawnPanel != null) { SayNode(... "refuse" ...); }
// 변경: if (isDawn && dawnPanel != null) { SayNode(... "refuse" ...); }
//       else { refuseReadTime 만큼 대기 }
```

## B. 잠금 시 커튼 닫고 전등 끄기 (방 안 안 보이게)

`RoomController` 에 배열 2개 추가. 잠금(`seal`) 시 상태를 강제하고 상호작용도 끔.

```csharp
[SerializeField] private Interactable[] curtains;   // 잠금 시 SetState(true) = 닫힘
[SerializeField] private Interactable[] lights;     // 잠금 시 SetState(false) = 꺼짐

// Apply(phase) seal 처리부:
foreach (it in curtains) if (it) { if (seal) it.SetState(true);  it.enabled = !seal; }
foreach (it in lights)   if (it) { if (seal) it.SetState(false); it.enabled = !seal; }
```

- `curtain`: toggle, `IsOn == true` = 닫힘(`curtain_1`). 기본 열림. 잠금 → `SetState(true)` 닫기.
- `Light_switch` ×2: toggle, `IsOn == true` = `Lamps_ON`. 기본 꺼짐. 잠금 → `SetState(false)` 끄기.
- `SetState` 는 `CanInteract`/`enabled` 무시하고 효과 재생 → `enabled=false` 와 무관하게 동작. 이미 목표 상태면 no-op(연출 1회만).
- **잠금 해제 시 원복 안 함** — 아침 청소 창에서 플레이어가 열거나, 손님이 다시 조정. `doc/0122`(mood)/방 리셋이 들어오면 그쪽에서.

### 프리팹 배선 (`Motel_Room.prefab`, uloop 저장)

`sealedInteractables` 에서 `curtain`·`Light_switch`·`Light_switch (1)` 제거 → `curtains`/`lights` 로 이동.
`sealedInteractables` 는 이제 `[Bed, Bed (1), Phone, Door]`.

## 검증 (플레이)

- npc3 를 101호 체크인 → 커튼 열고 불 켠 상태에서 Dawn 전환 → `curtain.IsOn=True`(닫힘), `Light_switch.IsOn=False`(꺼짐), 둘 다 `enabled=False`. ✅
- `KnockEffect` 컴파일 Error 0. (아침 노크 무응답은 로직 확인 — 풀 플레이 미검증)

## 영향 파일

```
Interaction/Effects/KnockEffect.cs   수정  refuse 노드 재생을 isDawn 조건으로
Interaction/RoomController.cs        수정  curtains[]/lights[] + seal 시 SetState
InGame/Prefabs/MotelRoom/Motel_Room.prefab  배선  curtains/lights 이동
Docs/RoomController.md · Docs/KnockEffect.md  갱신
```

## 상태

2026-09-02 코드 + 프리팹 배선 + 컴파일(Error 0) + 잠금 시 커튼/전등 플레이 검증 완료.
