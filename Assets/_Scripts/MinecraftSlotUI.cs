using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MinecraftSlotUI : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum SlotType { Inventory, Crafting, Result, InventoryCrafting, InventoryResult }

    [Header("Настройки слота")]
    public SlotType slotType = SlotType.Inventory;
    public int slotIndex; 

    [Header("Ссылки на компоненты")]
    public Image itemIconImage; 
    public TextMeshProUGUI itemCountText;  

    private Inventory playerInventory;
    private static GameObject dragVisualIcon;
    private Sprite defaultSlotSprite;

    void Start()
    {
        playerInventory = Object.FindAnyObjectByType<Inventory>();
        if (itemIconImage == null) itemIconImage = GetComponent<Image>();
        if (itemIconImage != null) defaultSlotSprite = itemIconImage.sprite;
        Refresh();
    }

    public int GetRealIndex()
    {
        if (slotType == SlotType.Inventory) return slotIndex;
        if (slotType == SlotType.Crafting) return 32 + slotIndex;
        if (slotType == SlotType.Result) return 41;
        if (slotType == SlotType.InventoryCrafting) return 42 + slotIndex;
        if (slotType == SlotType.InventoryResult) return 46;
        return slotIndex;
    }

    public void Refresh()
    {
        if (playerInventory == null || playerInventory.slots == null) return;
        
        int realIdx = GetRealIndex();
        if (realIdx < 0 || realIdx >= playerInventory.slots.Count) return;

        Inventory.InventorySlot mySlot = playerInventory.slots[realIdx];
        if (itemIconImage == null) itemIconImage = GetComponent<Image>();

        if (itemIconImage != null)
        {
            itemIconImage.enabled = true;
            Color c = itemIconImage.color;
            c.a = 1f;
            itemIconImage.color = c;

            if (mySlot != null && mySlot.item != null && mySlot.count > 0)
                itemIconImage.sprite = mySlot.item.icon;
            else
                itemIconImage.sprite = defaultSlotSprite;
        }

        if (itemCountText != null)
        {
            if (mySlot != null && mySlot.item != null && mySlot.count > 1)
            {
                itemCountText.text = mySlot.count.ToString();
                itemCountText.enabled = true;
            }
            else
            {
                itemCountText.text = "";
                itemCountText.enabled = false;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (playerInventory == null || playerInventory.slots == null) return;

        int realIdx = GetRealIndex();
        if (realIdx < 0 || realIdx >= playerInventory.slots.Count) return;

        Inventory.InventorySlot cell = playerInventory.slots[realIdx];

        if (eventData.button == PointerEventData.InputButton.Right && slotType != SlotType.Result && slotType != SlotType.InventoryResult)
        {
            if (cell.item != null && cell.count > 0)
            {
                Inventory.InventorySlot mouse = playerInventory.mouseSlot;
                if (mouse.item == null && cell.count > 1)
                {
                    int takeCount = cell.count / 2;
                    mouse.item = cell.item;
                    mouse.count = takeCount;
                    cell.count -= takeCount;
                }
                UpdateAllUI();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right) return;
        HandleSlotInteraction();
    }

    public void DropItemIntoSlot()
    {
        HandleSlotInteraction();
    }

    private void HandleSlotInteraction()
    {
        if (playerInventory == null || playerInventory.slots == null) return;

        int realIdx = GetRealIndex();
        if (realIdx < 0 || realIdx >= playerInventory.slots.Count) return;

        Inventory.InventorySlot cell = playerInventory.slots[realIdx]; 
        Inventory.InventorySlot mouse = playerInventory.mouseSlot;       

        if (slotType == SlotType.Result || slotType == SlotType.InventoryResult)
        {
            if (cell.item != null)
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

                   UI_CraftingContainer craftingContainer = Object.FindObjectOfType<UI_CraftingContainer>(true);
                    if (craftingContainer != null) craftingContainer.OnCraftTaken();
                }
            }
            UpdateAllUI();
            return;
        }

        if (mouse.item != null && cell.item == mouse.item && cell.count < cell.item.maxStack)
        {
            int availableSpace = cell.item.maxStack - cell.count;
            int amountToAdd = Mathf.Min(availableSpace, mouse.count);
            cell.count += amountToAdd;
            mouse.count -= amountToAdd;
            if (mouse.count <= 0) mouse.Clear();
        }
        else
        {
            ItemData tempItem = cell.item;
            int tempCount = cell.count;

            cell.item = mouse.item;
            cell.count = mouse.count;

            mouse.item = tempItem;
            mouse.count = tempCount;
        }

        UI_CraftingContainer finalCraftContainer = Object.FindObjectOfType<UI_CraftingContainer>();
        if (finalCraftContainer != null) 
        {
            finalCraftContainer.CheckRecipe();
        }

        UpdateAllUI();
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        if (playerInventory == null || playerInventory.mouseSlot == null || playerInventory.mouseSlot.item == null) return;

        if (dragVisualIcon == null)
        {
            dragVisualIcon = new GameObject("DragVisualIcon");
            dragVisualIcon.transform.SetParent(GetComponentInParent<Canvas>().transform, false);
            Image img = dragVisualIcon.AddComponent<Image>();
            img.raycastTarget = false;
        }

        dragVisualIcon.GetComponent<Image>().sprite = playerInventory.mouseSlot.item.icon;
        dragVisualIcon.GetComponent<Image>().enabled = true;
        dragVisualIcon.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisualIcon != null && dragVisualIcon.GetComponent<Image>().enabled)
        {
            dragVisualIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragVisualIcon != null) dragVisualIcon.GetComponent<Image>().enabled = false;

        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            if (raycastResults[i].gameObject == null) continue;

            MinecraftSlotUI targetSlot = raycastResults[i].gameObject.GetComponentInParent<MinecraftSlotUI>();
            if (targetSlot != null && targetSlot != this)
            {
                targetSlot.DropItemIntoSlot();
                break;
            }
        }
        UpdateAllUI();
    }

    void UpdateAllUI()
    {
        if (playerInventory != null) playerInventory.UpdateAllUISlots();

        MinecraftSlotUI[] allSlots = Object.FindObjectsOfType<MinecraftSlotUI>(true);
        for (int i = 0; i < allSlots.Length; i++)
        {
            if (allSlots[i] != null) allSlots[i].Refresh();
        }
    }

    public ItemData GetItemData() { int realIdx = GetRealIndex(); return (playerInventory != null && realIdx < playerInventory.slots.Count) ? playerInventory.slots[realIdx].item : null; }
    public int GetItemCount() { int realIdx = GetRealIndex(); return (playerInventory != null && realIdx < playerInventory.slots.Count) ? playerInventory.slots[realIdx].count : 0; }
    public void SetSlotData(ItemData item, int count) { int realIdx = GetRealIndex(); if (playerInventory != null && realIdx < playerInventory.slots.Count) { playerInventory.slots[realIdx].item = item; playerInventory.slots[realIdx].count = count; Refresh(); } }
    public void ClearSlot() { int realIdx = GetRealIndex(); if (playerInventory != null && realIdx < playerInventory.slots.Count) { playerInventory.slots[realIdx].Clear(); Refresh(); } }
    public void ReduceCount(int amount) { int realIdx = GetRealIndex(); if (playerInventory != null && realIdx < playerInventory.slots.Count) { playerInventory.slots[realIdx].count -= amount; if (playerInventory.slots[realIdx].count <= 0) playerInventory.slots[realIdx].Clear(); Refresh(); } }
}
