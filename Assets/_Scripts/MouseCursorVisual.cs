using UnityEngine;
using UnityEngine.UI;

public class MinecraftMouseCursor : MonoBehaviour
{
    public Image iconImage;
    public Text countText;
    
    private InventoryController playerInventory;

    void Start()
    {
        playerInventory = FindObjectOfType<InventoryController>();
        Refresh();
    }

    void Update()
    {
       
        transform.position = Input.mousePosition;
    }

    public void Refresh()
    {
        if (playerInventory == null) return;

        InventoryController.SlotData mouse = playerInventory.mouseSlot;

        if (mouse.item != null && mouse.count > 0)
        {
            iconImage.sprite = mouse.item.icon;
            iconImage.enabled = true;
            countText.text = mouse.count > 1 ? mouse.count.ToString() : "";
            countText.enabled = mouse.count > 1;
        }
        else
        {
            iconImage.enabled = false;
            countText.enabled = false;
        }
    }
}
