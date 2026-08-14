# 0024. DayNightSwitcher에 Volume Profile 전환 추가

## 날짜
2026-08-14

## 요청 내용
> 낮과 밤에 맞춰서 볼륨 프로필도 변경되도록 할수 있어?

## 조사 내용
- `Assets/Scenes/InGame.unity`에 `Global Volume` 오브젝트, `Volume` 컴포넌트 하나 존재(`m_IsGlobal: 1`), 현재 `sharedProfile`이 `Night-VolumeProfile.asset`으로 지정돼 있음(작업 중 임시로 물려둔 상태로 보임)
- `Day-VolumeProfile.asset`은 아직 빈 프로필(오버라이드 없음, 0021에서 만든 그대로)
- 기존 `DayNightSwitcher.cs`(스카이박스/라이트)와 같은 패턴으로 — 블렌딩 없이 즉시 교체(`sharedProfile` 직접 스왑) 방식 제안. 이전에 논의한 "방법 2: Profile 직접 교체"와 동일한 방식이라 스카이박스/라이트 전환과 타이밍이 자연스럽게 맞음

## 계획된 변경
`Assets/My/Scripts/Environment/DayNightSwitcher.cs`에 필드 3개 추가, Set 함수에 한 줄씩 추가:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class DayNightSwitcher : MonoBehaviour
{
    [SerializeField] private Material nightSkybox;
    [SerializeField] private Material morningSkybox;
    [SerializeField] private GameObject nightLight;
    [SerializeField] private GameObject morningLight;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private VolumeProfile nightProfile;
    [SerializeField] private VolumeProfile morningProfile;

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            SetNight();
        else if (keyboard.digit2Key.wasPressedThisFrame)
            SetMorning();
    }

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
}
```

## 사용자 확인
"진행 (권장)" 선택 → 위 내용 그대로 적용.

## 변경된 파일
- `Assets/My/Scripts/Environment/DayNightSwitcher.cs` (필드 3개 + Set 함수 2곳 수정)

## 남은 작업 (사용자가 직접)
Inspector에서 `Global Volume` 필드에 씬의 `Global Volume` 오브젝트, `Night Profile`/`Morning Profile` 필드에 `Night-VolumeProfile.asset`/`Day-VolumeProfile.asset` 드래그.
