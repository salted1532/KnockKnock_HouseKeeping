using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    private const int SlotCount = 5;

    [SerializeField] private Image[] slotIcons = new Image[SlotCount];
    [SerializeField] private GameObject[] activateIcons = new GameObject[SlotCount];

    private readonly Sprite[] itemIcons = new Sprite[SlotCount];
    private readonly GameObject[] equipTargets = new GameObject[SlotCount];
    private int activeSlot = -1;

    private void Awake()
    {
        Instance = this;

        foreach (GameObject activateIcon in activateIcons)
            if (activateIcon != null)
                activateIcon.SetActive(false);
    }

    private void Start()
    {
        SelectSlot(0);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
        else if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
    }

    public bool AddItem(Sprite icon, GameObject equipTarget)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (equipTargets[i] != null)
                continue;

            itemIcons[i] = icon;
            equipTargets[i] = equipTarget;
            equipTarget.SetActive(i == activeSlot);

            if (slotIcons[i] != null)
                slotIcons[i].sprite = icon;
            return true;
        }
        return false;
    }

    private void SelectSlot(int index)
    {
        if (activeSlot >= 0 && equipTargets[activeSlot] != null)
            equipTargets[activeSlot].SetActive(false);

        if (activeSlot >= 0 && activateIcons[activeSlot] != null)
            activateIcons[activeSlot].SetActive(false);

        activeSlot = index;

        if (equipTargets[index] != null)
            equipTargets[index].SetActive(true);

        if (activateIcons[index] != null)
            activateIcons[index].SetActive(true);
    }
}
