using UnityEngine;
using UnityEngine.Events;

public enum InteractionType
{
    Pickup,
    TidyBed,
    Generic,
    Flashlight,
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

    public void Interact()
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
}
