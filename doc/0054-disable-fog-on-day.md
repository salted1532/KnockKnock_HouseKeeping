# 0054 - 낮 전환 시 Fog 비활성화

## 요청
"낮으로 변경시 envirment에 fog를 비활성화 해줄수 있어?"

## 조사
- `Assets/My/Scripts/Environment/DayNightSwitcher.cs`가 Q키로 낮/밤을 토글.
- `SetNight()` / `SetMorning()`에서 스카이박스, 라이트, Volume 프로필만 전환하고 `RenderSettings.fog`는 건드리지 않음 (Lighting 창의 Environment > Fog 설정, `RenderSettings.fog` bool로 제어됨).
- 밤에는 기존 씬 설정값(현재 켜져 있다고 가정) 유지, 낮으로 전환 시에만 끄고 다시 밤으로 가면 원래 상태로 복원하는 방식으로 제안.

## 계획된 변경
`Assets/My/Scripts/Environment/DayNightSwitcher.cs`

```csharp
    private void SetNight()
    {
        RenderSettings.skybox = nightSkybox;
        nightLight.SetActive(true);
        morningLight.SetActive(false);
        globalVolume.sharedProfile = nightProfile;
    }

    private void SetMorning()
    {
        RenderSettings.skybox = morningSkybox;
        morningLight.SetActive(true);
        nightLight.SetActive(false);
        globalVolume.sharedProfile = morningProfile;
    }
```
→

```csharp
    private void SetNight()
    {
        RenderSettings.skybox = nightSkybox;
        nightLight.SetActive(true);
        morningLight.SetActive(false);
        globalVolume.sharedProfile = nightProfile;
        RenderSettings.fog = true;
    }

    private void SetMorning()
    {
        RenderSettings.skybox = morningSkybox;
        morningLight.SetActive(true);
        nightLight.SetActive(false);
        globalVolume.sharedProfile = morningProfile;
        RenderSettings.fog = false;
    }
```

## 요약/남은 작업
승인 완료, 위 diff 그대로 적용함.

## 변경된 파일
- `Assets/My/Scripts/Environment/DayNightSwitcher.cs`
