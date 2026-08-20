# 0034 - 상호작용 프롬프트 텍스트(Interaction_Text) 표시

## 날짜
2026-08-20

## 요청 내용 (원문)
> Canvas안에다가 Interaction_Text라는 텍스트를 추가했는데 상호작용이 일어나고 있을때만 해당 텍스트가 보이도록 해줘

## 조사 내용
- `Assets/My/Scripts/Player/InteractionOutline.cs`는 이전 세션 이후 사용자가 직접 `Interactable`/E키 입력 로직을 추가해둔 상태(`Assets/My/Scripts/Interaction/Interactable.cs`, `InteractionType` enum으로 Pickup/TidyBed/Generic 분기). 매 프레임 화면 중앙 레이캐스트로 `Interactable` 컴포넌트를 찾아 `currentInteractable`에 저장하고, E키 입력 시 `Interact()` 호출.
- `Interaction_Text`는 씬에 아직 저장되지 않아(`Assets/Scenes/InGame.unity`에서 검색 안 됨) 직접 참조를 하드코딩할 수 없음 → 기존 `playerCamera`처럼 인스펙터에서 직접 연결하는 필드로 노출.
- "상호작용이 일어나고 있을때만"은 E키를 누르는 그 찰나가 아니라, 상호작용 가능한 물건을 쳐다보고 있는 동안(아웃라인이 켜지는 시점과 동일)을 의미하는 것으로 해석 — "F키를 눌러 상호작용" 같은 프롬프트 UI 용도로 판단.

## 계획

### `Assets/My/Scripts/Player/InteractionOutline.cs` 수정
```diff
     [SerializeField] private float interactDistance = 3f;
     [SerializeField] private LayerMask interactMask = ~0;
     [SerializeField] private Camera playerCamera;
+    [SerializeField] private GameObject interactionText;

     private Outline currentOutline;
     private Interactable currentInteractable;
@@
         if (hitOutline != currentOutline)
         {
             if (currentOutline != null)
                 currentOutline.enabled = false;
             if (hitOutline != null)
                 hitOutline.enabled = true;
             currentOutline = hitOutline;
         }

-        currentInteractable = hitInteractable;
+        if (hitInteractable != currentInteractable)
+        {
+            currentInteractable = hitInteractable;
+            if (interactionText != null)
+                interactionText.SetActive(currentInteractable != null);
+        }
```
- 대상이 바뀔 때만 `SetActive` 호출(매 프레임 불필요한 호출 방지), 아웃라인 켜고 끄는 것과 동일한 타이밍에 텍스트도 같이 켜지고 꺼짐.

## 결과
승인 후 계획대로 적용 완료.

## 남은 작업 (씬 작업, 사용자 진행)
1. `InteractionOutline` 컴포넌트(PlayerCapsule) 인스펙터에서 `Interaction Text` 필드에 Canvas의 `Interaction_Text` 오브젝트 연결
2. `Interaction_Text`의 초기 활성 상태는 꺼둘 것(비활성) — 상호작용 대상이 감지될 때 스크립트가 켜줌

## 변경된 파일
- `Assets/My/Scripts/Player/InteractionOutline.cs` — `interactionText` 필드 추가, 상호작용 대상이 바뀔 때 활성/비활성 토글
