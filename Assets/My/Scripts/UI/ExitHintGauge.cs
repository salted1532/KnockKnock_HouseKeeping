using UnityEngine;
using UnityEngine.UI;

// 화면고정(접객·새벽 대화) 나가기 홀드 게이지 — UIInteractionMode.ExitProgress(0~1) 를 Image.fillAmount 로.
// UIInteractionMode.exitHint 오브젝트(또는 그 자식)에 붙이고 Filled 타입 Image 를 연결한다. doc/0144.
public class ExitHintGauge : MonoBehaviour
{
    [SerializeField] private Image fill;

    private void Reset() => fill = GetComponent<Image>();

    private void Update()
    {
        if (fill == null) return;
        fill.fillAmount = UIInteractionMode.Instance != null ? UIInteractionMode.Instance.ExitProgress : 0f;
    }
}
