using UnityEngine;

public class HandHeldEating : MonoBehaviour
{
    [Header("Звуковые эффекты")]
    public AudioClip eatSound;           
    private AudioSource audioSource;

    private BlockPlacer blockPlacer;
    private Inventory playerInventory;
    private PlayerHealth playerHealth;

    private float eatingTimer = 0f;
    private float timeToEat = 1.5f;      
    public float pickupProtectionTimer = 0f; 
    private int lastItemCount = 0;
    private bool mustReleaseButton = false;

    void Start()
    {
        blockPlacer = GetComponentInParent<BlockPlacer>();
        playerInventory = GetComponentInParent<Inventory>();
        playerHealth = GetComponentInParent<PlayerHealth>();
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; 
    }

    void Update()
    {
        if (blockPlacer == null || playerInventory == null || playerHealth == null) return;

        int activeSlot = GetActiveSlotIndex();
        bool holdingMutton = false;

        if (activeSlot >= 0 && activeSlot < playerInventory.slots.Count)
        {
            var slot = playerInventory.slots[activeSlot];
            if (slot != null && slot.item != null && slot.item.itemID.ToLower() == "mutton")
            {
                holdingMutton = true;
                if (slot.count > lastItemCount)
                {
                    pickupProtectionTimer = 1.5f;
                    mustReleaseButton = true; 
                }
                lastItemCount = slot.count;
            }
            else
            {
                lastItemCount = 0;
            }
        }
        if (mustReleaseButton)
        {
            if (!Input.GetMouseButton(1))
            {
                mustReleaseButton = false;
            }
            ResetEatingState();
            return;
        }
        if (pickupProtectionTimer > 0f)
        {
            pickupProtectionTimer -= Time.deltaTime;
            ResetEatingState();
            return; 
        }

        if (!holdingMutton)
        {
            ResetEatingState();
            return;
        }
        if (Input.GetMouseButton(1) && playerHealth.currentHealth < playerHealth.maxHealth)
        {
            eatingTimer += Time.deltaTime;

            if (eatSound != null && !audioSource.isPlaying)
            {
                audioSource.clip = eatSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            if (eatingTimer >= timeToEat)
            {
                EatSuccess(activeSlot);
            }
        }
        else
        {
            ResetEatingState();
        }
    }

    void EatSuccess(int slotIndex)
    {
        ResetEatingState();

        playerHealth.currentHealth += 20f;
        playerHealth.currentHealth = Mathf.Clamp(playerHealth.currentHealth, 0, playerHealth.maxHealth);

        var slot = playerInventory.slots[slotIndex];
        slot.count--;
        if (slot.count <= 0) slot.Clear();

        playerInventory.UpdateAllUISlots();
        playerHealth.Invoke("UpdateHeartsUI", 0f);

        if (audioSource.isPlaying) audioSource.Stop();
        audioSource.loop = false;
        
        lastItemCount = 0; 
        Debug.Log("[HandHeldEating] Мясо успешно съедено игроком через зажатие ПКМ!");
    }

    void ResetEatingState()
    {
        eatingTimer = 0f;
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
    }

    int GetActiveSlotIndex()
    {
        if (blockPlacer == null) return 0;
        var fields = blockPlacer.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        foreach (var f in fields)
        {
            if (f.Name == "selectedSlot") return (int)f.GetValue(blockPlacer);
        }
        return 0;
    }
}
