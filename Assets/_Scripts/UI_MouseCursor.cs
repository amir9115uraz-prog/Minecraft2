using UnityEngine;
using UnityEngine.UI;

public class UI_MouseCursor : MonoBehaviour
{
    public Image iconImage;
    public Text countText;
    private InventoryController playerInv;

    void Start()
    {
        playerInv = FindObjectOfType<InventoryController>();
        Refresh();
    }

    void Update()
    {
        transform.position = Input.mousePosition;
    }

    public void Refresh()
    {
        if (playerInv == null) return;

        if (playerInv.mouseSlot.item != null)
        {
            iconImage.sprite = playerInv.mouseSlot.item.icon;
            iconImage.enabled = true;
            countText.text = playerInv.mouseSlot.count > 1 ? playerInv.mouseSlot.count.ToString() : "";
            countText.enabled = playerInv.mouseSlot.count > 1;
        }
        else
        {
            iconImage.enabled = false;
            countText.enabled = false;
        }
    }
}
