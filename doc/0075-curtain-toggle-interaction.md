# 0075 - 커튼 on/off 토글 상호작용

## 요청
상호작용(E) 시 커튼 열림/닫힘 토글. TidyBed와 비슷하지만 **양방향 on/off**.
열린 커튼 / 닫힌 커튼 두 오브젝트 중 해당하는 쪽으로 SetActive 전환.

## 조사
- `Interactable.cs`의 `TidyBed`가 이미 "visual A 끄고 visual B 켜기"(`messyVisual`/`tidyVisual` SetActive) 패턴. 단, 단방향(messy→tidy)이고 되돌리기 없음.
- 커튼은 왕복 토글이 필요 → TidyBed 재사용/수정하지 않고 별도 `Curtain` 케이스 추가 (기존 프리팹 배선 안 건드림).
- 애니메이션 없이 즉시 스왑이면 Door처럼 별도 컴포넌트 불필요 → TidyBed와 동일하게 `Interactable` 내부 인라인 처리.
- ZNS3D에 `curtain_1`, `curtain_2` 프리팹 있음(열림/닫힘 메시로 추정).

## 변경 내용: `Assets/My/Scripts/Interaction/Interactable.cs`

```
 enum InteractionType { ... Door, }   →   { ... Door, Curtain, }

 [Header("TidyBed")]
 [SerializeField] private GameObject messyVisual;
 [SerializeField] private GameObject tidyVisual;
+
+[Header("Curtain (on/off 토글)")]
+[SerializeField] private GameObject curtainOpen;
+[SerializeField] private GameObject curtainClosed;
+private bool curtainIsOpen;

+private void Awake()
+{
+    // 씬에서 켜둔 쪽을 초기 상태로 인식 (강제로 바꾸지 않음)
+    if (curtainOpen != null) curtainIsOpen = curtainOpen.activeSelf;
+}

 switch (type) {
     ...
     case InteractionType.Door:
         if (door != null) door.Toggle();
         break;
+    case InteractionType.Curtain:
+        ToggleCurtain();
+        break;
 }

+private void ToggleCurtain()
+{
+    curtainIsOpen = !curtainIsOpen;
+    if (curtainOpen != null) curtainOpen.SetActive(curtainIsOpen);
+    if (curtainClosed != null) curtainClosed.SetActive(!curtainIsOpen);
+}
```

## 씬/프리팹 세팅 (에디터 — 사용자)
1. 커튼 프리팹 루트에 `curtainOpen`(열린 메시) / `curtainClosed`(닫힌 메시) 자식 2개.
2. 루트에 `Interactable` 추가, `type = Curtain`, 두 필드에 각 자식 드래그.
3. 시작 상태로 둘 중 하나만 활성화(예: 닫힘만 켜기).
4. 레이캐스트 대상 Collider는 **항상 켜져 있는 오브젝트**에 둘 것 — 별도 자식(예: 커튼레일/봉)에 BoxCollider를 두거나, 두 메시 각각에 Collider. (꺼진 오브젝트는 레이캐스트 안 맞음)
5. 아웃라인 원하면 켜져 있는 메시에 `Outline`.

## 알려진 한계 / 후속
- SFX(커튼 스르륵 소리) 미포함. 필요하면 Door처럼 AudioClip 필드 추가 가능.
- 즉시 스왑(애니메이션 없음). 슬라이드 연출 원하면 Door.cs처럼 별도 컴포넌트로.
- 콜라이더를 꺼지는 메시에만 두면 그 상태에서 다시 상호작용 불가 → 세팅 4번 주의.

## 상태
2026-08-27 승인("진행시켜줘") → `Interactable.cs` 적용 완료 (제안과 동일). 에디터 세팅은 사용자 작업.

## 추가 (2026-08-27) — on/off 소리
요청: "on/off 마다 door처럼 소리가 따로 나도록" + "중간에 끊기면 소리 중단되고 변경".
- 필드 추가: `curtainOpenClip`, `curtainCloseClip` (AudioClip).
- `Awake`: 클립 지정 시 AudioSource 자동 추가(Door.cs와 동일 — playOnAwake off, spatialBlend 1). 볼륨/믹서는 컴포넌트에서 조정.
- `ToggleCurtain`: `curtainAudio.Stop(); .clip = 새 클립; .Play()` → 재생 중 다시 토글하면 이전 소리 끊고 교체 (`PlayOneShot` 대신 clip+Play).
- 초안의 `curtainVolume` 필드는 제외(Door와 일관성, AudioSource에서 조정).
