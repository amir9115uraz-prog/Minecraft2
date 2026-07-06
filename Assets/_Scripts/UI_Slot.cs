using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UI_Slot : MonoBehaviour, IPointerClickHandler
{
    public enum SlotType { Inventory, Crafting, Result }
    
    [Header("Настройки слота")]
    public SlotType type;
    public int slotIndex; 

    [Header("UI элементы ячейки")]
    public Image iconImage;
    public TextMeshProUGUI countText; 

    private InventoryController playerInv;
    private UI_CraftingContainer craftingContainer;

    void Start()
    {
        playerInv = FindObjectOfType<InventoryController>();
        craftingContainer = FindObjectOfType<UI_CraftingContainer>();
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (playerInv == null) return;

        InventoryController.SlotData targetSlot = GetTargetSlot();

        if (targetSlot != null && targetSlot.item != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = targetSlot.item.icon;
                iconImage.enabled = true;
            }
            
            if (countText != null)
            {
                if (targetSlot.count > 1)
                {
                    countText.text = targetSlot.count.ToString();
                    countText.enabled = true;
                }
                else
                {
                    countText.enabled = false;
                }
            }
        }
        else
        {
            if (iconImage != null) iconImage.enabled = false;
            if (countText != null) countText.enabled = false;
        }
    }

    private InventoryController.SlotData GetTargetSlot()
    {
        if (playerInv == null || playerInv.slots == null) return null;

        if (type == SlotType.Inventory)
        {
            if (slotIndex >= 0 && slotIndex < playerInv.slots.Count) return playerInv.slots[slotIndex];
        }
        else if (type == SlotType.Crafting)
        {
            int targetIndex = 32 + slotIndex;
            if (targetIndex >= 0 && targetIndex < playerInv.slots.Count) return playerInv.slots[targetIndex];
        }
        else if (type == SlotType.Result)
        {
            if (playerInv.slots.Count >= 42) return playerInv.slots[41];
        }
        return null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (playerInv == null) return;

        InventoryController.SlotData cell = GetTargetSlot();
        if (cell == null) return;
        
        InventoryController.SlotData mouse = playerInv.mouseSlot;

        if (type == SlotType.Result && cell.item != null)
        {
            if (mouse.item == null || (mouse.item == cell.item && mouse.count + cell.count <= mouse.item.maxStack))
            {
                if (mouse.item == null)
                {
                    mouse.item = cell.item;
                    mouse.count = cell.count;
                }
                else
                {
                    mouse.count += cell.count;
                }
                cell.Clear();
                if (craftingContainer != null)
                {
                    craftingContainer.OnCraftTaken();
                }
            }
            UpdateAllUI();
            return;
        }

        ItemData tempItem = cell.item;
        int tempCount = cell.count;

        cell.item = mouse.item;
        cell.count = mouse.count;

        mouse.item = tempItem;
        mouse.count = tempCount;

        if (type == SlotType.Crafting && craftingContainer != null)
        {
            craftingContainer.CheckRecipe();
        }

        UpdateAllUI();
    }

    void UpdateAllUI()
    {
        foreach (var slot in FindObjectsOfType<UI_Slot>())
        {
            slot.RefreshUI();
        }
        if (playerInv != null)
        {
            playerInv.UpdateAllUISlots();
        }
        FindObjectOfType<UI_MouseCursor>()?.Refresh();
    }
}
