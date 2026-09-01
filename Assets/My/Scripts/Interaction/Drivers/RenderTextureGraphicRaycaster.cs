using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// World Space Canvas 를 "MainCamera → RenderTexture(Posterize) → 풀스크린 RawImage" 경로로 보는
// 화면(CRT 모니터 등)에 붙인다. 일반 GraphicRaycaster 는 마우스 스크린 좌표를 그대로 Event Camera 에
// 넘기는데, 게임 화면이 1280x720 RT 를 거쳐 실제 해상도로 blit 되므로 좌표·FOV 가 어긋나 버튼이
// 엉뚱한 자리에서 눌린다. 여기서 커서를 RawImage 사각형 기준으로 정규화 → worldCamera 렌더타깃
// 픽셀 공간으로 변환한 뒤 base.Raycast 를 호출한다. EventSystem/InputModule 은 그대로 두므로
// Button/Toggle/Slider 등 uGUI 가 정상 동작한다. (CursorInteractor.TryCursorRay 와 같은 변환)
//
// 배선: Canvas(Render Mode = World Space) 에 이 컴포넌트를 GraphicRaycaster 대신 붙이기만 하면 된다.
//   worldCamera / screen / canvasCamera 는 비워두면 씬의 CursorInteractor 에서 자동으로 가져온다
//   (같은 RT 파이프라인). 프리팹이라 씬 참조를 못 담아도 런타임에 해결됨. Event Camera 자동 설정.
[RequireComponent(typeof(Canvas))]
public class RenderTextureGraphicRaycaster : GraphicRaycaster
{
    [Header("비워두면 CursorInteractor 에서 자동 참조")]
    [Tooltip("월드를 RenderTexture 로 그리는 카메라 (MainCamera)")]
    [SerializeField] private Camera worldCamera;
    [Tooltip("그 RenderTexture 를 표시하는 풀스크린 RawImage")]
    [SerializeField] private RawImage screen;
    [Tooltip("RawImage 캔버스를 그리는 카메라. Screen Space - Overlay 면 비워둔다")]
    [SerializeField] private Camera canvasCamera;

    private Canvas canvas;

    protected override void OnEnable()
    {
        base.OnEnable();
        canvas = GetComponent<Canvas>();
        EnsureRefs();

        // 월드 캔버스가 카메라를 등지게 배치되면(캔버스 forward 가 카메라 forward 와 반대) 기본
        // GraphicRaycaster 는 "뒤집힌 그래픽"으로 보고 클릭을 전부 버린다. 화면 메쉬 방향에 따라
        // 캔버스를 어느 쪽으로 돌리든 클릭이 되게 항상 끈다. (등지면 렌더가 미러링되니 그건 눈에 띔)
        ignoreReversedGraphics = false;
    }

    // CursorInteractor 가 아직 없을 수도 있어(초기화 순서) Raycast 에서도 재시도.
    private void EnsureRefs()
    {
        if (worldCamera != null && screen != null) { LinkEventCamera(); return; }

        var ci = FindAnyObjectByType<CursorInteractor>(FindObjectsInactive.Include);
        if (ci != null)
        {
            if (worldCamera == null) worldCamera = ci.WorldCamera;
            if (screen == null) screen = ci.Screen;
            if (canvasCamera == null) canvasCamera = ci.CanvasCamera;
        }
        if (worldCamera == null) worldCamera = Camera.main;
        LinkEventCamera();
    }

    // World Space 캔버스의 Event Camera. base.Raycast 가 이 카메라로 ScreenPointToRay 한다.
    private void LinkEventCamera()
    {
        if (canvas != null && canvas.worldCamera == null && worldCamera != null)
            canvas.worldCamera = worldCamera;
    }

    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        if (worldCamera == null || screen == null)
        {
            EnsureRefs();
            if (worldCamera == null || screen == null) { base.Raycast(eventData, resultAppendList); return; }
        }

        // 커서가 자유로울 때(화면고정/오버레이 UI 모드)만 반응 — 게임플레이 중 조준점 클릭이
        // 화면 버튼을 누르는 것 방지. ponytail: UIInteractionMode 전역 Active 로 게이트.
        // 접객 모드에서도 켜지지만 그때도 커서가 있으니 무해 (접객→모니터 스택 시 오히려 필요).
        if (UIInteractionMode.Instance == null || !UIInteractionMode.Instance.Active)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                screen.rectTransform, eventData.position, canvasCamera, out Vector2 local))
            return;

        Rect r = screen.rectTransform.rect;
        float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
        if (u < 0f || u > 1f || v < 0f || v > 1f) return;   // 화면 밖

        Rect uv = screen.uvRect;                            // 기본 (0,0,1,1)
        Vector2 remapped = new(
            (uv.x + u * uv.width) * worldCamera.pixelWidth,
            (uv.y + v * uv.height) * worldCamera.pixelHeight);

        Vector2 saved = eventData.position;
        eventData.position = remapped;
        base.Raycast(eventData, resultAppendList);
        eventData.position = saved;   // 나머지 모듈(드래그/델타 계산)이 실제 커서 위치를 쓰도록 복원
    }
}
