# 0026 - 간단한 SoundManager 제안

## 날짜
2026-08-17

## 요청 내용
> 간단한 사운드매니저 하나 만들어줘 인스펙터로 클립하나 넣으면 무한 재생되도록

**후속 요청 (수정):**
> 1,2 에 맞춰서 2개의 클립을 바꿔가며 틀도록 했으면 좋겠어 밤낮에 맞춰

→ 기존 `DayNightSwitcher.cs`가 숫자키 1(밤)/2(낮)로 스카이박스·조명을 전환하는 것과 동일한 키 입력을 사용해, 클립도 밤/낮에 맞춰 전환하도록 변경.

## 조사 내용
- 기존 스크립트 컨벤션 확인: `Assets/My/Scripts/<카테고리>/<이름>.cs` (예: `Assets/My/Scripts/Environment/DayNightSwitcher.cs`)
- `DayNightSwitcher.cs`: `Keyboard.current.digit1Key` → 밤, `digit2Key` → 낮 전환. 동일 패턴 재사용
- Unity `AudioSource.loop`가 무한 재생을 네이티브로 지원하므로 별도 반복 로직(코루틴 등) 불필요

## 계획된 파일
`Assets/My/Scripts/Audio/SoundManager.cs` (신규)

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip nightClip;
    [SerializeField] private AudioClip morningClip;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
    }

    private void Start()
    {
        Play(nightClip);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            Play(nightClip);
        else if (keyboard.digit2Key.wasPressedThisFrame)
            Play(morningClip);
    }

    private void Play(AudioClip clip)
    {
        if (clip == null || source.clip == clip) return;
        source.clip = clip;
        source.Play();
    }
}
```

- Inspector에서 Night Clip / Morning Clip 두 필드에 클립을 넣으면, 시작 시 Night Clip부터 무한 재생, 1키/2키로 밤/낮 클립 전환 (같은 클립 재입력 시 재시작 안 함)
- `DayNightSwitcher`와 직접 연동(호출)하지 않고 동일한 키 입력을 독립적으로 감지 — 결합도 낮춤, 두 스크립트를 같은 오브젝트에 둘 필요 없음

## 스킵한 것
- 볼륨/믹서/3개 이상 클립/정지 API 등은 요청 범위 밖 → 필요해지면 추가
- `DayNightSwitcher`와의 직접 연동(이벤트/함수 호출)은 스킵, 필요해지면 키 입력 대신 이벤트로 리팩터링

## 남은 작업
승인 완료, `Assets/My/Scripts/Audio/SoundManager.cs` 생성함. 빈 GameObject에 붙이고 AudioSource(자동 생성됨) + Night/Morning Clip 필드만 채우면 됨.
