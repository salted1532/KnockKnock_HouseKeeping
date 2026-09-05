using UnityEngine;

// 특정 시간대로 넘어가 페이드 인이 끝난 직후(OnPhaseChangeFinished) 화면 중앙에 문구 1회 (ScreenMessage).
// 예: 새벽 진입 → "손님은 다 온 것 같다." 한 오브젝트에 여러 개 붙여 시간대별로 쓸 수 있다. doc/0145.
public class PhaseMessage : MonoBehaviour
{
    [SerializeField] private DayPhase phase = DayPhase.Dawn;
    [TextArea] [SerializeField] private string messageEn = "";
    [TextArea] [SerializeField] private string messageKo = "";

    private void Start()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChangeFinished += Handle;
    }

    private void OnDestroy()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChangeFinished -= Handle;
    }

    private void Handle(DayPhase p)
    {
        if (p != phase) return;
        string en = messageEn.Length > 0 ? messageEn : messageKo;
        string ko = messageKo.Length > 0 ? messageKo : messageEn;
        if (ko.Length > 0) ScreenMessage.Show(en, ko);
    }
}
