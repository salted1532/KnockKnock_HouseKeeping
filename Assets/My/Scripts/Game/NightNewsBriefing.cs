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

    [Header("UI 정리")]
    [Tooltip("뉴스 대화 패널(News_Panel) 루트. 브리핑 동안 이것과 같은 HUD Canvas 아래의 " +
             "다른 UI(돈/할일/조준점 등)는 전부 숨김. ScreenFader·RawImage(게임 화면)는 유지")]
    [SerializeField] private GameObject newsPanelRoot;

    private readonly List<GameObject> hudHidden = new List<GameObject>();

    private void Awake()
    {
        if (tv != null) tv.SetActive(false);
    }

    // 침대(NewsBriefingEffect)에서 호출. 매 일차 새벽마다 브리핑을 시작한다 (대사집 없는 날은 대체 문구).
    // 끝나면 스스로 아침으로 전환. false 는 이론상 안 나오지만 방어적으로 유지.
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
        // 1. 여는 페이드 아웃 → 암전 중 순간이동 + 화면고정 + TV 켜기 + 나머지 HUD 숨김
        yield return Fade(() =>
        {
            UIInteractionMode.Instance?.FreezeForOverlay(true, briefingAnchor);
            if (tv != null) tv.SetActive(true);
            if (tvImage != null && slides.Count > 0) tvImage.sprite = slides[0];
            HideHud(true);
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
            HideHud(false);                                                 // 숨겼던 HUD 복원 (TransitionTo 전에 — DayEndTitle 등이 다시 켜져야 함)
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

    // 뉴스 패널과 같은 HUD Canvas 아래의 다른 UI 를 전부 껐다(hide=true) / 다시 켠다(hide=false).
    // 게임 화면(RawImage) 과 페이드 오버레이(ScreenFader) 는 유지. 새 HUD 위젯이 생겨도 자동 포함.
    private void HideHud(bool hide)
    {
        if (!hide)
        {
            foreach (var go in hudHidden) if (go != null) go.SetActive(true);
            hudHidden.Clear();
            return;
        }

        if (newsPanelRoot == null) { Debug.LogWarning("[NightNewsBriefing] newsPanelRoot 미할당 — HUD 정리 생략", this); return; }
        Transform hud = newsPanelRoot.transform.parent;
        if (hud == null) return;

        hudHidden.Clear();
        foreach (Transform child in hud)
        {
            if (child.gameObject == newsPanelRoot || !child.gameObject.activeSelf) continue;
            if (child.GetComponent<ScreenFader>() != null || child.GetComponent<RawImage>() != null) continue;
            child.gameObject.SetActive(false);
            hudHidden.Add(child.gameObject);
        }
    }

    private List<string> LinesForToday()
    {
        var plan = TodayPlan();
        if (plan != null)
        {
            bool ko = LocalizationManager.Korean && plan.newsLinesKo != null && plan.newsLinesKo.Count > 0;
            var lines = ko ? plan.newsLinesKo : plan.newsLinesEn;
            if (lines != null && lines.Count > 0) return lines;
        }
        return FallbackLines();   // 편성/대사집이 없는 날도 브리핑은 매일 작동
    }

    private List<Sprite> SlidesForToday()
    {
        var plan = TodayPlan();
        return plan?.newsSlides ?? new List<Sprite>();
    }

    // 오늘 편성이 없거나 뉴스 문구가 비었으면 1일차 편성으로 대체 (아직 대사집을 안 넣은 날).
    private CampaignData.DayPlan TodayPlan()
    {
        if (campaign == null) return null;
        int day = DayPhaseManager.Instance != null ? DayPhaseManager.Instance.DayCount : 1;
        var plan = campaign.Day(day);
        if (plan == null || !HasLines(plan)) plan = campaign.Day(1);
        return plan;
    }

    private static bool HasLines(CampaignData.DayPlan p) =>
        (p.newsLinesEn != null && p.newsLinesEn.Count > 0) || (p.newsLinesKo != null && p.newsLinesKo.Count > 0);

    // 캠페인이 아예 없거나 1일차 대사집도 비었을 때의 최후 문구 — 브리핑이 조용히 스킵되지 않게.
    private static List<string> FallbackLines() => new List<string>
    {
        LocalizationManager.T(
            "This is your midnight update. Nothing to report tonight. Good night.",
            "자정 뉴스입니다. 오늘 밤은 전할 소식이 없습니다. 편안한 밤 되십시오."),
    };
}
