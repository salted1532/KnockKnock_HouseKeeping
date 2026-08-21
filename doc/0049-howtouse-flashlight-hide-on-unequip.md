# 0049 - 손전등 안 들었을 때 HowToUse_Flashlight 비활성화

## 요청 내용
> 손전등을 들고 있지 않으면 HowToUse_Flashlight 이 비활성화 되도록 해줘
> 아이템으로 먹었다고 해서 계속 나와있는게 아니라

## 조사 내용
`Interactable.cs`의 `ActivatePlayerFlashlight()`에서 손전등을 주울 때 `Canvas/HowToUse_Flashlight`를 무조건 `SetActive(true)`만 하고, 이후 슬롯 전환(`InventorySystem.SelectSlot`)이나 던지기(`ThrowActiveItem`)에서 다시 꺼주는 곳이 없어 한 번 주우면 계속 떠 있었음.

`InventorySystem.cs`는 이미 `equipTargets[]`/`activeSlot`으로 "지금 손에 든 것"을 관리하고 있어, 여기서 힌트 표시를 같이 관리하는 게 자연스러움.

## 계획된 변경 (승인 후 적용)

**`InventorySystem.cs`**
- `isFlashlightSlot[SlotCount]` 추가, `AddItem(icon, equipTarget, bool isFlashlight = false)`로 확장
- `AddItem` / `SelectSlot` / `ThrowActiveItem` 끝에서 `UpdateFlashlightHint()` 호출
- `UpdateFlashlightHint()`: 활성 슬롯이 손전등이고 실제로 들려 있을 때만 `Canvas/HowToUse_Flashlight` 활성화, 아니면 비활성화

**`Interactable.cs`**
- `ActivatePlayerFlashlight()`: `AddItem(itemIcon, flashlight.gameObject, isFlashlight: true)` 호출로 변경
- 직접 힌트를 켜던 코드 제거 (InventorySystem이 중앙 관리)

씬 파일은 변경 없음 (기존과 동일하게 `GameObject.Find("Canvas")`로 참조).

## 적용 결과
계획대로 적용함:
- `Assets/My/Scripts/Inventory/InventorySystem.cs`: `isFlashlightSlot[]`, `AddItem` 파라미터 확장, `UpdateFlashlightHint()` 추가 및 `AddItem`/`SelectSlot`/`ThrowActiveItem`에서 호출
- `Assets/My/Scripts/Interaction/Interactable.cs`: `ActivatePlayerFlashlight()`에서 힌트 직접 제어하던 코드 제거, `AddItem` 호출에 `isFlashlight: true` 전달

Unity 에디터로 직접 실행해 테스트하진 못함 — 에디터에서 열어서 손전등 줍기/슬롯 전환/F키로 던지기 각각 상황에서 힌트가 의도대로 켜지고 꺼지는지 확인 필요.

## 요약 / 남은 작업
- 변경 파일: `Assets/My/Scripts/Inventory/InventorySystem.cs`, `Assets/My/Scripts/Interaction/Interactable.cs`
- 씬/프리팹 변경 없음
