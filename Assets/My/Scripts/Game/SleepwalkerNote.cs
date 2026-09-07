using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// 숙소 노트(note_image) 내용 = 그날의 몽유병 환자 구별법.
// ShowPanelEffect 가 노트를 켤 때(OnEnable) CampaignData 에서 오늘 일차의 힌트를 읽어
// "1. ...  2. ..." 로 채운다. note_image 에 부착.
public class SleepwalkerNote : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private CampaignData campaign;

    [Header("맨 위 제목 줄 (비우면 제목 없음)")]
    [SerializeField] private string titleEn = "How to spot the sleepwalker";
    [SerializeField] private string titleKo = "몽유병 환자 구별법";

    private void Awake()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
    }

    private void OnEnable()
    {
        if (label != null) label.text = Build();
    }

    private string Build()
    {
        var hints = HintsForToday();
        var sb = new StringBuilder();

        string title = LocalizationManager.T(titleEn, titleKo);
        if (!string.IsNullOrEmpty(title)) sb.Append(title).Append("\n\n");

        if (hints == null || hints.Count == 0)
            sb.Append(LocalizationManager.T("(No notes for today.)", "(오늘 자 기록 없음.)"));
        else
            for (int i = 0; i < hints.Count; i++)
                sb.Append(i + 1).Append(". ").Append(hints[i]).Append('\n');

        return sb.ToString().TrimEnd();
    }

    private List<string> HintsForToday()
    {
        if (campaign == null) return null;
        int day = DayPhaseManager.Instance != null ? DayPhaseManager.Instance.DayCount : 1;
        var plan = campaign.Day(day) ?? campaign.Day(1);
        if (plan == null) return null;

        bool ko = LocalizationManager.Korean
                  && plan.sleepwalkerHintsKo != null && plan.sleepwalkerHintsKo.Count > 0;
        return ko ? plan.sleepwalkerHintsKo : plan.sleepwalkerHintsEn;
    }
}
