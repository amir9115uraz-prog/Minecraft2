using UnityEngine;

public class MinecraftCraftingWindowUI : MonoBehaviour
{
    private Inventory playerInventory;          

    void Start()
    {
        playerInventory = Object.FindAnyObjectByType<Inventory>();
        CheckRecipeUpdate();
    }

    private void OnEnable()
    {
        CheckRecipeUpdate();
    }

    public void CheckRecipeUpdate()
    {
        UI_CraftingContainer container = Object.FindAnyObjectByType<UI_CraftingContainer>();
        if (container != null)
        {
            container.CheckRecipe();
        }
    }
}
