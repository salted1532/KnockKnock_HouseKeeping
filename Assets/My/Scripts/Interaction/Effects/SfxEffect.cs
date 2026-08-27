using UnityEngine;

// 상호작용 시 효과음. 토글 상호작용이면 on/off 클립을 따로 재생.
[RequireComponent(typeof(AudioSource))]
public class SfxEffect : InteractionEffect
{
    [Tooltip("비토글 상호작용용 단일 클립")]
    [SerializeField] private AudioClip clip;
    [Header("토글 상호작용용")]
    [SerializeField] private AudioClip onClip;
    [SerializeField] private AudioClip offClip;
    [Tooltip("재생 중 다시 상호작용하면 이전 소리를 끊고 새 소리로 교체 (문 스윙 도중 재토글 등)")]
    [SerializeField] private bool interrupt = true;

    private AudioSource src;

    // 에디터에서 컴포넌트 추가 시 AudioSource 를 3D/논플레이온어웨이크로 초기화
    private void Reset()
    {
        var a = GetComponent<AudioSource>();
        if (a != null) { a.playOnAwake = false; a.spatialBlend = 1f; }
    }

    private void Awake()
    {
        src = GetComponent<AudioSource>();
        if (src == null)   // RequireComponent 로 보통 있지만 방어
        {
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
        }
    }

    public override void Play(in InteractionContext ctx)
    {
        AudioClip c = ctx.Interactable.IsToggle ? (ctx.IsOn ? onClip : offClip) : clip;
        if (c == null) return;

        if (interrupt)
        {
            src.Stop();
            src.clip = c;
            src.Play();
        }
        else
        {
            src.PlayOneShot(c);
        }
    }
}
