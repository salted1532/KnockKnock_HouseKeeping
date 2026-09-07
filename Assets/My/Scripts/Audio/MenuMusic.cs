using UnityEngine;

// 메인화면(MainScene) 배경음악 재생기. 씬에 오브젝트 하나 두고 bgm 클립을 연결하면 루프 재생한다.
// 씬 전환(Play → InGame) 시 함께 파괴되어 자동으로 멈춘다.
[RequireComponent(typeof(AudioSource))]
public class MenuMusic : MonoBehaviour
{
    [Tooltip("루프 재생할 배경음악 클립")]
    [SerializeField] private AudioClip bgm;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.5f;

    // 컴포넌트 추가 시 AudioSource 를 2D·논플레이온어웨이크·루프로 초기화
    private void Reset()
    {
        var a = GetComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = true;
        a.spatialBlend = 0f;
    }

    private void Awake()
    {
        var src = GetComponent<AudioSource>();
        src.loop = true;
        src.spatialBlend = 0f;
        src.playOnAwake = false;
        src.volume = volume;

        if (bgm == null) { Debug.LogWarning("[MenuMusic] bgm 클립 미할당", this); return; }
        src.clip = bgm;
        src.Play();
    }
}
