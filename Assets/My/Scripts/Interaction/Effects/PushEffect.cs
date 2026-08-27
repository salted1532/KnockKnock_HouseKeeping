using UnityEngine;

// 상호작용 시 부모 Rigidbody 를 주체 반대 방향으로 밀기. (구 Push 케이스 대체)
public class PushEffect : InteractionEffect
{
    [SerializeField] private float pushForce = 6f;
    [SerializeField] private float torqueForce = 2f;
    [Tooltip("토크를 로컬 Z(조향) 축으로만 제한 — 쇼핑카트용")]
    [SerializeField] private bool useSteerAxis = true;

    public override void Play(in InteractionContext ctx)
    {
        Rigidbody body = GetComponentInParent<Rigidbody>();
        GameObject source = ctx.Source != null ? ctx.Source : GameObject.FindGameObjectWithTag("Player");
        if (body == null || body.isKinematic || source == null) return;

        Vector3 dir = body.transform.position - source.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        body.AddForce(dir * pushForce, ForceMode.Impulse);

        Vector3 offset = ctx.Point - body.worldCenterOfMass;
        Vector3 torque = Vector3.Cross(offset, dir) * torqueForce;
        if (useSteerAxis)
        {
            Vector3 axis = body.transform.forward;
            body.AddTorque(Vector3.Dot(torque, axis) * axis, ForceMode.Impulse);
        }
        else
        {
            body.AddTorque(torque, ForceMode.Impulse);
        }
    }
}
