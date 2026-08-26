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
    private bool isNight = true;
    private AudioClip lastFootstepClip;

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

        if (keyboard.qKey.wasPressedThisFrame)
        {
            isNight = !isNight;
            Play(isNight ? nightClip : morningClip);
        }
    }

    private void Play(AudioClip clip)
    {
        if (clip == null || source.clip == clip) return;
        source.clip = clip;
        source.Play();
    }

    public void PlayFootstep(int groundLayer, float pitch = 1f)
    {
        if (footstepSource == null || footstepSource.isPlaying) return;

        AudioClip[] clips =
            groundLayer == woodLayer ? woodStepClips :
            groundLayer == metalLayer ? metalStepClips :
            groundLayer == grassLayer ? grassStepClips : concreteStepClips;

        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clips.Length > 1 && clip == lastFootstepClip)
            clip = clips[(System.Array.IndexOf(clips, clip) + 1) % clips.Length];
        lastFootstepClip = clip;

        footstepSource.pitch = pitch;
        footstepSource.PlayOneShot(clip);
    }
}
