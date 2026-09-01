using StarterAssets;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepSystem : MonoBehaviour
{
    [SerializeField] private float stepDistance = 2f;
    [SerializeField] private float rayDistance = 1.5f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float sprintPitch = 2f;

    private CharacterController controller;
    private StarterAssetsInputs input;
    private float distanceAccumulator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<StarterAssetsInputs>();
    }

    private void Update()
    {
        // 화면고정(노크·접객·모니터)이나 오버레이(노트·페이드 전환) 중엔 발소리 안 냄 —
        // CharacterController 가 꺼진 채 마지막 속도(controller.velocity)가 남아 발소리가 계속 트리거되던 문제.
        if (!controller.enabled ||
            (UIInteractionMode.Instance != null && UIInteractionMode.Instance.MovementLocked))
        {
            distanceAccumulator = 0f;
            return;
        }

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

        float pitch = (input != null && input.sprint) ? sprintPitch : 1f;
        SoundManager.Instance?.PlayFootstep(hit.collider.gameObject.layer, pitch);
    }
}
