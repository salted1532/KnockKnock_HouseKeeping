using UnityEngine;
using UnityEngine.Events;

public enum InteractionType
{
    Pickup,
    TidyBed,
    Generic,
}

public class Interactable : MonoBehaviour
{
    [SerializeField] private InteractionType type;

    [Header("Pickup")]
    [SerializeField] private string itemName;

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
            case InteractionType.Generic:
                onInteract?.Invoke();
                break;
        }
    }

    private void Pickup()
    {
        Debug.Log($"Picked up {itemName}");
        Destroy(gameObject);
    }

    private void TidyBed()
    {
        if (messyVisual != null) messyVisual.SetActive(false);
        if (tidyVisual != null) tidyVisual.SetActive(true);
    }
}
