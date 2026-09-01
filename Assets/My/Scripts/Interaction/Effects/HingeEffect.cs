using System.Collections;
using UnityEngine;

// 경첩 회전으로 여닫기. (구 Door.cs 대체)
// hinge 를 지정하면 그 Transform 을, 비우면 이 오브젝트 transform 을 회전시킨다.
// axis 는 hinge 의 로컬 회전축. Interactable.isToggle 을 켜서 쓴다.
public class HingeEffect : InteractionEffect
{
    [Tooltip("회전시킬 경첩 Transform. 비우면 이 오브젝트")]
    [SerializeField] private Transform hinge;
    [Tooltip("hinge 로컬 기준 회전축 (문=위쪽, 뚜껑=옆쪽 등)")]
    [SerializeField] private Vector3 axis = Vector3.up;
    [Tooltip("열림 각도. 음수면 반대 방향")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openTime = 0.6f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Transform Target => hinge != null ? hinge : transform;

    private Quaternion closedRot;
    private Quaternion openRot;
    private Coroutine swing;

    private void Awake()
    {
        Vector3 a = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;
        closedRot = Target.localRotation;
        openRot = closedRot * Quaternion.AngleAxis(openAngle, a);
    }

    private void Start()
    {
        Target.localRotation = GetComponent<Interactable>().IsOn ? openRot : closedRot;
    }

    public override void Play(in InteractionContext ctx)
    {
        bool open = ctx.Interactable.IsToggle ? ctx.IsOn : true;
        StartSwing(open ? openRot : closedRot, openTime);
    }

    // 코드/연출용: 닫힘 기준 임의 각도로 스윙 (KnockEffect 노크 살짝 열기 등). Interactable.IsOn 안 건드림.
    public void SwingTo(float angleDeg, float time)
    {
        Vector3 a = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;
        StartSwing(closedRot * Quaternion.AngleAxis(angleDeg, a), Mathf.Max(0.01f, time));
    }

    private void StartSwing(Quaternion target, float dur)
    {
        if (swing != null) StopCoroutine(swing);
        swing = StartCoroutine(Swing(target, dur));
    }

    private IEnumerator Swing(Quaternion target, float dur)
    {
        Transform tr = Target;
        Quaternion from = tr.localRotation;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            tr.localRotation = Quaternion.Slerp(from, target, ease.Evaluate(Mathf.Clamp01(t / dur)));
            yield return null;
        }
        tr.localRotation = target;
        swing = null;
    }
}
