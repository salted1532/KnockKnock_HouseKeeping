using UnityEngine;

// 스프라이트 손님용 hover 하이라이트. QuickOutline(Outline)은 3D 메쉬 실루엣용이라
// SpriteRenderer 에 붙으면 fill 머티리얼이 스프라이트 쿼드를 덧그려 깨진다.
// 대신 같은 스프라이트를 조금 키워 단색으로 원본 뒤에 깔고, Interactor 가 hover 시 켠다.
// (중심 기준 균일 확대라 알파 모양 외곽선은 아님 — 실루엣이 살짝 두꺼워지는 방식)
[DisallowMultipleComponent]
public class SpriteOutline : MonoBehaviour
{
    [Tooltip("따라갈 원본 스프라이트. 비우면 자식에서 탐색 (= GuestView.body)")]
    [SerializeField] private SpriteRenderer source;
    [Tooltip("외곽선 렌더러 머티리얼. 비우면 원본 머티리얼 복사(단색 안 됨). My/SpriteSilhouette 권장")]
    [SerializeField] private Material material;
    [SerializeField] private Color color = new(1f, 0.85f, 0.2f, 1f);
    [Tooltip("원본 대비 확대 배율 (1.06 = 6% 큼)")]
    [SerializeField] private float scale = 1.06f;
    [Tooltip("원본보다 이만큼 뒤 sortingOrder")]
    [SerializeField] private int sortingOffset = -1;

    private SpriteRenderer outline;

    private void Awake()
    {
        if (source == null) source = GetComponentInChildren<SpriteRenderer>();
        if (source == null) { enabled = false; return; }

        var go = new GameObject("SpriteOutline");
        go.layer = source.gameObject.layer;
        go.transform.SetParent(source.transform, false);   // 원본 스케일(초상화별 리스케일) 자동 상속
        go.transform.localScale = Vector3.one * scale;

        outline = go.AddComponent<SpriteRenderer>();
        outline.sharedMaterial = material != null ? material : source.sharedMaterial;
        outline.color = color;
        outline.enabled = false;
    }

    private void LateUpdate()
    {
        if (outline != null && outline.enabled) Mirror();
    }

    private void Mirror()
    {
        outline.sprite = source.sprite;
        outline.flipX = source.flipX;
        outline.flipY = source.flipY;
        outline.sortingLayerID = source.sortingLayerID;
        outline.sortingOrder = source.sortingOrder + sortingOffset;
    }

    // Interactor 가 hover 진입/이탈 시 호출.
    public void SetHighlighted(bool on)
    {
        if (outline == null) return;
        if (on) Mirror();   // LateUpdate 이 이번 프레임 이미 지났을 수 있어 즉시 1회
        outline.enabled = on;
    }
}
