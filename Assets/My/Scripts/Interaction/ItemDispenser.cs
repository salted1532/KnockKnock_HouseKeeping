using UnityEngine;

public class ItemDispenser : MonoBehaviour
{
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private GameObject equipTarget;

    public void Dispense()
    {
        if (itemPrefab == null)
            return;

        GameObject spawned = Instantiate(itemPrefab, transform.position, transform.rotation);
        spawned.GetComponent<Interactable>()?.SetEquipTarget(equipTarget);
    }
}
