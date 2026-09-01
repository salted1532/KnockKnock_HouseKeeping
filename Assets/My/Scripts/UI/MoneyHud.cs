using TMPro;
using UnityEngine;

// 소지금 HUD. Wallet.OnChanged 구독 → 텍스트 갱신 + 입금(delta>0) 시 효과음.
// Canvas 의 "Money" 텍스트에 붙이면 label·AudioSource 는 자동으로 잡힌다. cashClip 만 연결하면 됨.
[RequireComponent(typeof(AudioSource))]
public class MoneyHud : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [Tooltip("돈이 들어올 때(delta>0) 재생하는 현금 효과음")]
    [SerializeField] private AudioClip cashClip;

    private AudioSource sfx;
    private bool subscribed;

    // 컴포넌트 추가 시: label 자동 획득 + AudioSource 를 2D·논플레이온어웨이크로 초기화
    private void Reset()
    {
        label = GetComponent<TMP_Text>();
        var a = GetComponent<AudioSource>();
        if (a != null) { a.playOnAwake = false; a.spatialBlend = 0f; }
    }

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        sfx = GetComponent<AudioSource>();
    }

    // OnEnable 은 Wallet.Awake 보다 먼저 돌 수 있어 실패할 수 있다 → Start 에서 재시도.
    private void OnEnable() => TrySubscribe();
    private void Start() => TrySubscribe();

    private void OnDisable()
    {
        if (subscribed && Wallet.Instance != null) Wallet.Instance.OnChanged -= Refresh;
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed || Wallet.Instance == null) return;
        Wallet.Instance.OnChanged += Refresh;
        subscribed = true;
        Show(Wallet.Instance.Balance);
    }

    private void Refresh(int delta, int balance)
    {
        Show(balance);
        if (delta > 0 && sfx != null && cashClip != null) sfx.PlayOneShot(cashClip);
    }

    private void Show(int balance)
    {
        if (label != null) label.text = $"${balance:N0}";
    }
}
