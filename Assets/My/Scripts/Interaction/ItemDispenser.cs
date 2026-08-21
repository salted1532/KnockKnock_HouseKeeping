using UnityEngine;

public class ItemDispenser : MonoBehaviour
{
    [SerializeField] private GameObject itemPrefab;

    public void Dispense()
    {
        if (itemPrefab != null)
            Instantiate(itemPrefab, transform.position, transform.rotation);
    }
}
