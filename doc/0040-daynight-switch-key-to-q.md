# 0040 - 밤/낮 전환 키를 Q로 변경 (인벤토리 키 충돌 해소)

## 날짜
2026-08-20

## 요청 내용 (원문)
> 현재 밤 낮 변경하는거랑 인벤토리랑 키가 겹치는데 밤 낮 스위치 키를 Q로 변경해줘 Q누르면 밤낮 변경되도록

간단한 키 리매핑이라 별도 제안서 없이 바로 진행.

## 조사 내용
- 밤/낮 전환은 두 스크립트가 각각 독립적으로 숫자키 1/2를 감지해서 처리하고 있었음:
  - `Assets/My/Scripts/Audio/SoundManager.cs` — 앰비언트 사운드(night/morning) 전환
  - `Assets/My/Scripts/Environment/DayNightSwitcher.cs` — 스카이박스/조명/볼륨 프로필 전환
- `Assets/My/Scripts/Inventory/InventorySystem.cs`가 숫자키 1~5를 슬롯 선택에 쓰고 있어서 겹침 발생.
- "Q누르면 밤낮 변경"(둘 중 하나를 지정하는 게 아니라 토글) 요청이라, 두 스크립트 모두 내부에 `isNight` 상태를 두고 Q키 입력마다 상태를 뒤집어 그에 맞는 쪽을 호출하는 방식으로 변경. 두 스크립트가 각자 독립적으로 토글하지만 시작 상태(`isNight = true`, 기존 `SoundManager.Start()`가 night로 시작하던 것과 동일)가 같아서 항상 같이 맞물려 전환됨.

## 변경 내용

### `Assets/My/Scripts/Audio/SoundManager.cs`
```diff
     private int woodLayer, concreteLayer, metalLayer, grassLayer;
+    private bool isNight = true;
...
-        if (keyboard.digit1Key.wasPressedThisFrame)
-            Play(nightClip);
-        else if (keyboard.digit2Key.wasPressedThisFrame)
-            Play(morningClip);
+        if (keyboard.qKey.wasPressedThisFrame)
+        {
+            isNight = !isNight;
+            Play(isNight ? nightClip : morningClip);
+        }
```

### `Assets/My/Scripts/Environment/DayNightSwitcher.cs`
```diff
+    private bool isNight = true;
+
     private void Update()
     {
         var keyboard = Keyboard.current;
         if (keyboard == null) return;

-        if (keyboard.digit1Key.wasPressedThisFrame)
-            SetNight();
-        else if (keyboard.digit2Key.wasPressedThisFrame)
-            SetMorning();
+        if (keyboard.qKey.wasPressedThisFrame)
+        {
+            isNight = !isNight;
+            if (isNight) SetNight();
+            else SetMorning();
+        }
     }
```

## 결과
계획대로 적용 완료. 이제 Q 한 키로 밤↔낮이 토글되고, 인벤토리 숫자키(1~5)와 겹치지 않음.

## 변경된 파일
- `Assets/My/Scripts/Audio/SoundManager.cs`
- `Assets/My/Scripts/Environment/DayNightSwitcher.cs`
