using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 일차 종료 연출 (doc/0145).
// 새벽에 침대 상호작용(NewsBriefingEffect) → 여는 페이드 → 암전 중 플레이어를 briefingAnchor 로 순간이동
//  (화면고정 + 상호작용 차단) → 왼쪽 대화창에 그날 뉴스 나레이션(선택지 없음) + (있으면) 오른쪽 TV 슬라이드
//  → 다 보면 닫는 페이드(암전 중 원위치 복원 + 아침 전환) → 아침으로 밝아짐.
// 뉴스 콘텐츠(문구·슬라이드)는 CampaignData.DayPlan 에서 오늘 일차로 조회.
public class NightNewsBriefing : MonoBehaviour
{
    // 브리핑 진행 중 — DayPhaseManager 디버그 키·재상호작용 게이트.
    public static bool Playing { get; private set; }

    [SerializeField] private CampaignData campaign;
    [Tooltip("플레이어를 순간이동시킬 위치/정면. 카메라가 왼쪽=대화창, 오른쪽=TV 가 되도록 배치")]
    [SerializeField] private Transform briefingAnchor;
    [Tooltip("왼쪽 중앙 스크린 대화 패널 (SpeechBubble, billboard off). 문틈 대화용 dawnPanel 과 별개")]
    [SerializeField] private SpeechBubble newsPanel;

    [Header("인게임 TV (선택)")]
    [SerializeField] private GameObject tv;      // 브리핑 동안만 켜짐. 시작 비활성 권장
    [SerializeField] private Image tvImage;      // TV 화면 — 슬라이드 스프라이트 표시

    private void Awake()
    {
        if (tv != null) tv.SetActive(false);
    }

    // 침대(NewsBriefingEffect)에서 호출. 오늘 뉴스가 없으면 false 반환 → 호출측이 바로 아침으로 전환.
    // true 면 브리핑을 시작함 — 끝나면 스스로 아침으로 전환한다.
    public bool Play()
    {
        if (Playing) return true;   // 이미 진행 중

        var lines = LinesForToday();
        if (lines == null || lines.Count == 0) return false;

        if (briefingAnchor == null)
            Debug.LogWarning("[NightNewsBriefing] briefingAnchor 미할당 — 플레이어 순간이동 없이 그 자리에서 진행", this);

        Playing = true;
        StartCoroutine(Run(lines, SlidesForToday()));
        return true;
    }

    private IEnumerator Run(List<string> lines, List<Sprite> slides)
    {
        // 1. 여는 페이드 아웃 → 암전 중 순간이동 + 화면고정 + TV 켜기
        yield return Fade(() =>
        {
            UIInteractionMode.Instance?.FreezeForOverlay(true, briefingAnchor);
            if (tv != null) tv.SetActive(true);
            if (tvImage != null && slides.Count > 0) tvImage.sprite = slides[0];
        });

        // 2. 나레이션 — 선택지 없음, 클릭/E/Space 로 한 줄씩. 줄마다 TV 슬라이드 교체.
        if (newsPanel != null)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (tvImage != null && slides.Count > 0)
                    tvImage.sprite = slides[Mathf.Min(i, slides.Count - 1)];
                yield return newsPanel.ShowLine(lines[i]);
            }
            newsPanel.Hide();
        }

        // 3. 닫는 페이드 아웃 → 암전 중 TV 끄기 + 원위치 복원(순간이동) + 아침으로 전환.
        //    아침 페이드 인은 이 Fade 가 담당 (TransitionTo 는 fade:false 로 상태만).
        yield return Fade(() =>
        {
            if (tv != null) tv.SetActive(false);
            UIInteractionMode.Instance?.FreezeForOverlay(false);            // 검은 화면 중 원위치 복원 + 조작 복구
            DayPhaseManager.Instance?.TransitionTo(DayPhase.Morning, false); // DayCount++/비주얼 스왑, 페이드 없음
        });

        Playing = false;
    }

    // 암전 → atBlack() → 유지 → 밝아짐. 페이더가 없거나 이상하면 연출 없이 atBlack 만 — 소프트락 방지.
    private IEnumerator Fade(System.Action atBlack)
    {
        var sf = ScreenFader.Instance;
        if (sf != null)
        {
            // 앞선 페이드가 끝나길 대기 (겹치면 FadeThrough 가 무시됨). 최대 2초.
            for (float t = 0f; sf.IsFading && t < 2f; t += Time.deltaTime) yield return null;

            bool done = false;
            sf.FadeThrough(atBlack, () => done = true);

            // 정상이면 ~1.1초 안에 done. 3초 넘으면 페이더 이상 → 아래에서 직접 진행.
            for (float t = 0f; !done && t < 3f; t += Time.deltaTime) yield return null;
            if (done) yield break;
        }
        atBlack();
    }

    private List<string> LinesForToday()
    {
        var plan = TodayPlan();
        if (plan == null) return null;
        bool ko = LocalizationManager.Korean && plan.newsLinesKo != null && plan.newsLinesKo.Count > 0;
        return ko ? plan.newsLinesKo : plan.newsLinesEn;
    }

    private List<Sprite> SlidesForToday()
    {
        var plan = TodayPlan();
        return plan?.newsSlides ?? new List<Sprite>();
    }

    private CampaignData.DayPlan TodayPlan()
    {
        if (campaign == null) return null;
        int day = DayPhaseManager.Instance != null ? DayPhaseManager.Instance.DayCount : 1;
        return campaign.Day(day);
    }
}
