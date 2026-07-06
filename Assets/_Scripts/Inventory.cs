using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Inventory : MonoBehaviourPun
{
    [System.Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int count;

        public void Clear() { item = null; count = 0; }
    }

    [Header("Слоты данных предметов")]
    public List<InventorySlot> slots = new List<InventorySlot>(47);
    
    [HideInInspector] public InventorySlot mouseSlot = new InventorySlot();

    private List<Image> uiSlots = new List<Image>();
    private List<object> uiTexts = new List<object>(); 
    private Dictionary<string, ItemData> itemCache = new Dictionary<string, ItemData>();

    void Awake()
    {
        if (photonView != null && !photonView.IsMine) return;

        slots.Clear();
        for (int i = 0; i < 47; i++) slots.Add(new InventorySlot());

        ItemData[] allItems = Resources.LoadAll<ItemData>("");
        foreach (var item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemID))
            {
                if (!itemCache.ContainsKey(item.itemID.ToLower()))
                    itemCache.Add(item.itemID.ToLower(), item);
            }
        }
    }

    void Start()
    {
        if (photonView != null && !photonView.IsMine) return;

        GameObject hotbar = GameObject.Find("HotbarPanel");
        if (hotbar != null)
        {
            int index = 0;
            foreach (Transform cell in hotbar.transform)
            {
                Image targetImage = cell.GetComponent<Image>();
                if (targetImage != null) uiSlots.Add(targetImage);

                Text txt = cell.GetComponentInChildren<Text>(true);
                if (txt != null)
                {
                    uiTexts.Add(txt);
                }
                else
                {
                    var tmp = cell.GetComponentInChildren(System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro"));
                    if (tmp != null) uiTexts.Add(tmp);
                    else uiTexts.Add(null);
                }

                index++;
                if (index >= 8) break;
            }
        }
        
        UpdateAllUISlots();
    }

    public ItemData FindItemInCache(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        string lowerId = id.ToLower();
        if (itemCache.ContainsKey(lowerId)) return itemCache[lowerId];
        
        ItemData[] fallback = Resources.LoadAll<ItemData>("");
        foreach (var item in fallback)
        {
            if (item != null && (item.itemID.ToLower() == lowerId || item.name.ToLower() == lowerId))
                return item;
        }
        return null;
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null) return false;
        for (int i = 0; i < slots.Count; i++)
        {
            if (IsHotbarSlot(i))
            {
                if (slots[i].item != null && slots[i].item.itemID.ToLower() == item.itemID.ToLower() && slots[i].count < item.maxStack)
                {
                    slots[i].count += amount;
                    UpdateAllUISlots();
                    return true;
                }
            }
        }
        for (int i = 0; i < slots.Count; i++)
        {
            if (IsHotbarSlot(i))
            {
                if (slots[i].item == null)
                {
                    slots[i].item = item;
                    slots[i].count = amount;
                    UpdateAllUISlots();
                    return true;
                }
            }
        }
        for (int i = 0; i < slots.Count; i++)
        {
            if (!IsHotbarSlot(i))
            {
                if (slots[i].item != null && slots[i].item.itemID.ToLower() == item.itemID.ToLower() && slots[i].count < item.maxStack)
                {
                    slots[i].count += amount;
                    UpdateAllUISlots();
                    return true;
                }
            }
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (!IsHotbarSlot(i))
            {
                if (slots[i].item == null)
                {
                    slots[i].item = item;
                    slots[i].count = amount;
                    UpdateAllUISlots();
                    return true;
                }
            }
        }

        return false; 
    }
    private bool IsHotbarSlot(int index)
    {
        if (index >= 0 && index <= 7) return true;
        if (index >= 32 && index <= 39) return true;

        return false;
    }


    public void RemoveItem(ItemData item)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].item != null && slots[i].item.itemID == item.itemID)
            {
                slots[i].count--;
                if (slots[i].count <= 0) slots[i].Clear();
                UpdateAllUISlots();
                return;
            }
        }
    }

    public void UpdateAllUISlots()
    {
        UpdateUI();

        MinecraftSlotUI[] allUiSlots = Object.FindObjectsOfType<MinecraftSlotUI>(true);
        for (int i = 0; i < allUiSlots.Length; i++)
        {
            if (allUiSlots[i] != null) allUiSlots[i].Refresh();
        }
    }

    public void UpdateUI()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (uiSlots.Count <= i || uiSlots[i] == null) continue;

            MinecraftSlotUI slotUI = uiSlots[i].GetComponent<MinecraftSlotUI>();

            if (slots[i] != null && slots[i].item != null && slots[i].count > 0)
            {
                uiSlots[i].sprite = slots[i].item.icon;
                uiSlots[i].enabled = true;

                Color c = uiSlots[i].color;
                c.a = 1f; 
                uiSlots[i].color = c;

                SetTextValue(i, slots[i].count > 1 ? slots[i].count.ToString() : "");
            }
            else
            {
                if (slotUI != null)
                {
                    uiSlots[i].sprite = null; 
                    slotUI.Refresh(); 
                }
                else
                {
                    uiSlots[i].sprite = null;
                    uiSlots[i].enabled = true;
                }
                SetTextValue(i, "");
            }
        }
    }

    private void SetTextValue(int index, string value)
    {
        if (uiTexts.Count <= index || uiTexts[index] == null) return;

        if (uiTexts[index] is Text standardText)
        {
            standardText.text = value;
            standardText.enabled = !string.IsNullOrEmpty(value);
        }
        else 
        {
            var property = uiTexts[index].GetType().GetProperty("text");
            var enabledProp = uiTexts[index].GetType().GetProperty("enabled");
            if (property != null) property.SetValue(uiTexts[index], value);
            if (enabledProp != null) enabledProp.SetValue(uiTexts[index], !string.IsNullOrEmpty(value));
        }
    }
}
