using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [Header("Все блоки и предметы в игре")]
    public List<ItemData> allItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public ItemData GetItemByBlockType(BlockType type)
    {
        foreach (var item in allItems)
        {
            if (item.blockType == type) return item;
        }
        return null;
    }
}
