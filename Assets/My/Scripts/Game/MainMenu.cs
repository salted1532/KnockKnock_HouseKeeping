using UnityEngine;
using UnityEngine.SceneManagement;

// 메인 메뉴 씬(MainScene) 버튼 핸들러. Canvas 에 붙이고 Play/Exit 버튼 onClick 에 연결한다.
// playScene 은 Build Settings 에 포함돼 있어야 로드된다.
public class MainMenu : MonoBehaviour
{
    [Tooltip("Play 버튼이 여는 씬 이름")]
    [SerializeField] private string playScene = "InGame";

    // InGame 게임플레이는 커서를 잠그고 숨긴다 — 메인화면으로 나오면 다시 풀어 보이게.
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Play() => SceneManager.LoadScene(playScene);

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
