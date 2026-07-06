using UnityEngine;

public enum BiomeType { Plains, Desert, Jungle, Taiga, BirchForest }

[System.Serializable]
public class BiomeSettings
{
    public BiomeType biomeType;
    public string biomeName;
    
    [Header("Блоки ландшафта")]
    public BlockType topBlock;    
    public BlockType surfaceBlock;
    
    [Header("Настройки высоты")]
    public float terrainHeightScale = 20f; 
    public float baseTerrainHeight = 60f;  

    [Header("Настройки деревьев биома")]
    public BlockType logBlock;   
    public BlockType leavesBlock; 
    [Range(0f, 1f)] public float treeSpawnChance = 0.02f; 
}
