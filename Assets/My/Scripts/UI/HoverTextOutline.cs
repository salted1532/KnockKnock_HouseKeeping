using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// UI 오브젝트(버튼 등) 위에 마우스를 올리면 자식 TMP 텍스트의 글자색 + 외곽선색을 hoverColor 로 바꾼다.
// 버튼 이미지를 끄고 글자만 보이는 메뉴에서 호버 피드백용. label 비우면 자식에서 자동 검색.
// 외곽선은 fontMaterial 게터로 만든 텍스트 전용 인스턴스 머티리얼에만 적용 → 공유 머티리얼 무변경.
public class HoverTextOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color hoverColor = Color.yellow;

    private Color normalFace = Color.white;
    private Color normalOutline = Color.black;

    private void Awake()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            normalFace = label.color;
            normalOutline = label.fontSharedMaterial.GetColor(ShaderUtilities.ID_OutlineColor);
        }
    }

    private void OnDisable() => Apply(normalFace, normalOutline);   // 씬 전환/비활성 시 원복

    public void OnPointerEnter(PointerEventData _) => Apply(hoverColor, hoverColor);
    public void OnPointerExit(PointerEventData _) => Apply(normalFace, normalOutline);

    private void Apply(Color face, Color outline)
    {
        if (label == null) return;
        label.color = face;
        label.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outline);
    }
}
