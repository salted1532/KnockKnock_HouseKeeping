using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Game.PlayerHandItem;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    private const int SlotCount = 5;
    private static readonly Color EmptySlotColor = new Color(0.32156864f, 0.32156864f, 0.32156864f, 1f);

    [SerializeField] private Image[] slotIcons = new Image[SlotCount];
    [SerializeField] private GameObject[] activateIcons = new GameObject[SlotCount];
    [SerializeField] private Transform throwPos;
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private AudioSource audioSource;

    private readonly Sprite[] itemIcons = new Sprite[SlotCount];
    private readonly GameObject[] equipTargets = new GameObject[SlotCount];
    private readonly GameObject[] pickupSources = new GameObject[SlotCount];
    private readonly bool[] isFlashlightSlot = new bool[SlotCount];
    private readonly AudioClip[] useClips = new AudioClip[SlotCount];
    private readonly bool[] consumeOnUseSlot = new bool[SlotCount];
    private readonly Flashlight[] flashlightRefs = new Flashlight[SlotCount];
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

        if (Keyboard.current.fKey.wasPressedThisFrame) ThrowActiveItem();
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) UseActiveItem();

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0f && !IsActiveFlashlightOn())
                SelectSlot((activeSlot + (scroll > 0f ? SlotCount - 1 : 1)) % SlotCount);
        }
    }

    public bool AddItem(Sprite icon, GameObject equipTarget, GameObject pickupSource, bool isFlashlight = false, AudioClip useClip = null, bool consumeOnUse = false)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (equipTargets[i] != null)
                continue;

            itemIcons[i] = icon;
            equipTargets[i] = equipTarget;
            pickupSources[i] = pickupSource;
            isFlashlightSlot[i] = isFlashlight;
            useClips[i] = useClip;
            consumeOnUseSlot[i] = consumeOnUse;
            flashlightRefs[i] = isFlashlight ? equipTarget.GetComponentInChildren<Flashlight>(true) : null;
            equipTarget.SetActive(i == activeSlot);

            if (slotIcons[i] != null)
            {
                slotIcons[i].sprite = icon;
                slotIcons[i].color = Color.white;
            }

            UpdateFlashlightHint();
            return true;
        }
        return false;
    }

    private void ThrowActiveItem()
    {
        if (activeSlot < 0 || equipTargets[activeSlot] == null || throwPos == null)
            return;

        GameObject heldItem = equipTargets[activeSlot];
        GameObject thrownItem = pickupSources[activeSlot];

        heldItem.SetActive(false);

        if (thrownItem != null)
        {
            thrownItem.SetActive(true);
            thrownItem.transform.SetParent(null);

            Transform origin = throwPos.parent != null ? throwPos.parent : throwPos;
            Vector3 spawnPos = throwPos.position;
            Vector3 offset = spawnPos - origin.position;
            float checkDist = offset.magnitude;
            if (checkDist > 0.01f)
            {
                Transform playerRoot = throwPos.root;
                Vector3 dir = offset.normalized;
                float closest = checkDist;
                bool blocked = false;
                foreach (RaycastHit h in Physics.SphereCastAll(origin.position, 0.1f, dir, checkDist, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.transform.IsChildOf(playerRoot) || h.distance >= closest)
                        continue;
                    closest = h.distance;
                    blocked = true;
                }
                if (blocked)
                    spawnPos = origin.position + dir * Mathf.Max(closest - 0.15f, 0f);
            }

            thrownItem.transform.SetPositionAndRotation(spawnPos, throwPos.rotation);

            Rigidbody rb = thrownItem.GetComponent<Rigidbody>();
            if (rb == null)
                rb = thrownItem.AddComponent<Rigidbody>();
            rb.isKinematic = false;   // 고리에 걸렸다 다시 주워 던지는 경로 대비
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.AddForce(throwPos.forward * throwForce, ForceMode.Impulse);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            CharacterController playerController = player != null ? player.GetComponent<CharacterController>() : null;
            if (playerController != null)
            {
                foreach (Collider itemCollider in thrownItem.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(itemCollider, playerController, true);
            }
        }

        ClearSlot(activeSlot);
    }

    private void UseActiveItem()
    {
        if (activeSlot < 0 || equipTargets[activeSlot] == null || useClips[activeSlot] == null)
            return;

        if (audioSource != null)
            audioSource.PlayOneShot(useClips[activeSlot]);

        if (consumeOnUseSlot[activeSlot])
        {
            if (pickupSources[activeSlot] != null)
                Destroy(pickupSources[activeSlot]);
            equipTargets[activeSlot].SetActive(false);
            ClearSlot(activeSlot);
        }
    }

    // 현재 활성 슬롯(손에 든 것)의 HandItem. 없으면 null.
    public HandItem ActiveHandItem =>
        activeSlot >= 0 && equipTargets[activeSlot] != null
            ? equipTargets[activeSlot].GetComponentInChildren<HandItem>(true)
            : null;

    // 활성 슬롯을 비우고 원래 월드 오브젝트(pickupSource)를 반환. 빈 손이면 null.
    // HookEffect 가 "들고 있는 아이템을 고리에 건다" 용도로 사용.
    public GameObject RemoveActiveItem()
    {
        if (activeSlot < 0 || equipTargets[activeSlot] == null) return null;
        GameObject pickupSource = pickupSources[activeSlot];
        equipTargets[activeSlot].SetActive(false);
        ClearSlot(activeSlot);
        return pickupSource;
    }

    private void ClearSlot(int index)
    {
        equipTargets[index] = null;
        itemIcons[index] = null;
        pickupSources[index] = null;
        isFlashlightSlot[index] = false;
        useClips[index] = null;
        consumeOnUseSlot[index] = false;
        flashlightRefs[index] = null;
        if (slotIcons[index] != null)
        {
            slotIcons[index].sprite = null;
            slotIcons[index].color = EmptySlotColor;
        }
        UpdateFlashlightHint();
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

        UpdateFlashlightHint();
    }

    private bool IsActiveFlashlightOn() =>
        activeSlot >= 0 && isFlashlightSlot[activeSlot] &&
        flashlightRefs[activeSlot] != null && flashlightRefs[activeSlot].IsOpen;

    private void UpdateFlashlightHint()
    {
        bool holdingFlashlight = activeSlot >= 0 && equipTargets[activeSlot] != null && isFlashlightSlot[activeSlot];

        GameObject canvas = GameObject.Find("Canvas");
        Transform hint = canvas != null ? canvas.transform.Find("HowToUse_Flashlight") : null;
        if (hint != null)
            hint.gameObject.SetActive(holdingFlashlight);
    }
}
