using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public Vector2Int chunkPosition; 
    private BlockType[,,] map;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>(); 

    private List<int> airTriangles = new List<int>();
    private List<int> dirtTriangles = new List<int>();
    private List<int> grassTriangles = new List<int>();
    private List<int> stoneTriangles = new List<int>();
    private List<int> oakLogTriangles = new List<int>();
    private List<int> oakLeavesTriangles = new List<int>();
    private List<int> birchLogTriangles = new List<int>();
    private List<int> birchLeavesTriangles = new List<int>();
    private List<int> bedrockTriangles = new List<int>();
    private List<int> coalTriangles = new List<int>();
    private List<int> ironTriangles = new List<int>();
    private List<int> goldTriangles = new List<int>();
    private List<int> diamondTriangles = new List<int>();
    private List<int> emeraldTriangles = new List<int>();
    private List<int> snowGrassTriangles = new List<int>();
    private List<int> snowBlockTriangles = new List<int>();
    private List<int> iceTriangles = new List<int>();
    private List<int> spruceLogTriangles = new List<int>();
    private List<int> spruceLeavesTriangles = new List<int>();
    private List<int> sandTriangles = new List<int>();
    private List<int> cactusTriangles = new List<int>();
    private List<int> jungleLogTriangles = new List<int>();
    private List<int> jungleLeavesTriangles = new List<int>();

    private int vertexIndex = 0, chunkWidthSize;
    private Transform playerTransform;
    
    private static readonly Vector3[] faceChecks = { Vector3.back, Vector3.forward, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
    private static readonly Vector3[][] faceVertices = {
        new[] { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,0,0) }, 
        new[] { new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1), new Vector3(0,0,1) }, 
        new[] { new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,1,0), new Vector3(0,0,0) }, 
        new[] { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(1,0,1) }, 
        new[] { new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,1,0) }, 
        new[] { new Vector3(1,0,0), new Vector3(1,0,1), new Vector3(0,0,1), new Vector3(0,0,0) }  
    };

    public void Initialize(Vector2Int position)
    {
        chunkPosition = position;
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        int width = WorldGenerator.Instance != null ? WorldGenerator.Instance.chunkWidth : 16;
        int height = WorldGenerator.Instance != null ? WorldGenerator.Instance.chunkHeight : 256;
        chunkWidthSize = width;
        map = new BlockType[width, height, width];
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
        GenerateChunkData(width, height);
        BuildMesh(width, height);
    }
    public BlockType GetBlockFromMap(int x, int y, int z)
    {
        if (map == null) return BlockType.Air;
        
        int width = map.GetLength(0);
        int height = map.GetLength(1);
        int depth = map.GetLength(2);

        if (x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < depth)
        {
            return map[x, y, z];
        }
        return BlockType.Air;
    }


       void Update()
    {
        if (playerTransform != null && chunkWidthSize > 0)
        {
            float chunkCenterX = chunkPosition.x * chunkWidthSize + (chunkWidthSize / 2f);
            float chunkCenterZ = chunkPosition.y * chunkWidthSize + (chunkWidthSize / 2f);
            
            float distanceToPlayer = Mathf.Max(
                Mathf.Abs(playerTransform.position.x - chunkCenterX), 
                Mathf.Abs(playerTransform.position.z - chunkCenterZ)
            );
            if (distanceToPlayer > chunkWidthSize * 5)
            {
                if (meshFilter != null && meshFilter.gameObject.activeSelf)
                {
                    GetComponent<MeshRenderer>().enabled = false;
                    if (meshCollider != null) meshCollider.enabled = false;
                }
            }
            else
            {
                GetComponent<MeshRenderer>().enabled = true;
                if (meshCollider != null) meshCollider.enabled = true;
            }

            if (distanceToPlayer > chunkWidthSize * 8)
            {
                Destroy(gameObject);
            }
        }
    }


       void GenerateChunkData(int width, int height)
    {
        int globalXOffset = chunkPosition.x * width, globalZOffset = chunkPosition.y * width;
        int currentSeed = WorldGenerator.Instance != null ? WorldGenerator.Instance.seed : 12345;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < width; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    BlockType originalType = WorldGenerator.Instance != null ? WorldGenerator.Instance.GetBlockType(globalXOffset + x, y, globalZOffset + z) : (y < 60 ? BlockType.Stone : BlockType.Air);
                    if (WorldSaveManager.Instance != null)
                    {
                        originalType = WorldSaveManager.Instance.GetSavedBlockOrOriginal(globalXOffset + x, y, globalZOffset + z, originalType);
                    }

                    map[x, y, z] = originalType;
                }
            }
        }
        Random.InitState(chunkPosition.x * 3123 + chunkPosition.y * 7142 + currentSeed);
        if (WorldGenerator.Instance == null) return;

        for (int x = 2; x < width - 2; x++) 
        {
            for (int z = 2; z < width - 2; z++)
            {
                for (int y = height - 2; y > 0; y--)
                {
                    if (map[x, y, z] == BlockType.Grass || map[x, y, z] == BlockType.Dirt || map[x, y, z] == BlockType.Sand || map[x, y, z] == BlockType.SnowGrass)
                    {
                        if (map[x, y + 1, z] == BlockType.Air)
                        {
                            BiomeType currentBiomeType = WorldGenerator.Instance.GetBiomeAt(globalXOffset + x, globalZOffset + z);
                            BiomeSettings currentBiomeSettings = WorldGenerator.Instance.GetSettingsForBiome(currentBiomeType);

                            if (currentBiomeSettings != null && currentBiomeSettings.treeSpawnChance > 0f)
                            {
                                if (Random.value < currentBiomeSettings.treeSpawnChance)
                                {
                                    StructureGenerator.CreateTree(map, x, y + 1, z, currentBiomeSettings.logBlock, currentBiomeSettings.leavesBlock, width, height);
                                }
                            }
                        }
                        y = 1; 
                    }
                }
            }
        }
    }


        bool CheckBlock(Vector3 position, int width, int height)
    {
        int x = Mathf.FloorToInt(position.x), y = Mathf.FloorToInt(position.y), z = Mathf.FloorToInt(position.z);
        if (y < 0 || y >= height) return false;
        
        if (x < 0 || x >= width || z < 0 || z >= width) 
        {
            return WorldGenerator.Instance == null || WorldGenerator.Instance.GetBlockType(chunkPosition.x * width + x, y, chunkPosition.y * width + z) == BlockType.Air;
        }

        BlockType currentBlock = map[x, y, z];
        if (currentBlock == BlockType.Air || 
            currentBlock == BlockType.OakLeaves || 
            currentBlock == BlockType.BirchLeaves || 
            currentBlock == BlockType.SpruceLeaves || 
            currentBlock == BlockType.JungleLeaves)
        {
            return true;
        }

        return false;
    }


    void BuildMesh(int width, int height)
    {
        vertices.Clear(); uvs.Clear(); vertexIndex = 0;
        dirtTriangles.Clear(); grassTriangles.Clear(); stoneTriangles.Clear(); oakLogTriangles.Clear(); oakLeavesTriangles.Clear();
        birchLogTriangles.Clear(); birchLeavesTriangles.Clear(); bedrockTriangles.Clear(); coalTriangles.Clear(); ironTriangles.Clear();
        goldTriangles.Clear(); diamondTriangles.Clear(); emeraldTriangles.Clear(); snowGrassTriangles.Clear(); snowBlockTriangles.Clear();
        iceTriangles.Clear(); spruceLogTriangles.Clear(); spruceLeavesTriangles.Clear(); sandTriangles.Clear(); cactusTriangles.Clear();
        jungleLogTriangles.Clear(); jungleLeavesTriangles.Clear();

        for (int x = 0; x < width; x++)
            for (int z = 0; z < width; z++)
                for (int y = 0; y < height; y++)
                    if (map[x, y, z] != BlockType.Air) AddBlockData(x, y, z, width, height);
        
        Mesh mesh = new Mesh { vertices = vertices.ToArray(), uv = uvs.ToArray(), subMeshCount = 22 };
        mesh.SetTriangles(dirtTriangles.ToArray(), 0);
        mesh.SetTriangles(grassTriangles.ToArray(), 1);
        mesh.SetTriangles(stoneTriangles.ToArray(), 2);
        mesh.SetTriangles(oakLogTriangles.ToArray(), 3);
        mesh.SetTriangles(oakLeavesTriangles.ToArray(), 4);
        mesh.SetTriangles(birchLogTriangles.ToArray(), 5);
        mesh.SetTriangles(birchLeavesTriangles.ToArray(), 6);
        mesh.SetTriangles(bedrockTriangles.ToArray(), 7);
        mesh.SetTriangles(coalTriangles.ToArray(), 8);
        mesh.SetTriangles(ironTriangles.ToArray(), 9);
        mesh.SetTriangles(goldTriangles.ToArray(), 10);
        mesh.SetTriangles(diamondTriangles.ToArray(), 11);
        mesh.SetTriangles(emeraldTriangles.ToArray(), 12);
        mesh.SetTriangles(snowGrassTriangles.ToArray(), 13);
        mesh.SetTriangles(snowBlockTriangles.ToArray(), 14);
        mesh.SetTriangles(iceTriangles.ToArray(), 15);
        mesh.SetTriangles(spruceLogTriangles.ToArray(), 16);
        mesh.SetTriangles(spruceLeavesTriangles.ToArray(), 17);
        mesh.SetTriangles(sandTriangles.ToArray(), 18);
        mesh.SetTriangles(cactusTriangles.ToArray(), 19);
        mesh.SetTriangles(jungleLogTriangles.ToArray(), 20);
        mesh.SetTriangles(jungleLeavesTriangles.ToArray(), 21);

        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        if (meshCollider != null) { meshCollider.enabled = true; meshCollider.sharedMesh = mesh; }
        Object.FindAnyObjectByType<ChunkMobSpawner>()?.OnChunkGenerated(this);
    }

        void AddBlockData(int x, int y, int z, int width, int height)
    {
        Vector3 blockPos = new Vector3(x, y, z);
        BlockType blockType = map[x, y, z];
        for (int i = 0; i < 6; i++)
        {
            if (CheckBlock(blockPos + faceChecks[i], width, height))
            {
                for (int j = 0; j != 4; j++) vertices.Add(blockPos + faceVertices[i][j]);
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(1, 0));
                
                List<int> currentTrianglesList = stoneTriangles;
                
                if (blockType == BlockType.Dirt) currentTrianglesList = dirtTriangles;
                else if (blockType == BlockType.Grass) currentTrianglesList = grassTriangles;
                else if (blockType == BlockType.Stone) currentTrianglesList = stoneTriangles;
                else if (blockType == BlockType.OakLog) currentTrianglesList = oakLogTriangles;
                else if (blockType == BlockType.OakLeaves) currentTrianglesList = oakLeavesTriangles;
                else if (blockType == BlockType.BirchLog) currentTrianglesList = birchLogTriangles;
                else if (blockType == BlockType.BirchLeaves) currentTrianglesList = birchLeavesTriangles;
                else if (blockType == BlockType.Bedrock) currentTrianglesList = bedrockTriangles;
                else if (blockType == BlockType.CoalOre) currentTrianglesList = coalTriangles;
                else if (blockType == BlockType.IronOre) currentTrianglesList = ironTriangles;
                else if (blockType == BlockType.GoldOre) currentTrianglesList = goldTriangles;
                else if (blockType == BlockType.DiamondOre) currentTrianglesList = diamondTriangles;
                else if (blockType == BlockType.EmeraldOre) currentTrianglesList = emeraldTriangles;
                else if (blockType == BlockType.SnowGrass) currentTrianglesList = snowGrassTriangles;
                else if (blockType == BlockType.SnowBlock) currentTrianglesList = snowBlockTriangles;
                else if (blockType == BlockType.Ice) currentTrianglesList = iceTriangles;
                else if (blockType == BlockType.SpruceLog) currentTrianglesList = spruceLogTriangles;
                else if (blockType == BlockType.SpruceLeaves) currentTrianglesList = spruceLeavesTriangles;
                else if (blockType == BlockType.Sand) currentTrianglesList = sandTriangles;
                else if (blockType == BlockType.Cactus) currentTrianglesList = cactusTriangles;
                else if (blockType == BlockType.JungleLog) currentTrianglesList = jungleLogTriangles;
                else if (blockType == BlockType.JungleLeaves) currentTrianglesList = jungleLeavesTriangles;

                currentTrianglesList.AddRange(new[] { vertexIndex, vertexIndex + 1, vertexIndex + 2, vertexIndex, vertexIndex + 2, vertexIndex + 3 });
                vertexIndex += 4;
            }
        }
    }

        public void ModifyBlock(int x, int y, int z, BlockType type)
    {
        int width = WorldGenerator.Instance != null ? WorldGenerator.Instance.chunkWidth : 16;
        int height = WorldGenerator.Instance != null ? WorldGenerator.Instance.chunkHeight : 256;
        
        if (x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < width)
        {
            map[x, y, z] = type;

            int globalX = x + chunkPosition.x * width;
            int globalZ = z + chunkPosition.y * width;
            if (WorldSaveManager.Instance != null)
            {
                WorldSaveManager.Instance.RegisterBlockChange(globalX, y, globalZ, type);
            }

            BuildMesh(width, height);

            if (x == 0) UpdateNeighborChunk(Vector2Int.left);
            else if (x == width - 1) UpdateNeighborChunk(Vector2Int.right);
            
            if (z == 0) UpdateNeighborChunk(Vector2Int.down);
            else if (z == width - 1) UpdateNeighborChunk(Vector2Int.up);
        }
    }

    private void UpdateNeighborChunk(Vector2Int direction)
    {
        Vector2Int neighborPos = chunkPosition + direction;
        Chunk[] allChunks = Object.FindObjectsOfType<Chunk>();
        if (allChunks == null || allChunks.Length == 0) allChunks = Object.FindObjectsByType<Chunk>(FindObjectsSortMode.None);

        for (int i = 0; i < allChunks.Length; i++)
        {
            if (allChunks[i] != null && allChunks[i].chunkPosition == neighborPos)
            {
                int width = WorldGenerator.Instance != null ? WorldGenerator.Instance.chunkWidth : 16;
                int height = WorldGenerator.Instance != null ? WorldGenerator.Instance.chunkHeight : 256;
                allChunks[i].BuildMesh(width, height);
                break;
            }
        }
    }
}