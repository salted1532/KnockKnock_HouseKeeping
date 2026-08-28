using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 마우스 커서 레이 → 호버 아웃라인, 좌클릭 상호작용. UI 모드(UIInteractionMode)에서만 활성화된다.
//
// 게임 화면은 MainCamera(FOV 40) → RenderTexture → 풀스크린 RawImage(PxlCrush) → 화면 순으로 그려지므로
// 커서 스크린 좌표를 그대로 카메라에 넘기면 어긋난다. RawImage 사각형 기준으로 정규화해서
// worldCamera 뷰포트 레이로 변환한다 (FOV/해상도/종횡비 무관).
public class CursorInteractor : Interactor
{
    [SerializeField] private float interactDistance = 5f;
    [SerializeField] private LayerMask interactMask = ~0;
    [Tooltip("월드를 RenderTexture 로 그리는 카메라 (MainCamera)")]
    [SerializeField] private Camera worldCamera;
    [Tooltip("그 RenderTexture 를 표시하는 풀스크린 RawImage")]
    [SerializeField] private RawImage screen;
    [Tooltip("RawImage 캔버스를 그리는 카메라. Screen Space - Overlay 면 비워둔다")]
    [SerializeField] private Camera canvasCamera;

    private Outline currentOutline;

    private void Reset() => worldCamera = Camera.main;
    private void OnDisable() => ClearOutline();

    private void Update()
    {
        if (Mouse.current == null || worldCamera == null || screen == null) return;

        if (!TryCursorRay(out Ray ray)) { ClearOutline(); return; }

        Interactable hovered = null;
        Outline hitOutline = null;
        Vector3 point = Vector3.zero;

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            hitOutline = hit.collider.GetComponentInParent<Outline>();
            var candidate = hit.collider.GetComponentInParent<Interactable>();
            if (candidate != null && candidate.CanInteract)
                hovered = candidate;
        }

        if (hitOutline != currentOutline)
        {
            if (currentOutline != null) currentOutline.enabled = false;
            if (hitOutline != null) hitOutline.enabled = true;
            currentOutline = hitOutline;
        }

        if (hovered != null && Mouse.current.leftButton.wasPressedThisFrame)
            hovered.Interact(this, point);
    }

    // 스크린 커서 → RawImage 로컬 좌표 → 정규화 뷰포트 → worldCamera 레이
    private bool TryCursorRay(out Ray ray)
    {
        ray = default;

        Vector2 sp = Mouse.current.position.ReadValue();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                screen.rectTransform, sp, canvasCamera, out Vector2 local))
            return false;

        Rect r = screen.rectTransform.rect;
        float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
        if (u < 0f || u > 1f || v < 0f || v > 1f) return false;   // 화면 밖

        Rect uv = screen.uvRect;                                  // 기본 (0,0,1,1)
        ray = worldCamera.ViewportPointToRay(
            new Vector3(uv.x + u * uv.width, uv.y + v * uv.height, 0f));
        return true;
    }

    private void ClearOutline()
    {
        if (currentOutline != null) currentOutline.enabled = false;
        currentOutline = null;
    }
}
