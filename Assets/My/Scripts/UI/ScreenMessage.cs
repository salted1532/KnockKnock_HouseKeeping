using System.Collections;
using TMPro;
using UnityEngine;

// 화면 중앙에 잠깐 뜨는 나레이션/관찰 문구 (노크 거절 "응답이 없다" 등). 싱글턴.
// 호출: ScreenMessage.Show("응답이 없다") 또는 ScreenMessage.Show(en, ko) — 언어는 LocalizationManager 가 결정.
// 씬 HUD Canvas 에 CanvasGroup + 중앙 TMP_Text 를 가진 오브젝트로 배치.
public class ScreenMessage : MonoBehaviour
{
    public static ScreenMessage Instance { get; private set; }

    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text label;
    [SerializeField] private float fadeIn = 0.3f;
    [SerializeField] private float hold = 2.5f;
    [SerializeField] private float fadeOut = 0.7f;

    private Coroutine running;

    private void Awake()
    {
        Instance = this;
        if (group == null) group = GetComponent<CanvasGroup>();
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public static void Show(string en, string ko) => Show(LocalizationManager.T(en, ko));

    public static void Show(string text)
    {
        if (Instance != null) Instance.Run(text);
        else Debug.LogWarning($"[ScreenMessage] 씬에 인스턴스 없음 — '{text}' 표시 못 함");
    }

    private void Run(string text)
    {
        if (label != null) label.text = text;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Fade());
    }

    private IEnumerator Fade()
    {
        yield return To(1f, fadeIn);
        yield return new WaitForSeconds(hold);
        yield return To(0f, fadeOut);
        running = null;
    }

    private IEnumerator To(float target, float dur)
    {
        float from = group != null ? group.alpha : 0f;
        for (float t = 0f; t < dur && dur > 0f; t += Time.deltaTime)
        {
            if (group != null) group.alpha = Mathf.Lerp(from, target, t / dur);
            yield return null;
        }
        if (group != null) group.alpha = target;
    }
}
