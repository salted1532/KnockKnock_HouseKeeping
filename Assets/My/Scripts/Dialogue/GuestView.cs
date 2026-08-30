using UnityEngine;

// 재활용되는 손님 오브젝트의 겉모습. 접객 큐가 손님마다 Apply(npc) 로 스프라이트를 바꾼다.
// 대화 중에는 DialogueRunner.OnLineShown 을 구독해 줄별 표정(Neutral/Angry)도 반영.
public class GuestView : MonoBehaviour
{
    // 걷는 구간별 바라보는 방향. Auto = 화면 이동방향으로 자동 판정(SetSide).
    public enum Facing { Auto, Front, Back, Left, Right }

    [Tooltip("손님 몸/얼굴 스프라이트 (2D). NpcData 초상화로 교체됨")]
    [SerializeField] private SpriteRenderer body;
    [Tooltip("손님 스프라이트 목표 높이(월드 유닛). 초상화마다 해상도가 달라도 이 높이에 맞춰 스케일. 0 이면 스케일 안 건드림")]
    [SerializeField] private float worldHeight = 3.2f;
    [Tooltip("스프라이트 바닥이 놓일 루트 기준 로컬 Y. 0 = 루트(발밑)가 지면. 양수면 위로 띄움")]
    [SerializeField] private float feetLocalY = 0f;
    [Tooltip("있으면 스프라이트 높이에 맞춰 캡슐 콜라이더 크기/중심도 동기화")]
    [SerializeField] private CapsuleCollider bodyCollider;
    [Tooltip("콜라이더 반지름 = worldHeight × 이 비율")]
    [SerializeField] private float colliderRadiusRatio = 0.16f;

    private NpcData npc;
    private Sprite restSprite;   // 걷기 중이 아닐 때의 기본 스프라이트 (SetSide → EndSide 로 복귀)

    private void Reset() => body = GetComponentInChildren<SpriteRenderer>();

    public void Apply(NpcData data)
    {
        npc = data;
        SetExpression(Expression.Neutral);
        if (DialogueRunner.Instance != null)
            DialogueRunner.Instance.OnLineShown += OnLine;
    }

    public void Clear()
    {
        if (DialogueRunner.Instance != null)
            DialogueRunner.Instance.OnLineShown -= OnLine;
        npc = null;
    }

    private void OnDisable() => Clear();

    private void OnLine(NpcData shown, DialogueLine line)
    {
        if (shown == npc) SetExpression(line.expression);
    }

    // 퇴장 시 뒷모습으로. backPortrait 없으면 그대로 둔다.
    public void ShowBack()
    {
        if (npc == null || npc.backPortrait == null) return;
        restSprite = npc.backPortrait;
        ApplySprite(restSprite);
        if (body != null) body.flipX = false;
    }

    private void SetExpression(Expression e)
    {
        if (npc == null) return;
        restSprite = npc.Portrait(e);
        ApplySprite(restSprite);
        if (body != null) body.flipX = false;
    }

    // 걷는 구간의 방향 지정. Auto 면 이동방향 자동 판정(SetSide), 아니면 명시된 스프라이트로.
    public void SetWalkFacing(Facing f, Vector3 worldMoveDir)
    {
        if (body == null || npc == null) return;
        switch (f)
        {
            case Facing.Auto:  SetSide(worldMoveDir);                        break;
            case Facing.Front: SwapWalk(npc.Portrait(Expression.Neutral), false); break;
            case Facing.Back:  SwapWalk(npc.backPortrait, false);            break;
            case Facing.Left:  SwapWalk(npc.sidePortrait, true);             break;
            case Facing.Right: SwapWalk(npc.sidePortrait, false);            break;
        }
    }

    private void SwapWalk(Sprite s, bool flip)
    {
        if (s == null) return;
        if (body.sprite != s) ApplySprite(s);   // 매 프레임 스케일 재계산 방지
        body.flipX = flip;
    }

    // 걷는 중 옆모습. worldMoveDir = 이번 프레임 정규화 이동 방향(y 무시).
    // 화면 기준 수평 성분이 작으면(깊이 방향) 정면/뒷모습 그대로 둔다.
    public void SetSide(Vector3 worldMoveDir)
    {
        if (body == null || npc == null || npc.sidePortrait == null) return;

        var cam = Camera.main;
        float screenX = cam != null ? Vector3.Dot(worldMoveDir, cam.transform.right) : worldMoveDir.x;
        if (Mathf.Abs(screenX) < 0.15f) return;

        if (body.sprite != npc.sidePortrait) ApplySprite(npc.sidePortrait);
        body.flipX = screenX < 0f;   // sidePortrait 원본이 화면 오른쪽을 향함 → 왼쪽 이동 시 반전
    }

    // 걷기 종료 → 기본 스프라이트로 복귀.
    public void EndSide()
    {
        if (body == null || restSprite == null) return;
        if (body.sprite != restSprite) ApplySprite(restSprite);
        body.flipX = false;
    }

    private void ApplySprite(Sprite s)
    {
        if (body == null) return;
        body.sprite = s;
        if (s == null || worldHeight <= 0f) return;

        float h = s.bounds.size.y;        // PPU 반영된 유닛 높이
        if (h <= 0.0001f) return;

        // 초상화 원본 해상도가 제각각이라 body 트랜스폼을 목표 높이에 맞춰 균일 스케일.
        float k = worldHeight / h;
        var ls = body.transform.localScale;
        body.transform.localScale = new Vector3(k, k, ls.z);

        // 스프라이트 바닥(피벗 무관, bounds.min.y)을 feetLocalY 에 맞춘다 → 지면에 안 박히고 안 뜬다.
        float bottomFromPivot = s.bounds.min.y * k;   // 피벗 → 바닥 (보통 음수)
        var lp = body.transform.localPosition;
        body.transform.localPosition = new Vector3(lp.x, feetLocalY - bottomFromPivot, lp.z);

        // 콜라이더도 같은 높이에 맞춤.
        if (bodyCollider != null)
        {
            bodyCollider.height = worldHeight;
            bodyCollider.radius = worldHeight * colliderRadiusRatio;
            var c = bodyCollider.center;
            bodyCollider.center = new Vector3(c.x, feetLocalY + worldHeight * 0.5f, c.z);
        }
    }
}
