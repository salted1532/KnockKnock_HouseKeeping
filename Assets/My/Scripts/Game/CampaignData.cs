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
        [TextArea(2, 6)] public string nightNews;   // (구) SYS-11 취침 뉴스 — 디자이너 메모용

        [Header("일차 종료 TV 뉴스 브리핑 (doc/0145)")]
        [Tooltip("대화창에 뜨는 뉴스 나레이션. 한 항목 = 한 줄(클릭/E/Space 로 넘김)")]
        [TextArea(1, 3)] public List<string> newsLinesEn = new();
        [Tooltip("비면 영어(newsLinesEn) 사용")]
        [TextArea(1, 3)] public List<string> newsLinesKo = new();
        [Tooltip("오른쪽 인게임 TV 슬라이드. 나레이션 줄 i → 슬라이드 i (모자라면 마지막 유지). 비우면 TV 이미지 그대로")]
        public List<Sprite> newsSlides = new();
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
