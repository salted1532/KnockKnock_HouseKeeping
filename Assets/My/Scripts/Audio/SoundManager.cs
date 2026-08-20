using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip nightClip;
    [SerializeField] private AudioClip morningClip;

    [Header("Footstep")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioClip[] woodStepClips;
    [SerializeField] private AudioClip[] concreteStepClips;
    [SerializeField] private AudioClip[] metalStepClips;
    [SerializeField] private AudioClip[] grassStepClips;

    public static SoundManager Instance { get; private set; }

    private AudioSource source;

    private int woodLayer, concreteLayer, metalLayer, grassLayer;

    private void Awake()
    {
        Instance = this;

        source = GetComponent<AudioSource>();
        source.loop = true;

        woodLayer = LayerMask.NameToLayer("Wood");
        concreteLayer = LayerMask.NameToLayer("Concrete");
        metalLayer = LayerMask.NameToLayer("Metal");
        grassLayer = LayerMask.NameToLayer("Grass");
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

    public void PlayFootstep(int groundLayer)
    {
        if (footstepSource == null) return;

        AudioClip[] clips =
            groundLayer == woodLayer ? woodStepClips :
            groundLayer == metalLayer ? metalStepClips :
            groundLayer == grassLayer ? grassStepClips : concreteStepClips;

        if (clips == null || clips.Length == 0) return;
        footstepSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }
}
