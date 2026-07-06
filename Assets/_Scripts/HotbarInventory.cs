using UnityEngine;
using UnityEngine.UI;

public class HotbarInventory : MonoBehaviour
{
    public static HotbarInventory Instance;
    
    [Header("Слоты (ровно 8 штук)")]
    public Image[] slotIcons;       
    public int selectedSlot = 0;    

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) selectedSlot = (selectedSlot - 1 + 8) % 8;
        if (scroll < 0f) selectedSlot = (selectedSlot + 1) % 8;
        for (int i = 0; i < 8; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedSlot = i;
            }
        }

        HighlightSelectedUI();
    }
    void HighlightSelectedUI()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            var color = slotIcons[i].color;
            color.a = (i == selectedSlot) ? 1f : 0.6f;
            slotIcons[i].color = color;
        }
    }
}
