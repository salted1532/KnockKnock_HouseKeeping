# 0023. 낮/밤 전환 스크립트

## 날짜
2026-08-14

## 요청 내용
> 간단한 스카이 박스 + Directional Light을 변경하는 시간 조정 스크립트를 추가하려고하는데 My폴더안에 Scripts안에 스크립트는 작성해야해 해당하는 카테고리의 폴더를 생성해서
> 숫자 1, 2 로 밤과 낮으로 바꾸는 스크립트인데 My -> ingame -> prefabs폴더 안에있는 Morning과 Night 2개의 스카이 박스를 바꾸고 현재 inGame 씬안에있는 Directional Light(Night) 와 (Morning)을 서로 꺼지고 켜지게 하면 되는 스크립트 작성해줘

## 조사 내용
- `Assets/My/Scripts/`는 현재 비어있음(하위 카테고리 폴더 없음)
- 스카이박스 머티리얼 2개 확인: `Assets/My/InGame/Prefabs/Morning/Morning.mat`, `Assets/My/InGame/Prefabs/Night/Night.mat` (둘 다 Skybox/Procedural 셰이더)
- `Assets/Scenes/InGame.unity`에 `Directional Light(Morning)`(현재 `m_IsActive: 0`, 꺼짐), `Directional Light(Night)`(오버라이드 없음 = 기본 켜짐) 프리팹 인스턴스 확인
- `ProjectSettings.asset`의 `activeInputHandler: 1` → **새 Input System 전용**(레거시 `Input.GetKeyDown`은 예외 발생, 못 씀). `com.unity.inputsystem 1.19.0` 설치돼 있으므로 `UnityEngine.InputSystem.Keyboard.current`로 작성해야 함

## 계획된 변경

### 1) 신규 폴더: `Assets/My/Scripts/Environment/`
낮/밤 전환처럼 씬 환경(하늘/조명)을 다루는 스크립트 카테고리로 제안.

### 2) 신규 스크립트: `Assets/My/Scripts/Environment/DayNightSwitcher.cs`
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class DayNightSwitcher : MonoBehaviour
{
    [SerializeField] private Material nightSkybox;
    [SerializeField] private Material morningSkybox;
    [SerializeField] private GameObject nightLight;
    [SerializeField] private GameObject morningLight;

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
    }

    private void SetMorning()
    {
        RenderSettings.skybox = morningSkybox;
        morningLight.SetActive(true);
        nightLight.SetActive(false);
    }
}
```
- `nightSkybox`/`morningSkybox`/`nightLight`/`morningLight` 4개는 Inspector에서 직접 드래그해서 연결(하드코딩 경로/Find 안 씀 — 씬에 배치된 실제 오브젝트를 직접 물리는 게 제일 단순하고 안전함)
- 1 = 밤, 2 = 낮(아침) 매핑 — 요청 문구("1, 2로 밤과 낮") 그대로

## 사용자 확인
"진행 (권장)" 선택 → 위 내용 그대로 적용.

## 변경된 파일
- `Assets/My/Scripts/Environment/DayNightSwitcher.cs` (+ `.meta`, Unity 에디터에서 자동 생성) 신규

## 남은 작업 (사용자가 직접)
씬의 오브젝트에 `DayNightSwitcher` 컴포넌트를 붙이고, Inspector에서 `Night Skybox`/`Morning Skybox`/`Night Light`/`Morning Light` 4개 필드에 각각 `Night.mat`/`Morning.mat`/`Directional Light(Night)`/`Directional Light(Morning)`을 드래그해서 연결.
