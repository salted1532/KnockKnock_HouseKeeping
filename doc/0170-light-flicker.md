# 0170 – 고장난 형광등 깜박임 LightFlicker

## 요청
연출용 Point Light 를 고장난 형광등처럼 랜덤하게 깜박이게 하는 스크립트.

## 현황 (조사)
- 조명 깜박임 스크립트 없음 (`ActiveInPhases`=시간대 토글, `PhaseVisuals`=시간대 라이트 스왑, `Flashlight`=손전등)
- 대상 후보: `MainScene` `MoonlightMotel/Point Light` (Point, intensity 1, range 1, 붉은빛 #FF6969 — 네온 간판 조명)
- DOTween 있으나 불필요 (코루틴으로 충분)

## 작업

### 신규 `Assets/My/Scripts/Environment/LightFlicker.cs` (~70줄)
상태머신 코루틴:
1. **정상 점등** — `onDuration`(1.5~6초) 동안 기준 intensity 유지 + 매 프레임 미세 버즈 떨림(`buzzAmount`)
2. **방해** — `glitchDuration`(0.05~0.5초):
   - `blackoutChance`(35%) 확률로 **완전 소등**(`dimLevel`, 기본 0)
   - 아니면 **빠른 stutter** — `stutterInterval`(0.03~0.09초)마다 어둡게/밝게 랜덤 껌뻑
3. 반복

- 기준 intensity = `Awake` 에서 Light 의 현재 intensity 캡처(인스펙터 authored 값 존중)
- `OnDisable` 에서 원래 intensity 복구 + 코루틴 정지
- `target` 미할당 시 같은 오브젝트의 `Light` 자동 사용, 없으면 경고

```csharp
using System.Collections;
using UnityEngine;

// 고장난 형광등 느낌으로 Light 를 랜덤하게 깜박이게 한다.
// 대부분 켜진 채 미세하게 떨리다가, 가끔 빠르게 stutter 하거나 잠깐 완전히 꺼졌다 돌아온다.
public class LightFlicker : MonoBehaviour
{
    [SerializeField] private Light target;

    [Header("켜져 있는 구간")]
    [SerializeField] private Vector2 onDuration = new(1.5f, 6f);
    [SerializeField, Range(0f, 0.5f)] private float buzzAmount = 0.06f;

    [Header("깜박임(방해) 구간")]
    [SerializeField] private Vector2 glitchDuration = new(0.05f, 0.5f);
    [SerializeField] private Vector2 stutterInterval = new(0.03f, 0.09f);
    [SerializeField, Range(0f, 1f)] private float blackoutChance = 0.35f;
    [Tooltip("어두워질 때 남는 밝기 비율 (0 = 완전 소등)")]
    [SerializeField, Range(0f, 1f)] private float dimLevel = 0f;

    private float baseIntensity;

    private void Awake()
    {
        if (target == null) target = GetComponent<Light>();
        if (target == null) { Debug.LogWarning("[LightFlicker] Light 없음", this); enabled = false; return; }
        baseIntensity = target.intensity;
    }

    private void OnEnable() { if (target != null) StartCoroutine(Run()); }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (target != null) target.intensity = baseIntensity;
    }

    private IEnumerator Run()
    {
        while (true)
        {
            for (float t = Random.Range(onDuration.x, onDuration.y); t > 0f; t -= Time.deltaTime)
            {
                target.intensity = baseIntensity * (1f - Random.value * buzzAmount);
                yield return null;
            }

            float g = Random.Range(glitchDuration.x, glitchDuration.y);
            if (Random.value < blackoutChance)
            {
                target.intensity = baseIntensity * dimLevel;
                yield return new WaitForSeconds(g);
            }
            else
            {
                while (g > 0f)
                {
                    float step = Random.Range(stutterInterval.x, stutterInterval.y);
                    g -= step;
                    target.intensity = baseIntensity *
                        (Random.value < 0.5f ? dimLevel : Random.Range(0.5f, 1f));
                    yield return new WaitForSeconds(step);
                }
            }
        }
    }
}
```

### 배선
- `MainScene` `MoonlightMotel/Point Light` 에 `LightFlicker` 부착 (기본값 그대로)
- `uloop compile` + 플레이모드 시각 확인

## 사운드 (후속 요청)
`LightFlicker` 인스펙터에 **사운드(선택)** 섹션 추가:
- `flickerClip` (AudioClip) — **깜박이는 동안에만** 재생. 정상 점등 구간엔 `Pause()`, 다음 방해에서 `UnPause()` 로 이어서 재생 (첫 재생만 `Play()`).
- `flickerVolume` (0~1, 기본 0.6)
- `audioSource` — 비우면 Awake 에서 이 오브젝트에 3D AudioSource 자동 생성. Awake 에서 `clip=flickerClip`, `loop=true`(방해 중 끊김 방지), `playOnAwake=false`, `volume` 세팅.
- `OnDisable` 에서 `Stop()` + 상태 리셋.
- 방해 구간이 0.05~0.5초라 소리도 그만큼 짧게 "잠깐 났다 멈췄다" 반복됨.

## 안 하는 것
- 이미시브 머티리얼 연동, 상시 버즈 루프음, 시간대 게이트 — 요청 시 추가
- 자동화 테스트: 랜덤·시각 연출이라 플레이모드 눈 확인으로 대체

## 결과 (구현·플레이모드 검증 완료)
- `LightFlicker.cs` 신규, `uloop compile` 에러 0
- `MainScene` `MoonlightMotel/Point Light` 에 부착, 씬 저장 (`target` 자동, 기본값)
- 플레이모드 관찰(파라미터 일시 가속): intensity 가 ~0.97 버즈 → 0.00 완전 소등 → 0.59~0.71 stutter 로 랜덤하게 변동 확인. 소등/stutter/버즈 3상태 모두 동작.

## 수정 파일
- `Assets/My/Scripts/Environment/LightFlicker.cs` (신규)
- `Assets/Scenes/MainScene.unity` (Point Light 에 LightFlicker)
