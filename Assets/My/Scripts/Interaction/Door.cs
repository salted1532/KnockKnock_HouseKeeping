using System.Collections;
using UnityEngine;

// 상호작용 시 경첩 축(로컬 Y) 기준으로 회전하며 여닫는 문.
// 이 스크립트는 "경첩 피벗" 오브젝트에 붙인다. 문 메시를 그 자식으로 두면 피벗이 경첩 모서리에 온다.
// (메시 피벗이 이미 경첩에 있으면 문 오브젝트에 직접 붙여도 됨.)
public class Door : MonoBehaviour
{
    [SerializeField] private float openAngle = 90f;   // 로컬 Y 기준 열림 각도. 음수면 반대 방향으로 스윙
    [SerializeField] private float openTime = 0.6f;   // 여닫는 데 걸리는 시간(초)
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool startOpen = false;

    [Header("SFX (선택)")]
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;

    private Quaternion closedRot;
    private Quaternion openRot;
    private bool isOpen;
    private Coroutine swing;
    private AudioSource audioSource;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        closedRot = transform.localRotation;
        openRot = closedRot * Quaternion.Euler(0f, openAngle, 0f);

        isOpen = startOpen;
        transform.localRotation = isOpen ? openRot : closedRot;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (openClip != null || closeClip != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    // Interactable.Door 케이스 / UnityEvent에서 호출
    public void Toggle() => SetOpen(!isOpen);

    public void SetOpen(bool open)
    {
        if (open == isOpen)
            return;

        isOpen = open;

        // 스윙 도중 반대로 뒤집히면 이전 소리를 끊고 새 소리로 교체
        AudioClip clip = open ? openClip : closeClip;
        if (audioSource != null && clip != null)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }

        if (swing != null)
            StopCoroutine(swing);
        swing = StartCoroutine(Swing(open ? openRot : closedRot));
    }

    private IEnumerator Swing(Quaternion target)
    {
        Quaternion from = transform.localRotation;
        float t = 0f;
        while (t < openTime)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(from, target, ease.Evaluate(Mathf.Clamp01(t / openTime)));
            yield return null;
        }
        transform.localRotation = target;
        swing = null;
    }
}
