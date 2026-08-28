using System;
using System.Collections;
using UnityEngine;

// 전체 화면 검정 페이드. 시간대 전환 등에서 "암전 → 콜백 → 밝아짐" 연출.
// Overlay 캔버스에 풀스크린 검정 Image + CanvasGroup 을 두고 이 컴포넌트를 붙인다.
// 씬에 없으면 DayPhaseManager 등은 콜백을 즉시 실행한다 (null-safe).
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private float outDuration = 0.4f;   // 밝음 → 암전
    [SerializeField] private float holdDuration = 0.1f;   // 암전 유지 (이 동안 atBlack 처리)
    [SerializeField] private float inDuration = 0.6f;     // 암전 → 밝음

    private CanvasGroup group;
    private Coroutine running;

    public bool IsFading => running != null;

    private void Awake()
    {
        Instance = this;
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 암전 → atBlack() → 유지 → 밝아짐 → done(). 이미 진행 중이면 무시.
    public void FadeThrough(Action atBlack, Action done = null)
    {
        if (running != null) return;
        running = StartCoroutine(Run(atBlack, done));
    }

    private IEnumerator Run(Action atBlack, Action done)
    {
        group.blocksRaycasts = true;
        yield return Ramp(0f, 1f, outDuration);

        atBlack?.Invoke();
        if (holdDuration > 0f) yield return new WaitForSeconds(holdDuration);

        yield return Ramp(1f, 0f, inDuration);
        group.blocksRaycasts = false;

        running = null;
        done?.Invoke();
    }

    private IEnumerator Ramp(float from, float to, float dur)
    {
        if (dur <= 0f) { group.alpha = to; yield break; }
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            yield return null;
        }
        group.alpha = to;
    }
}
