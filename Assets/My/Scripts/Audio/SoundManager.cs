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
