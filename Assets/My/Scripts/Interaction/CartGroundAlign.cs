using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CartGroundAlign : MonoBehaviour
{
    [SerializeField] private Transform frontLeft;
    [SerializeField] private Transform frontRight;
    [SerializeField] private Transform rearLeft;
    [SerializeField] private Transform rearRight;
    [SerializeField] private float rayDistance = 1f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float alignSpeed = 8f;

    private Rigidbody rb;

    private void Awake() => rb = GetComponent<Rigidbody>();

    private void FixedUpdate()
    {
        if (frontLeft == null || frontRight == null || rearLeft == null || rearRight == null)
            return;

        Vector3 fl = GetGroundPoint(frontLeft);
        Vector3 fr = GetGroundPoint(frontRight);
        Vector3 rl = GetGroundPoint(rearLeft);
        Vector3 rr = GetGroundPoint(rearRight);

        Vector3 rightEdge = (fr + rr) * 0.5f - (fl + rl) * 0.5f;
        Vector3 forwardEdge = (fl + fr) * 0.5f - (rl + rr) * 0.5f;
        Vector3 targetUp = Vector3.Cross(forwardEdge, rightEdge).normalized;

        Quaternion delta = Quaternion.FromToRotation(transform.forward, targetUp);
        Quaternion targetRot = delta * rb.rotation;
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, alignSpeed * Time.fixedDeltaTime));
    }

    private Vector3 GetGroundPoint(Transform wheel)
    {
        if (Physics.Raycast(wheel.position, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            Debug.DrawLine(wheel.position, hit.point, Color.green);
            return hit.point;
        }
        Vector3 fallback = wheel.position + Vector3.down * rayDistance;
        Debug.DrawLine(wheel.position, fallback, Color.red);
        return fallback;
    }
}
