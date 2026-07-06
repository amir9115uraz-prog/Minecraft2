using UnityEngine;

public class BlockPhysicsManager : MonoBehaviour
{
    public static BlockPhysicsManager Instance;

    private ChunkLocationResolver chunkFinder;
    private BlockFallSimulator gravitySimulator;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        chunkFinder = gameObject.AddComponent<ChunkLocationResolver>();
        gravitySimulator = gameObject.AddComponent<BlockFallSimulator>();
        gravitySimulator.SetResolver(chunkFinder);
    }

    public bool TryModifyBlockWithPhysics(int globalX, int globalY, int globalZ, BlockType newType)
    {
        if (WorldGenerator.Instance == null) return false;

        if (globalY <= 1)
        {
            return false;
        }

        Chunk currentChunk = chunkFinder.GetChunkAtGlobalPosition(globalX, globalZ);
        if (currentChunk != null)
        {
            int width = WorldGenerator.Instance.chunkWidth;
            
            int localX = globalX - (currentChunk.chunkPosition.x * width);
            int localZ = globalZ - (currentChunk.chunkPosition.y * width);

            currentChunk.ModifyBlock(localX, globalY, localZ, newType);

            if (newType == BlockType.Air)
            {
                CheckAndApplyFall(globalX, globalY + 1, globalZ);
            }

            return true;
        }

        return false;
    }

    public void CheckAndApplyFall(int x, int y, int z)
    {
        if (gravitySimulator != null)
        {
            gravitySimulator.ExecuteFallLogic(x, y, z);
        }
    }
}

public class ChunkLocationResolver : MonoBehaviour
{
    public Chunk GetChunkAtGlobalPosition(int globalX, int globalZ)
    {
        if (WorldGenerator.Instance == null) return null;
        int width = WorldGenerator.Instance.chunkWidth;
        Vector2Int targetChunkPos = new Vector2Int(Mathf.FloorToInt((float)globalX / width), Mathf.FloorToInt((float)globalZ / width));
        
        Chunk[] allChunks = Object.FindObjectsOfType<Chunk>();
        if (allChunks == null || allChunks.Length == 0) allChunks = Object.FindObjectsByType<Chunk>(FindObjectsSortMode.None);

        for (int i = 0; i < allChunks.Length; i++)
        {
            if (allChunks[i] != null && allChunks[i].chunkPosition == targetChunkPos)
            {
                return allChunks[i];
            }
        }
        return null;
    }
}

public class BlockFallSimulator : MonoBehaviour
{
    private ChunkLocationResolver resolver;
    private VoxelEnvironmentScanner environmentScanner;

    private void Awake()
    {
        environmentScanner = gameObject.AddComponent<VoxelEnvironmentScanner>();
    }

    public void SetResolver(ChunkLocationResolver resolverInstance)
    {
        resolver = resolverInstance;
    }

    public void ExecuteFallLogic(int x, int y, int z)
    {
        if (WorldGenerator.Instance == null || y >= WorldGenerator.Instance.chunkHeight || resolver == null) return;

        BlockType aboveBlock = WorldGenerator.Instance.GetBlockType(x, y, z);

        if (aboveBlock == BlockType.Sand || aboveBlock == BlockType.Dirt || aboveBlock == BlockType.SnowBlock)
        {
            int targetY = environmentScanner.FindLowestAirY(x, y, z);

            if (targetY != y)
            {
                int width = WorldGenerator.Instance.chunkWidth;

                Chunk sourceChunk = resolver.GetChunkAtGlobalPosition(x, z);
                if (sourceChunk != null)
                {
                    int sLocalX = x - (sourceChunk.chunkPosition.x * width);
                    int sLocalZ = z - (sourceChunk.chunkPosition.y * width);
                    sourceChunk.ModifyBlock(sLocalX, y, sLocalZ, BlockType.Air);
                }

                Chunk targetChunk = resolver.GetChunkAtGlobalPosition(x, z);
                if (targetChunk != null)
                {
                    int tLocalX = x - (targetChunk.chunkPosition.x * width);
                    int tLocalZ = z - (targetChunk.chunkPosition.y * width);
                    targetChunk.ModifyBlock(tLocalX, targetY, tLocalZ, aboveBlock);
                }

                ExecuteFallLogic(x, y + 1, z);
            }
        }
    }
}

public class VoxelEnvironmentScanner : MonoBehaviour
{
    public int FindLowestAirY(int x, int startY, int z)
    {
        int currentY = startY;
        while (currentY > 1)
        {
            BlockType belowBlock = WorldGenerator.Instance.GetBlockType(x, currentY - 1, z);
            if (belowBlock == BlockType.Air)
            {
                currentY--;
            }
            else
            {
                break;
            }
        }
        return currentY;
    }
}
