# 0077 - 상호작용 레이캐스트 레이어 동작 (Q&A, 코드 변경 없음)

## 질문
상호작용 레이가 다른 default 콜라이더를 뚫고도 작동될 수 있나?

## 답: 그렇다 (의도된 설정)
`InteractionOutline` (씬 `Assets/Scenes/InGame.unity`):
- `interactDistance: 3`
- `interactMask.m_Bits: 2048` → **레이어 11 "Interaction" 만**
- `Physics.Raycast(ray, out hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore)` — 단일 히트(가장 가까운 것), RaycastAll 아님

`Physics.Raycast`는 레이어 마스크에 없는 콜라이더를 **감지도, 차단도 안 함** → Default/Wood/Concrete/Metal 등 벽·가구·소품 콜라이더는 레이가 그냥 통과. "Interaction"(11) 레이어 콜라이더에서만 멈춤. Trigger는 `QueryTriggerInteraction.Ignore`로 통과.

프로젝트 레이어: 0 Default, 1 TransparentFX, 2 Ignore Raycast, 4 Water, 5 UI, 6 PostProcess, 7 Wood, 8 Concrete, 9 Metal, 10 Grass, **11 Interaction**.

## 함의
- 필수: 상호작용 대상 콜라이더는 **레이어 11**이어야 감지됨. (커튼 루트 = 11 확인됨. Door 메쉬 콜라이더도 11로 설정 필요.)
- 장점: 앞에 잡소품 콜라이더가 걸쳐도 상호작용 가능.
- 단점: **얇은 벽 너머로도 상호작용 가능** (콜라이더가 11 레이어 + 3m 이내 + 화면 중앙). 가림 체크가 필요하면 환경 레이어로 별도 레이를 쏴서 시야 확보 확인하는 로직 추가해야 함 — 현재 없음. 필요 시 별도 요청.

## 후속: 벽 너머 상호작용 차단 (2026-08-27, "벽 너머 상호작용 막아줘")

### 변경: `Assets/My/Scripts/Player/InteractionOutline.cs`
레이어 11 히트 성공 후, **같은 레이로 2차 레이캐스트**해서 대상보다 앞에 막는 게 있으면 무시.

```
 if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
 {
+    const int ignoreRaycastLayer = 2;
+    int occlusionMask = ~interactMask.value & ~(1 << ignoreRaycastLayer);
+    if (!Physics.Raycast(ray, hit.distance - 0.01f, occlusionMask, QueryTriggerInteraction.Ignore))
+    {
         hitOutline = hit.collider.GetComponentInParent<Outline>();
         hitInteractable = hit.collider.GetComponentInParent<Interactable>();
+    }
 }
```

- `occlusionMask` = Interaction(11)·Ignore Raycast(2) 빼고 전부. 벽/가구(Default/Concrete/Wood/Metal/Grass 등)가 사이에 있으면 차단.
- `hit.distance - 0.01f`: 대상 표면 살짝 앞까지만 검사 → 자기 콜라이더 자기충돌 방지.
- 차단되면 `hitOutline`/`hitInteractable` 둘 다 null → 기존 로직이 외곽선·프롬프트 끄고 E 입력도 무시.
- 별도 serialize 필드 없음(자동 계산). 특정 레이어를 투과시키고 싶으면(예: 유리) 그때 필드 추가.

### 상태
2026-08-27 적용 완료.
