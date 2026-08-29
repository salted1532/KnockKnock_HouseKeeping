using System;
using System.Collections.Generic;
using UnityEngine;

// 캠페인 전체 편성. 일차를 리스트로 추가하고, 각 일차의 저녁 접객 손님을 번호로 관리.
// 구 DayData(일차당 에셋 1개) 대체 — 에셋 1개.
[CreateAssetMenu(menuName = "KnockKnock/Campaign Data", fileName = "Campaign")]
public class CampaignData : ScriptableObject
{
    public List<DayPlan> days = new();

    public DayPlan Day(int day) => days.Find(d => d != null && d.day == day);

    [Serializable]
    public class DayPlan
    {
        public int day = 1;
        [Tooltip("이 날 저녁 접객에 오는 손님 번호(NpcData.id), 순서대로")]
        public List<int> eveningGuestIds = new();
        [TextArea(2, 6)] public string nightNews;   // SYS-11 취침 뉴스
    }

    private void OnValidate()
    {
        var seen = new HashSet<int>();
        foreach (var d in days)
        {
            if (d == null) continue;
            if (!seen.Add(d.day))
                Debug.LogWarning($"[CampaignData] '{name}' day {d.day} 중복", this);
        }
    }
}
