using UnityEngine;
using TMPro;

public class UI_ItemSplitter : MonoBehaviour
{
    public static UI_ItemSplitter Instance;

    [Header("UI элементы окошка")]
    public GameObject splitterPanel;
    public TMP_InputField inputField;

    private MinecraftSlotUI sourceSlot;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (splitterPanel != null) splitterPanel.SetActive(false);
    }

    public void OpenSplitter(MinecraftSlotUI slot, int currentCount)
    {
        sourceSlot = slot;
        if (splitterPanel != null) splitterPanel.SetActive(true);
        if (inputField != null)
        {
            inputField.text = "1";
            inputField.ActivateInputField();
        }
    }

    public void ConfirmSplit()
    {
        if (sourceSlot == null || inputField == null) return;

        int amountToSplit = 0;
        if (int.TryParse(inputField.text, out amountToSplit) && amountToSplit > 0)
        {
            Inventory playerInv = Object.FindAnyObjectByType<Inventory>();
            if (playerInv != null && playerInv.slots != null)
            {
                int srcIdx = sourceSlot.GetRealIndex();
                if (srcIdx >= 0 && srcIdx < playerInv.slots.Count)
                {
                    var cell = playerInv.slots[srcIdx];
                    var mouse = playerInv.mouseSlot;

                    if (cell.item != null && amountToSplit <= cell.count)
                    {
                        if (mouse.item == null)
                        {
                            mouse.item = cell.item;
                            mouse.count = amountToSplit;
                            cell.count -= amountToSplit;
                        }
                        else if (mouse.item == cell.item && mouse.count + amountToSplit <= mouse.item.maxStack)
                        {
                            mouse.count += amountToSplit;
                            cell.count -= amountToSplit;
                        }

                        if (cell.count <= 0) cell.Clear();
                    }
                }
                playerInv.UpdateAllUISlots();
            }
        }

        CloseSplitter();
    }

    public void CloseSplitter()
    {
        sourceSlot = null;
        if (splitterPanel != null) splitterPanel.SetActive(false);
    }
}
