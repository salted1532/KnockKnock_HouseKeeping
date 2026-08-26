using UnityEngine;
using UnityEngine.Events;

public enum InteractionType
{
    Pickup,
    TidyBed,
    Generic,
    Flashlight,
    Push,
}

public class Interactable : MonoBehaviour
{
    [SerializeField] private InteractionType type;

    [Header("Pickup")]
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private GameObject equipTarget;
    [SerializeField] private AudioClip useClip;
    [SerializeField] private bool consumeOnUse;

    public void SetEquipTarget(GameObject target) => equipTarget = target;

    [Header("TidyBed")]
    [SerializeField] private GameObject messyVisual;
    [SerializeField] private GameObject tidyVisual;

    [Header("Generic (버튼 등 자유 연출)")]
    [SerializeField] private UnityEvent onInteract;

    [Header("Push (상호작용 시 플레이어 반대 방향으로 밀기)")]
    [SerializeField] private float pushForce = 6f;
    [SerializeField] private float rotationForce = 2f;

    public void Interact(Vector3? hitPoint = null)
    {
        switch (type)
        {
            case InteractionType.Pickup:
                Pickup();
                break;
            case InteractionType.TidyBed:
                TidyBed();
                break;
            case InteractionType.Flashlight:
                ActivatePlayerFlashlight();
                break;
            case InteractionType.Push:
                PushAwayFromPlayer(hitPoint ?? transform.position);
                break;
        }

        onInteract?.Invoke();
    }

    private void Pickup()
    {
        if (equipTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        if (InventorySystem.Instance != null && InventorySystem.Instance.AddItem(itemIcon, equipTarget, gameObject, useClip: useClip, consumeOnUse: consumeOnUse))
            gameObject.SetActive(false);
    }

    private void TidyBed()
    {
        if (messyVisual != null) messyVisual.SetActive(false);
        if (tidyVisual != null) tidyVisual.SetActive(true);
    }

    private void ActivatePlayerFlashlight()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Transform flashlight = player != null ? player.transform.Find("PlayerCameraRoot/flashlight") : null;

        if (flashlight == null || InventorySystem.Instance == null || !InventorySystem.Instance.AddItem(itemIcon, flashlight.gameObject, gameObject, isFlashlight: true))
            return;

        gameObject.SetActive(false);
    }

    private void PushAwayFromPlayer(Vector3 hitPoint)
    {
        Rigidbody body = GetComponentInParent<Rigidbody>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (body == null || body.isKinematic || player == null)
            return;

        Vector3 dir = transform.position - player.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        dir.Normalize();
        body.AddForce(dir * pushForce, ForceMode.Impulse);

        Vector3 offset = hitPoint - body.worldCenterOfMass;
        Vector3 torque = Vector3.Cross(offset, dir) * rotationForce;
        Vector3 steerAxis = body.transform.forward; // 카트 로컬 Z(조향) 축
        body.AddTorque(Vector3.Dot(torque, steerAxis) * steerAxis, ForceMode.Impulse);
    }
}
