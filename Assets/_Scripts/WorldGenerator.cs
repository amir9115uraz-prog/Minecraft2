using UnityEngine;
using System.IO;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator Instance;

    [Header("Настройки мира")]
    public int seed;
    public int chunkWidth = 16;
    public int chunkHeight = 256;

    [Header("Настройки шума генерации")]
    public float noiseScale = 0.005f;
    public float mountainScale = 0.02f;

    [Header("Настройки Шахт и Пещер")]
    public float caveScale3D = 0.045f; 
    [Range(0f, 1f)] public float caveThreshold = 0.58f; 
    public int maxCaveHeight = 55;   

    [Header("Настройки Входов в пещеры")]
    public float entranceScale = 0.015f; 
    [Range(0f, 1f)] public float entranceThreshold = 0.72f; 

    [Header("Массив настроек биомов из инспектора")]
    public BiomeSettings[] biomes;

    private float seedOffset;
    private string currentWorldName;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        currentWorldName = PlayerPrefs.GetString("CurrentWorldName", "DefaultWorld");
        LoadWorldSeed();

        seedOffset = (seed % 10000) * 0.1f;
    }

    public float GetSpawnHeightAt(int x, int z)
    {
        float xCoord = x + seedOffset;
        float zCoord = z + seedOffset;

        BiomeType biomeType = GetBiomeAt(x, z);
        BiomeSettings settings = GetSettingsForBiome(biomeType);

        float height = GetSurfaceHeight(xCoord, zCoord, biomeType, settings);
        return Mathf.FloorToInt(height) + 1.5f; 
    }

    public BlockType GetBlockType(int x, int y, int z)
    {
        if (y < 0 || y >= chunkHeight) return BlockType.Air;
        if (y == 0) return BlockType.Bedrock;

        float xCoord = x + seedOffset;
        float zCoord = z + seedOffset;
        float yCoord = y + seedOffset;

        BiomeType biomeType = GetBiomeAt(x, z);
        BiomeSettings settings = GetSettingsForBiome(biomeType);

        float height = GetSurfaceHeight(xCoord, zCoord, biomeType, settings);
        int finalHeight = Mathf.FloorToInt(height);

        if (y > finalHeight) return BlockType.Air;

        BlockType selectedBlock = BlockType.Stone;

        if (y == finalHeight)
        {
            selectedBlock = (settings != null) ? settings.topBlock : BlockType.Grass;
        }
        else if (y > finalHeight - 4)
        {
            selectedBlock = (settings != null) ? settings.surfaceBlock : BlockType.Dirt;
        }
        else
        {
            float oreX = xCoord * 0.25f;
            float oreY = yCoord * 0.25f;
            float oreX2 = xCoord * 0.15f;
            float oreZ = zCoord * 0.25f;

            float u1 = Mathf.PerlinNoise(oreX, oreY);
            float u2 = Mathf.PerlinNoise(oreY, oreZ);
            float u3 = Mathf.PerlinNoise(oreX2, oreZ);
            float ore3D = u1 * u2 * u3 * 2f;

            if (y < 13 && ore3D > 0.81f) selectedBlock = BlockType.DiamondOre;
            else if (y < 22 && ore3D > 0.79f && ore3D <= 0.81f) selectedBlock = BlockType.EmeraldOre;
            else if (y < 30 && ore3D > 0.74f && ore3D <= 0.79f) selectedBlock = BlockType.GoldOre;
            else if (y < 50 && ore3D > 0.64f && ore3D <= 0.70f) selectedBlock = BlockType.IronOre;
            else if (y < 60 && ore3D > 0.54f && ore3D <= 0.60f) selectedBlock = BlockType.CoalOre;
        }

        if (selectedBlock != BlockType.Bedrock)
        {
            float entranceNoise = Mathf.PerlinNoise(xCoord * entranceScale, zCoord * entranceScale);
            bool isEntranceZone = entranceNoise > entranceThreshold;

            int currentMaxHeight = isEntranceZone ? finalHeight : maxCaveHeight;

            if (!isEntranceZone && y >= (finalHeight - 5)) return selectedBlock;

            if (y <= currentMaxHeight)
            {
                float xy = Mathf.PerlinNoise(xCoord * caveScale3D, yCoord * caveScale3D);
                float yz = Mathf.PerlinNoise(yCoord * caveScale3D, zCoord * caveScale3D);
                float xz = Mathf.PerlinNoise(xCoord * caveScale3D, zCoord * caveScale3D);

                float yx = Mathf.PerlinNoise(yCoord * caveScale3D, xCoord * caveScale3D);
                float zy = Mathf.PerlinNoise(yCoord * caveScale3D, zCoord * caveScale3D);
                float zx = Mathf.PerlinNoise(zCoord * caveScale3D, xCoord * caveScale3D);

                float side1 = xy * yz * xz;
                float side2 = yx * zy * zx;
                float final3DNoise = (side1 + side2) * 2f;

                if (final3DNoise > caveThreshold) return BlockType.Air;
            }
        }

        return selectedBlock;
    }

    public BiomeType GetBiomeAt(int x, int z)
    {
        float xCoord = x + seedOffset;
        float zCoord = z + seedOffset;

        float temperature = Mathf.PerlinNoise(xCoord * 0.001f, zCoord * 0.001f);
        float moisture = Mathf.PerlinNoise(xCoord * 0.0015f, zCoord * 0.0015f);

        if (temperature < 0.35f)
        {
            return BiomeType.Taiga;
        }
        else if (temperature > 0.65f)
        {
            if (moisture < 0.4f) return BiomeType.Desert;
            return BiomeType.Jungle;
        }
        else
        {
            if (moisture > 0.6f) return BiomeType.BirchForest;
            return BiomeType.Plains;
        }
    }

    private float GetSurfaceHeight(float xCoord, float zCoord, BiomeType biome, BiomeSettings settings)
    {
        float baseNoise = Mathf.PerlinNoise(xCoord * noiseScale, zCoord * noiseScale);
        
        float hPlains = 60f + baseNoise * 15f;
        float hDesert = 62f + baseNoise * 6f;
        float hJungle = 63f + baseNoise * 25f;
        float hTaiga = 58f + baseNoise * 12f;
        float hBirch = 61f + baseNoise * 14f;

        float mountainNoise = Mathf.PerlinNoise(xCoord * mountainScale, zCoord * mountainScale);
        float hPlainsMountains = Mathf.Lerp(hPlains, 75f + mountainNoise * 60f, baseNoise);

        float temperature = Mathf.PerlinNoise(xCoord * 0.001f, zCoord * 0.001f);
        float moisture = Mathf.PerlinNoise(xCoord * 0.0015f, zCoord * 0.0015f);

        if (temperature < 0.35f) return hTaiga;
        if (temperature > 0.65f) return (moisture < 0.4f) ? hDesert : hJungle;
        return (moisture > 0.6f) ? hBirch : hPlainsMountains;
    }

    public BiomeSettings GetSettingsForBiome(BiomeType type)
    {
        if (biomes == null) return null;
        for (int i = 0; i < biomes.Length; i++)
        {
            if (biomes[i] != null && biomes[i].biomeType == type) return biomes[i];
        }
        return null;
    }

    public static void GetTreeBlocksForPosition(int globalX, int globalZ, out BlockType log, out BlockType leaves)
    {
        log = BlockType.OakLog;
        leaves = BlockType.OakLeaves;

        if (Instance != null)
        {
            BiomeType type = Instance.GetBiomeAt(globalX, globalZ);
            BiomeSettings settings = Instance.GetSettingsForBiome(type);
            if (settings != null)
            {
                log = settings.logBlock;
                leaves = settings.leavesBlock;
            }
        }
    }

    private void LoadWorldSeed()
    {
        string path = Path.Combine(Application.persistentDataPath, "Worlds", currentWorldName + "_seed.txt");
        if (File.Exists(path))
        {
            int.TryParse(File.ReadAllText(path), out seed);
        }
        else
        {
            seed = Random.Range(100000, 999999);
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, seed.ToString());
        }
    }
}
