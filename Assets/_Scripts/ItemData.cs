using UnityEngine;

public enum ItemType { Material, Tool, Weapon, Block }
public enum ToolTier { Wood, Stone, Iron, Gold, Diamond, None }

[CreateAssetMenu(fileName = "New Item", menuName = "Minecraft/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;       
    public Sprite icon;           
    public ItemType itemType;     
    public ToolTier tier;         
    public BlockType blockType;   

    [Header("Мультиплеер")]
    public GameObject weaponPrefab; 

    [Header("Характеристики")]
    public float damage;          
    public float mineSpeed;       
    public int maxStack = 64;     
    [Header("Настройки для Нового Инвентаря")]
    public string itemID;        
    public bool isBlock;         
}

