using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepSystem : MonoBehaviour
{
    [SerializeField] private float stepDistance = 2f;
    [SerializeField] private float rayDistance = 1.5f;
    [SerializeField] private LayerMask groundMask = ~0;

    private CharacterController controller;
    private float distanceAccumulator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!controller.isGrounded)
        {
            distanceAccumulator = 0f;
            return;
        }

        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        float speed = horizontalVelocity.magnitude;
        if (speed < 0.1f)
        {
            distanceAccumulator = 0f;
            return;
        }

        distanceAccumulator += speed * Time.deltaTime;
        if (distanceAccumulator >= stepDistance)
        {
            distanceAccumulator = 0f;
            PlayFootstep();
        }
    }

    private void PlayFootstep()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            return;

        SoundManager.Instance?.PlayFootstep(hit.collider.gameObject.layer);
    }
}
