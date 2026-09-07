using DG.Tweening;
using UnityEngine;

// 손님이 걷는 동안 스프라이트를 발걸음 박자에 맞춰 위아래로 통통 튀게(총총총) 한다.
// Guest.prefab 의 Square(= GuestView.body) 에 붙인다. GuestMover.Walking 을 따라간다.
// GuestView.ApplySprite 가 스프라이트 교체 시 localPosition 을 다시 쓰므로, 그 프레임에 base 를 재캡처해 충돌을 피한다.
public class GuestWalkBob : MonoBehaviour
{
    [SerializeField] private GuestMover mover;
    [SerializeField] private SpriteRenderer body;

    [Header("총총 스텝 (상하 튐)")]
    [Tooltip("한 발 튈 때 올라가는 높이(월드 유닛)")]
    [SerializeField] private float hopHeight = 0.07f;
    [Tooltip("한 발 = 이 시간(초). 짧을수록 총총총")]
    [SerializeField] private float stepInterval = 0.32f;

    private Vector3 baseLocalPos;
    private Sprite lastSprite;
    private bool bobbing;
    private float bobY;

    private void Awake()
    {
        if (mover == null) mover = GetComponentInParent<GuestMover>();
        if (body == null) body = GetComponent<SpriteRenderer>();
        baseLocalPos = transform.localPosition;
        lastSprite = body != null ? body.sprite : null;
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
        bobbing = false;
        bobY = 0f;
    }

    private void LateUpdate()
    {
        // GuestView 가 스프라이트를 바꾼 프레임 → 그 localPosition 이 새 base (bob 안 섞임)
        if (body != null && body.sprite != lastSprite)
        {
            lastSprite = body.sprite;
            baseLocalPos = transform.localPosition;
        }

        bool walking = mover != null && mover.Walking;
        if (walking && !bobbing) StartBob();
        else if (!walking && bobbing) StopBob();

        if (bobbing || bobY != 0f)
            transform.localPosition = baseLocalPos + new Vector3(0f, bobY, 0f);
    }

    private void StartBob()
    {
        bobbing = true;
        DOTween.Kill(this);
        DOTween.To(() => bobY, v => bobY = v, hopHeight, stepInterval)
            .From(0f).SetEase(Ease.OutQuad).SetLoops(-1, LoopType.Yoyo)
            .SetTarget(this).SetLink(gameObject);
    }

    private void StopBob()
    {
        bobbing = false;
        DOTween.Kill(this);
        DOTween.To(() => bobY, v => bobY = v, 0f, 0.15f).SetTarget(this).SetLink(gameObject);
    }
}
