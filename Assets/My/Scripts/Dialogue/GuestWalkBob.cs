using DG.Tweening;
using UnityEngine;

// 손님이 걷는 동안 스프라이트를 발걸음 박자에 맞춰 위아래로 통통 튀게(총총총) 한다.
// 멈춰 서 있을 때(대화 등)는 대신 가슴이 오르내리듯 세로 스케일을 아주 살짝 흔들어 "숨쉬기"를 준다.
// Guest.prefab 의 Square(= GuestView.body) 에 붙인다. GuestMover.Walking 을 따라간다.
// GuestView.ApplySprite 가 스프라이트 교체 시 localPosition/Scale 을 다시 쓰므로, 그 프레임에 base 를 재캡처해 충돌을 피한다.
public class GuestWalkBob : MonoBehaviour
{
    [SerializeField] private GuestMover mover;
    [SerializeField] private SpriteRenderer body;

    [Header("총총 스텝 (상하 튐)")]
    [Tooltip("한 발 튈 때 올라가는 높이(월드 유닛)")]
    [SerializeField] private float hopHeight = 0.07f;
    [Tooltip("한 발 = 이 시간(초). 짧을수록 총총총")]
    [SerializeField] private float stepInterval = 0.32f;

    [Header("숨쉬기 (멈춰 있을 때)")]
    [Tooltip("가만히 있을 때 세로 스케일이 커졌다 작아지는 폭 (0.012 = ±1.2%, 0 = 끔)")]
    [SerializeField] private float breatheAmount = 0.012f;
    [Tooltip("숨 한 번 주기(초)")]
    [SerializeField] private float breathePeriod = 3.6f;

    private Vector3 baseLocalPos;
    private Vector3 baseLocalScale;
    private Sprite lastSprite;
    private bool bobbing;
    private float bobY;

    private void Awake()
    {
        if (mover == null) mover = GetComponentInParent<GuestMover>();
        if (body == null) body = GetComponent<SpriteRenderer>();
        baseLocalPos = transform.localPosition;
        baseLocalScale = transform.localScale;
        lastSprite = body != null ? body.sprite : null;
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
        bobbing = false;
        bobY = 0f;
        transform.localScale = baseLocalScale;
    }

    private void LateUpdate()
    {
        // GuestView 가 스프라이트를 바꾼 프레임 → 그 localPosition/Scale 이 새 base (bob·숨쉬기 안 섞임)
        if (body != null && body.sprite != lastSprite)
        {
            lastSprite = body.sprite;
            baseLocalPos = transform.localPosition;
            baseLocalScale = transform.localScale;
        }

        bool walking = mover != null && mover.Walking;
        if (walking && !bobbing) StartBob();
        else if (!walking && bobbing) StopBob();

        if (bobbing || bobY != 0f)
            transform.localPosition = baseLocalPos + new Vector3(0f, bobY, 0f);

        // 멈춰 있을 때만 숨쉬기 — 걷는 중엔 총총 스텝이 우선.
        // ponytail: 피벗 기준 스케일이라 ±1.2% 면 발끝이 ~2cm 떴다 가라앉음 — 눈에 안 띄는 수준이라 보정 생략.
        if (!walking && breatheAmount > 0f && breathePeriod > 0f)
        {
            float s = 1f + Mathf.Sin(Time.time * (2f * Mathf.PI / breathePeriod)) * breatheAmount;
            transform.localScale = new Vector3(baseLocalScale.x, baseLocalScale.y * s, baseLocalScale.z);
        }
        else if (!walking && transform.localScale != baseLocalScale)
        {
            transform.localScale = baseLocalScale;
        }
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
