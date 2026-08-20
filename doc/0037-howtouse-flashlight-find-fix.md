# 0037 - HowToUse_Flashlight 활성화 안 되는 문제 수정

## 날짜
2026-08-20

## 요청 내용 (원문)
> 이제 손전등 획득시 손전등 활성화 관련은 잘되는데 마지막에 추가해달라고한 HowToUse_Flashlight 텍스트도 활성화 해주는게 안되네 확인좀

[[0036-flashlight-runtime-lookup-fix]]의 버그 수정 — 별도 제안서 없이 바로 진행.

## 조사 내용
- `Interactable.ActivatePlayerFlashlight()`에서 `GameObject.Find("HowToUse_Flashlight")`로 찾고 있었는데, **`GameObject.Find`는 비활성화된(inactive) 오브젝트를 찾지 못함**(Unity 공식 동작). `HowToUse_Flashlight`는 [[0036-flashlight-runtime-lookup-fix]]에서 시작 시 `m_IsActive: 0`으로 꺼둔 상태라 애초에 검색 대상에서 빠져있었음 → `hint`가 항상 `null`이라 활성화 코드가 실행되지 않았음.
- 반면 `flashlight`는 `player.transform.Find(...)`(Transform 기반 탐색)를 썼는데, `Transform.Find`는 비활성 자식도 정상적으로 찾기 때문에 그쪽은 문제없이 동작한 것.
- `HowToUse_Flashlight`는 씬 최상위 `Canvas` 오브젝트의 직계 자식(`Assets/Scenes/InGame.unity` 확인: `Canvas`의 RectTransform(fileID 1680860540) 자식 목록에 `HowToUse_Flashlight`의 RectTransform(1007547766) 포함).

## 변경 내용

### `Assets/My/Scripts/Interaction/Interactable.cs`
```diff
-        GameObject hint = GameObject.Find("HowToUse_Flashlight");
-        if (hint != null)
-            hint.SetActive(true);
+        GameObject canvas = GameObject.Find("Canvas");
+        Transform hint = canvas != null ? canvas.transform.Find("HowToUse_Flashlight") : null;
+        if (hint != null)
+            hint.gameObject.SetActive(true);
```
- 항상 활성 상태인 `Canvas`는 `GameObject.Find`로 찾고, 그 밑의 `HowToUse_Flashlight`는 (비활성이어도 찾아지는) `Transform.Find`로 찾도록 변경.

## 결과
계획대로 적용 완료.

## 변경된 파일
- `Assets/My/Scripts/Interaction/Interactable.cs`
