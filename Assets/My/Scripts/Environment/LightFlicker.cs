using System.Collections;
using UnityEngine;

// 고장난 형광등 느낌으로 Light 를 랜덤하게 깜박이게 한다.
// 대부분 켜진 채 미세하게 떨리다가, 가끔 빠르게 stutter 하거나 잠깐 완전히 꺼졌다 돌아온다.
// target 미할당 시 같은 오브젝트의 Light 를 쓴다. 원래 intensity 를 기준값으로 잡는다.
// flickerClip 을 넣으면 깜박이는 동안만 그 소리가 나고, 정상 점등 구간엔 일시정지(pause) 됐다가
// 다음 깜박임에서 이어서 재생된다.
public class LightFlicker : MonoBehaviour
{
    [SerializeField] private Light target;

    [Header("켜져 있는 구간")]
    [Tooltip("정상 점등이 유지되는 시간 범위(초)")]
    [SerializeField] private Vector2 onDuration = new(1.5f, 6f);
    [Tooltip("점등 중 미세 떨림 폭 (기준 intensity 대비 비율)")]
    [SerializeField, Range(0f, 0.5f)] private float buzzAmount = 0.06f;

    [Header("깜박임(방해) 구간")]
    [Tooltip("한 번의 방해가 지속되는 시간 범위(초)")]
    [SerializeField] private Vector2 glitchDuration = new(0.05f, 0.5f);
    [Tooltip("stutter 시 한 번 껌뻑이는 간격 범위(초)")]
    [SerializeField] private Vector2 stutterInterval = new(0.03f, 0.09f);
    [Tooltip("방해가 '완전 소등'일 확률 (나머지는 빠른 stutter)")]
    [SerializeField, Range(0f, 1f)] private float blackoutChance = 0.35f;
    [Tooltip("어두워질 때 남는 밝기 비율 (0 = 완전 소등)")]
    [SerializeField, Range(0f, 1f)] private float dimLevel = 0f;

    [Header("사운드 (선택)")]
    [Tooltip("깜박이는 동안만 재생. 정상 점등 구간엔 pause 됐다가 다음 깜박임에서 이어서 재생. 비우면 무음")]
    [SerializeField] private AudioClip flickerClip;
    [SerializeField, Range(0f, 1f)] private float flickerVolume = 0.6f;
    [Tooltip("비우면 이 오브젝트에 3D AudioSource 를 자동 생성")]
    [SerializeField] private AudioSource audioSource;

    private float baseIntensity;
    private bool audioStarted;

    private void Awake()
    {
        if (target == null) target = GetComponent<Light>();
        if (target == null) { Debug.LogWarning("[LightFlicker] Light 없음 — 비활성화", this); enabled = false; return; }
        baseIntensity = target.intensity;

        if (flickerClip != null)
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;   // 3D — 간판 위치에서 들리게
            }
            audioSource.clip = flickerClip;
            audioSource.loop = true;             // 깜박임 동안 끊기지 않게
            audioSource.playOnAwake = false;
            audioSource.volume = flickerVolume;
        }
    }

    private void OnEnable()
    {
        if (target != null) StartCoroutine(Run());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (target != null) target.intensity = baseIntensity;
        if (audioSource != null) audioSource.Stop();
        audioStarted = false;
    }

    private IEnumerator Run()
    {
        while (true)
        {
            // 정상 점등 + 버즈 떨림 — 소리는 멈춤
            PauseFlickerAudio();
            for (float t = Random.Range(onDuration.x, onDuration.y); t > 0f; t -= Time.deltaTime)
            {
                target.intensity = baseIntensity * (1f - Random.value * buzzAmount);
                yield return null;
            }

            // 방해: 완전 소등 or 빠른 stutter — 이 동안만 소리 재생
            ResumeFlickerAudio();
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

    private void ResumeFlickerAudio()
    {
        if (audioSource == null) return;
        if (!audioStarted) { audioSource.Play(); audioStarted = true; }
        else audioSource.UnPause();
    }

    private void PauseFlickerAudio()
    {
        if (audioSource != null && audioStarted) audioSource.Pause();
    }
}
