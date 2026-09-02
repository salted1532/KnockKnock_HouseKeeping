using UnityEngine;
using UnityEngine.UI;

// 상호작용이 가능해지면(Interactable.CanInteract) 이 오브젝트 위에 HUD 마커를 띄운다 — "여기로 가라" 유도.
// 3D 외곽선 대신 HUD 캔버스(ScreenSpaceOverlay)에 그리므로 PxlCrush(RawImage) 크러시를 안 탄다 → 색이 그대로.
// 마커 색은 오브젝트마다 지정. 화면 밖이면 가장자리로 클램프하고 다이아몬드 꼭짓점이 방향을 가리킨다.
// 게시판(아침 할일 완료)·접객 테이블(점심)·주인방 침대(새벽) 등에 붙인다. doc/0144.
[RequireComponent(typeof(Interactable))]
public class ObjectiveMarker : MonoBehaviour
{
    [SerializeField] private Color color = Color.yellow;
    [Tooltip("마커가 붙을 HUD 컨테이너 (ScreenSpaceOverlay Canvas). 비우면 자동 탐색")]
    [SerializeField] private RectTransform hudParent;
    [Tooltip("마커가 가리킬 월드 지점 (비우면 오브젝트 원점 + worldYOffset)")]
    [SerializeField] private Transform worldAnchor;
    [SerializeField] private float worldYOffset = 0.5f;
    [SerializeField] private float size = 22f;
    [SerializeField] private float edgePadding = 40f;

    [Header("상호작용 가능해질 때 화면 중앙 안내 (1회, ScreenMessage). 비우면 안 띄움")]
    [SerializeField] private string messageEn = "";
    [SerializeField] private string messageKo = "";

    private Interactable interactable;
    private RectTransform marker;
    private Image markerImage;
    private Camera cam;
    private bool wasInteractable;

    private void Awake() => interactable = GetComponent<Interactable>();

    private void Start()
    {
        if (hudParent == null)
        {
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
                if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.transform.parent == null)
                { hudParent = (RectTransform)c.transform; break; }
        }
        if (hudParent == null) { enabled = false; Debug.LogWarning("[ObjectiveMarker] HUD Canvas 못 찾음", this); return; }

        var go = new GameObject($"Marker_{name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        marker = (RectTransform)go.transform;
        marker.SetParent(hudParent, false);
        marker.anchorMin = marker.anchorMax = marker.pivot = new Vector2(0.5f, 0.5f);
        marker.sizeDelta = new Vector2(size, size);
        markerImage = go.GetComponent<Image>();
        markerImage.color = color;
        markerImage.raycastTarget = false;
        go.SetActive(false);
    }

    private void LateUpdate()
    {
        if (marker == null) return;
        if (cam == null) cam = Camera.main;

        bool canInteract = interactable.CanInteract;

        // 상호작용 가능으로 막 바뀐 순간 → 안내 문구 1회 ("게시판으로 가자" 등)
        if (canInteract && !wasInteractable)
        {
            string ko = messageKo.Length > 0 ? messageKo : messageEn;
            string en = messageEn.Length > 0 ? messageEn : messageKo;
            if (ko.Length > 0) ScreenMessage.Show(en, ko);
        }
        wasInteractable = canInteract;

        bool show = cam != null && canInteract;
        if (!show)
        {
            if (marker.gameObject.activeSelf) marker.gameObject.SetActive(false);
            return;
        }

        Vector3 wp = worldAnchor != null ? worldAnchor.position : transform.position + Vector3.up * worldYOffset;
        Vector3 vp = cam.WorldToViewportPoint(wp);

        bool behind = vp.z < 0f;
        Vector2 v = new Vector2(vp.x, vp.y);
        if (behind) v = new Vector2(0.5f, 0.5f) - (v - new Vector2(0.5f, 0.5f));   // 뒤쪽이면 반대편으로

        bool onScreen = !behind && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f;
        Vector2 canvasSize = hudParent.rect.size;

        Vector2 pos;
        float rot;
        if (onScreen)
        {
            pos = new Vector2((v.x - 0.5f) * canvasSize.x, (v.y - 0.5f) * canvasSize.y);
            rot = 45f;   // 정다이아몬드
        }
        else
        {
            Vector2 dir = v - new Vector2(0.5f, 0.5f);
            if (dir.sqrMagnitude < 1e-4f) dir = Vector2.up;
            dir.Normalize();
            Vector2 half = canvasSize * 0.5f - Vector2.one * edgePadding;
            float sx = Mathf.Abs(dir.x) > 1e-4f ? half.x / Mathf.Abs(dir.x) : float.MaxValue;
            float sy = Mathf.Abs(dir.y) > 1e-4f ? half.y / Mathf.Abs(dir.y) : float.MaxValue;
            pos = dir * Mathf.Min(sx, sy);
            rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 45f;   // 다이아몬드 꼭짓점이 방향을 향하게
        }

        marker.anchoredPosition = pos;
        marker.localRotation = Quaternion.Euler(0f, 0f, rot);
        if (!marker.gameObject.activeSelf) marker.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (marker != null) Destroy(marker.gameObject);
    }
}
