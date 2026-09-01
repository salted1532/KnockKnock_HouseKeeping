using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("프롬프트 (선택)")]
    [Tooltip("상호작용 가능한 대상에 호버 시 켜지는 UI. 비우면 표시 안 함")]
    [SerializeField] private GameObject promptRoot;
    [Tooltip("promptRoot 안의 문구 라벨. Interactable.Prompt 로 채워짐")]
    [SerializeField] private TMP_Text promptLabel;

    private Outline currentOutline;
    private SpriteOutline currentSprite;
    private Interactable currentHovered;

    private readonly List<RaycastResult> uiHits = new();
    private PointerEventData uiPointer;

    // RenderTextureGraphicRaycaster 등이 같은 RT 파이프라인 참조를 재사용
    public Camera WorldCamera => worldCamera;
    public RawImage Screen => screen;
    public Camera CanvasCamera => canvasCamera;

    private void Reset() => worldCamera = Camera.main;
    private void OnDisable() => ClearHover();

    private void Update()
    {
        if (Mouse.current == null || worldCamera == null || screen == null) return;

        Interactable hovered = null;
        Outline hitOutline = null;
        SpriteOutline hitSprite = null;
        Vector3 point = Vector3.zero;

        // 커서가 UI 위(대화 패널·버튼·모니터 화면)면 월드 레이는 안 쏘지만, 그 UI 가 얹힌
        // Interactable(모니터 등)이 있으면 그 오브젝트 외곽선은 켠다. 클릭·프롬프트는 안 건드림
        // (UI 요소가 자기 클릭 처리, 모니터 배경은 InteractableProxyClick). doc/0141.
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (overUI)
        {
            ResolveUIOutline(out hitOutline, out hitSprite);
        }
        else if (TryCursorRay(out Ray ray)
                 && Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            // 벽 너머 상호작용 차단: 대상 앞에 막는 콜라이더(Interaction / Ignore Raycast 제외)가 있으면 무시 (GazeInteractor 와 동일)
            const int ignoreRaycastLayer = 2;
            int occlusionMask = ~interactMask.value & ~(1 << ignoreRaycastLayer);
            if (!Physics.Raycast(ray, hit.distance - 0.01f, occlusionMask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                var candidate = hit.collider.GetComponentInParent<Interactable>();
                if (candidate != null && candidate.CanInteract)
                {
                    hovered = candidate;
                    // 아웃라인/하이라이트도 상호작용 가능할 때만 (조건 불만족 시 안 뜸)
                    hitOutline = hit.collider.GetComponentInParent<Outline>();
                    hitSprite = hit.collider.GetComponentInParent<SpriteOutline>();
                }
            }
        }

        if (hitOutline != currentOutline)
        {
            if (currentOutline != null) currentOutline.enabled = false;
            if (hitOutline != null) hitOutline.enabled = true;
            currentOutline = hitOutline;
        }

        if (hitSprite != currentSprite)
        {
            if (currentSprite != null) currentSprite.SetHighlighted(false);
            if (hitSprite != null) hitSprite.SetHighlighted(true);
            currentSprite = hitSprite;
        }

        if (hovered != currentHovered)
        {
            currentHovered = hovered;
            if (promptRoot != null) promptRoot.SetActive(hovered != null);
            if (promptLabel != null && hovered != null) promptLabel.text = hovered.Prompt;
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

    // 커서 밑 uGUI 요소가 얹혀 있는 Interactable(모니터 화면 등)의 외곽선 컴포넌트를 찾는다.
    // 이 화면 UI 를 호버해도 모니터 메쉬에 외곽선이 뜨도록. (프리팹 구조: CRTMonitor > screenON > ScreenUI(Canvas) > 버튼)
    private void ResolveUIOutline(out Outline ol, out SpriteOutline so)
    {
        ol = null;
        so = null;
        var es = EventSystem.current;
        if (es == null) return;

        uiPointer ??= new PointerEventData(es);
        uiPointer.position = Mouse.current.position.ReadValue();
        uiHits.Clear();
        es.RaycastAll(uiPointer, uiHits);

        foreach (var r in uiHits)
        {
            if (r.gameObject == null) continue;
            var it = r.gameObject.GetComponentInParent<Interactable>();
            if (it == null || !it.CanInteract) continue;
            ol = r.gameObject.GetComponentInParent<Outline>();
            so = r.gameObject.GetComponentInParent<SpriteOutline>();
            return;
        }
    }

    private void ClearHover()
    {
        if (currentOutline != null) currentOutline.enabled = false;
        currentOutline = null;
        if (currentSprite != null) currentSprite.SetHighlighted(false);
        currentSprite = null;
        currentHovered = null;
        if (promptRoot != null) promptRoot.SetActive(false);
    }
}
