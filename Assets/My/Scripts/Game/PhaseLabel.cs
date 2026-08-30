using TMPro;
using UnityEngine;

// 현재 시간대(선택적으로 일차)를 TMP 텍스트에 표시. DayPhaseManager.OnPhaseChanged 구독.
// 표시할 TMP_Text 와 같은 오브젝트에 붙이거나, label 필드에 직접 연결.
public class PhaseLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [Tooltip("켜면 'Day N · Morning' 형식, 끄면 'Morning'")]
    [SerializeField] private bool showDayCount = true;

    private void Reset() => label = GetComponent<TMP_Text>();

    private void Start()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        if (label == null) { Debug.LogWarning("[PhaseLabel] TMP_Text 미할당", this); return; }

        if (DayPhaseManager.Instance != null)
        {
            DayPhaseManager.Instance.OnPhaseChanged += Refresh;
            Refresh(DayPhaseManager.Instance.Current);
        }
        else label.text = "-";
    }

    private void OnDestroy()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChanged -= Refresh;
    }

    private void Refresh(DayPhase phase)
    {
        if (label == null) return;

        string name = LocalizationManager.Korean
            ? phase switch
            {
                DayPhase.Morning => "아침",
                DayPhase.Noon => "점심",
                DayPhase.Evening => "저녁",
                DayPhase.Dawn => "새벽",
                _ => phase.ToString(),
            }
            : phase.ToString();   // Morning / Noon / Evening / Dawn
        int day = DayPhaseManager.Instance != null ? DayPhaseManager.Instance.DayCount : 1;
        if (!showDayCount) { label.text = name; return; }
        label.text = LocalizationManager.Korean ? $"{day}일차 · {name}" : $"Day {day} · {name}";
    }
}
