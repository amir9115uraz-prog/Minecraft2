using System.Collections.Generic;
using UnityEngine;

public class UI_CraftingContainer : MonoBehaviour
{
    private Inventory playerInventory;

    private void Start()
    {
        playerInventory = Object.FindAnyObjectByType<Inventory>();
        CheckRecipe();
    }

    public void CheckRecipe()
    {
        if (playerInventory == null || CraftingManager.Instance == null) return;

        MinecraftSlotUI[] allSlots = Object.FindObjectsOfType<MinecraftSlotUI>(true);
        bool hasTableOpen = false;
        
        foreach (var slot in allSlots)
        {
            if (slot != null && slot.gameObject.activeInHierarchy && slot.slotType == MinecraftSlotUI.SlotType.Crafting)
            {
                hasTableOpen = true;
                break;
            }
        }

        if (hasTableOpen)
        {
            string[,] matrix3x3 = new string[3, 3];
            int idx = 32;
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    if (idx < playerInventory.slots.Count && playerInventory.slots[idx] != null && playerInventory.slots[idx].item != null)
                        matrix3x3[x, y] = playerInventory.slots[idx].item.itemID.ToLower();
                    else
                        matrix3x3[x, y] = "";
                    idx++;
                }
            }

            int count;
            ItemData result = CraftingManager.Instance.CheckRecipe(matrix3x3, out count);
            UpdateResultSlot(41, result, count);
        }
        else
        {
            string[,] matrix2x2 = new string[2, 2];
            int idx = 42;
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    if (idx < playerInventory.slots.Count && playerInventory.slots[idx] != null && playerInventory.slots[idx].item != null)
                        matrix2x2[x, y] = playerInventory.slots[idx].item.itemID.ToLower();
                    else
                        matrix2x2[x, y] = "";
                    idx++;
                }
            }

            int count;
            ItemData result = CraftingManager.Instance.CheckRecipe(matrix2x2, out count);
            UpdateResultSlot(46, result, count);
        }
    }

    private void UpdateResultSlot(int realIndex, ItemData resultItem, int count)
    {
        if (playerInventory == null || realIndex >= playerInventory.slots.Count) return;
        
        var resSlot = playerInventory.slots[realIndex];
        if (resultItem != null)
        {
            resSlot.item = resultItem;
            resSlot.count = count;
        }
        else
        {
            resSlot.Clear();
        }
        playerInventory.UpdateAllUISlots();
    }

    public void OnCraftTaken()
    {
        if (playerInventory == null) return;

        MinecraftSlotUI[] allSlots = Object.FindObjectsOfType<MinecraftSlotUI>(true);
        bool hasTableOpen = false;
        foreach (var slot in allSlots)
        {
            if (slot != null && slot.gameObject.activeInHierarchy && slot.slotType == MinecraftSlotUI.SlotType.Crafting)
            {
                hasTableOpen = true;
                break;
            }
        }

        int startIdx = hasTableOpen ? 32 : 42;
        int endIdx = hasTableOpen ? 41 : 46;

        for (int i = startIdx; i < endIdx; i++)
        {
            if (i < playerInventory.slots.Count && playerInventory.slots[i] != null && playerInventory.slots[i].item != null)
            {
                playerInventory.slots[i].count--;
                if (playerInventory.slots[i].count <= 0) playerInventory.slots[i].Clear();
            }
        }
        CheckRecipe();
    }
}
