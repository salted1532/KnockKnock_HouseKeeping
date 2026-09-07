using UnityEngine;
using UnityEngine.UI;

// 손님 초상화(풀바디 스프라이트)에서 얼굴 영역만 잘라 Image 에 넣는다. 신분증 profile_Image 용.
// faceRect01(NpcData) 이 지정돼 있으면 그걸로, 없으면 스프라이트 상단을 정사각형으로 자동 크롭.
// Sprite.Create 는 non-readable 텍스처도 렌더용으로는 동작한다(GetPixels 만 불가).
[RequireComponent(typeof(Image))]
public class PortraitFaceCrop : MonoBehaviour
{
    [SerializeField] private Image image;

    [Header("자동 크롭 (faceRect01 미지정 시)")]
    [Tooltip("스프라이트 실제 픽셀 높이 대비 얼굴 사각형 높이 비율")]
    [SerializeField, Range(0.1f, 1f)] private float faceHeightFrac = 0.34f;
    [Tooltip("정수리 위 여백. 양수 = 머리 위 공간 더, 음수 = 정수리부터 자름 (높이 대비 비율)")]
    [SerializeField, Range(-0.1f, 0.3f)] private float headroom = 0.04f;

    private Sprite generated;

    private void Awake()
    {
        if (image == null) image = GetComponent<Image>();
    }

    private void OnDestroy() => Discard();

    public void Clear()
    {
        if (image != null) image.sprite = null;
        Discard();
    }

    // portrait 에서 얼굴을 잘라 표시. faceRect01 (textureRect 기준 정규화, y=0 이 아래) 이
    // w·h 둘 다 > 0 이면 그 영역을, 아니면 자동 휴리스틱을 쓴다.
    public void Show(Sprite portrait, Rect faceRect01)
    {
        if (image == null) return;
        if (portrait == null || portrait.texture == null) { Clear(); return; }

        Rect b = portrait.textureRect;
        if (b.width < 1f || b.height < 1f) b = portrait.rect;

        Rect src;
        if (faceRect01.width > 0f && faceRect01.height > 0f)
        {
            src = new Rect(
                b.x + faceRect01.x * b.width,
                b.y + faceRect01.y * b.height,
                faceRect01.width * b.width,
                faceRect01.height * b.height);
        }
        else
        {
            float h = faceHeightFrac * b.height;
            float w = h;
            float top = b.yMax + headroom * b.height;   // 정수리 위 여백
            src = new Rect(b.center.x - w * 0.5f, top - h, w, h);
        }

        // 텍스처 경계 안으로 클램프
        float tw = portrait.texture.width, th = portrait.texture.height;
        src.x = Mathf.Clamp(src.x, 0f, tw - 1f);
        src.y = Mathf.Clamp(src.y, 0f, th - 1f);
        src.width = Mathf.Clamp(src.width, 1f, tw - src.x);
        src.height = Mathf.Clamp(src.height, 1f, th - src.y);

        var s = Sprite.Create(portrait.texture, src, new Vector2(0.5f, 0.5f), portrait.pixelsPerUnit);
        s.name = portrait.name + "_face";

        Discard();
        generated = s;
        image.sprite = s;
        image.preserveAspect = true;
    }

    private void Discard()
    {
        if (generated != null) Destroy(generated);
        generated = null;
    }
}
