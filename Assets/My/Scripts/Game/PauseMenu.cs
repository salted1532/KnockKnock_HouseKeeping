using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// InGame 씬 옵션(일시정지) 창. Esc 로 연다 — 단, 노트/모니터/새벽 노크처럼 Esc 로 먼저
// 빠져나갈 뷰가 있으면 그게 우선이고 이 창은 안 뜬다. 접객 모드·자유 이동에선 바로 뜬다.
// 열려 있을 때 Esc = 닫기(재개). 패널엔 "메인화면으로 나가기" 버튼만.
public class PauseMenu : MonoBehaviour
{
    [Tooltip("켜고 끌 옵션 패널 (반투명 배경 + 버튼). 시작 시 자동으로 꺼짐. 필수")]
    [SerializeField] private GameObject panel;
    [Tooltip("'메인화면으로 나가기' 가 여는 씬")]
    [SerializeField] private string mainMenuScene = "MainScene";

    private bool open;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        else Debug.LogWarning("[PauseMenu] panel 미할당", this);
    }

    private void OnDisable()
    {
        if (open) SetOpen(false);   // 씬 언로드 등 — timeScale 복구
    }

    private void Update()
    {
        var k = Keyboard.current;
        if (k == null || !k.escapeKey.wasPressedThisFrame) return;

        if (open) { SetOpen(false); return; }   // 열려 있으면 Esc = 닫기(재개)
        if (EscConsumedElsewhere()) return;     // 노트/모니터/새벽 노크가 먼저 처리
        SetOpen(true);
    }

    // 이번 Esc 를 다른 뷰가 소비하는가 (노트 닫기 / 모니터·새벽 노크 빠져나오기).
    private bool EscConsumedElsewhere()
    {
        if (ShowPanelEffect.ConsumesEsc) return true;
        var uim = UIInteractionMode.Instance;
        return uim != null && uim.Active && uim.TopEscExits;   // 모니터/새벽 노크 = escExits:true
    }

    private void SetOpen(bool value)
    {
        open = value;
        if (panel != null) panel.SetActive(value);

        Time.timeScale = value ? 0f : 1f;
        if (DialogueRunner.Instance != null) DialogueRunner.Instance.Paused = value;
        // 자유 이동: 커서 표시 + FPS 컨트롤·시선 상호작용 정지. 접객/모니터 중이면 no-op(이미 커서 있음).
        if (UIInteractionMode.Instance != null)
            UIInteractionMode.Instance.FreezeForOverlay(value, null, true);
    }

    // 패널의 "메인화면으로 나가기" 버튼 onClick 에 연결.
    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}
