using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class InventoryController : MonoBehaviourPun
{
    [System.Serializable]
    public class SlotData
    {
        public ItemData item;
        public int count;
        public void Clear() { item = null; count = 0; }
    }

    public List<SlotData> slots = new List<SlotData>(47);
    public SlotData mouseSlot = new SlotData();

    void Awake()
    {
        if (photonView != null && !photonView.IsMine) return;

        slots.Clear();
        for (int i = 0; i < 47; i++)
        {
            slots.Add(new SlotData());
        }
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null) return false;

        for (int i = 0; i < 32; i++)
        {
            if (slots[i].item != null && slots[i].item.itemID == item.itemID && slots[i].count < item.maxStack)
            {
                slots[i].count += amount;
                UpdateAllUISlots();
                return true;
            }
        }

        for (int i = 0; i < 32; i++)
        {
            if (slots[i].item == null)
            {
                slots[i].item = item;
                slots[i].count = amount;
                UpdateAllUISlots();
                return true;
            }
        }
        return false; 
    }

    public void RemoveItem(ItemData item)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].item != null && slots[i].item.itemID == item.itemID)
            {
                slots[i].count--;
                if (slots[i].count <= 0)
                {
                    slots[i].Clear();
                }
                UpdateAllUISlots();
                return;
            }
        }
    }

    public void RemoveItemFromSlot(int slotIndex, int amount = 1)
    {
        if (slotIndex >= 0 && slotIndex < slots.Count && slots[slotIndex].item != null)
        {
            slots[slotIndex].count -= amount;
            if (slots[slotIndex].count <= 0) slots[slotIndex].Clear();
            UpdateAllUISlots();
        }
    }

    public void UpdateAllUISlots()
    {
        MinecraftSlotUI[] uiSlots = FindObjectsOfType<MinecraftSlotUI>(true);
        foreach (var uiSlot in uiSlots)
        {
            if (uiSlot != null)
            {
                uiSlot.Refresh();
            }
        }
    }
}
