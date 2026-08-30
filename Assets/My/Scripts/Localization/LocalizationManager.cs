using UnityEngine;

// 게임의 표시 언어. 인스펙터에서는 콤보 박스로 뜬다.
public enum Language { English, Korean }

// 씬에 빈 GameObject 하나로 배치. Awake 에서 인스펙터 선택을 정적 Current 로 확정한다.
// 게임 시작 시 언어가 고정되며 런타임 전환은 없다(요청 사양).
// 대사/라벨은 각자 표시 직전에 Korean / T(en, ko) 를 읽는다 — 이벤트·리프레시 불필요.
[DefaultExecutionOrder(-500)]
public class LocalizationManager : MonoBehaviour
{
    [Tooltip("게임 시작 시 이 언어로 모든 텍스트를 출력한다")]
    [SerializeField] private Language language = Language.English;

    public static Language Current { get; private set; } = Language.English;
    public static bool Korean => Current == Language.Korean;

    private void Awake() => Current = language;

    // 정적 UI 문자열용 인라인 페어. 문자열 수가 적어 키 테이블은 두지 않는다.
    // ko 가 비어 있으면 en 으로 폴백.
    public static string T(string en, string ko) =>
        Korean && !string.IsNullOrEmpty(ko) ? ko : en;

#if UNITY_EDITOR
    // 에디터에서 콤보 박스를 바꾸면 플레이 전에도 프리뷰가 맞도록.
    private void OnValidate()
    {
        if (!Application.isPlaying) Current = language;
    }
#endif
}
