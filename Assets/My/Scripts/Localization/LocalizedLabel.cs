using TMPro;
using UnityEngine;

// 정적 TMP 라벨을 언어에 맞춰 채운다. 메뉴/HUD 등 코드가 값을 안 넣는 텍스트에 붙인다.
// 게임 시작 시 한 번(그리고 활성화될 때마다) 적용 — LocalizationManager 와 동일하게 런타임 전환은 없다.
[RequireComponent(typeof(TMP_Text))]
public class LocalizedLabel : MonoBehaviour
{
    [SerializeField, TextArea] private string english;
    [SerializeField, TextArea] private string korean;

    private void OnEnable()
    {
        var t = GetComponent<TMP_Text>();
        if (t != null) t.text = LocalizationManager.T(english, korean);
    }
}
