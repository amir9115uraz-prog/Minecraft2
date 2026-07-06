using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public static World Instance;
    [Header("Трансформ игрока (находится автоматически кодом)")]
    public Transform player;          
    public GameObject chunkPrefab;    
    public int renderDistance = 6; 
    
    private Dictionary<Vector2Int, Chunk> activeChunks = new Dictionary<Vector2Int, Chunk>();
    private Vector2Int lastPlayerChunkPos = new Vector2Int(-999, -999);

    private void Awake() { Instance = this; }
    
    private void Update()
    {
        if (player == null) 
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                return; 
            }
        }

        int pChunkX = Mathf.FloorToInt(player.position.x / 16f);
        int pChunkZ = Mathf.FloorToInt(player.position.z / 16f);
        Vector2Int currentPlayerChunkPos = new Vector2Int(pChunkX, pChunkZ);

        if (currentPlayerChunkPos != lastPlayerChunkPos)
        {
            CheckAndGenerateChunks(pChunkX, pChunkZ);
            lastPlayerChunkPos = currentPlayerChunkPos;
        }
    }

    void CheckAndGenerateChunks(int playerX, int playerZ)
    {
        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var chunk in activeChunks)
        {
            int distX = Mathf.Abs(chunk.Key.x - playerX);
            int distZ = Mathf.Abs(chunk.Key.y - playerZ);
            int maxDist = renderDistance + 2;
            if (distX > maxDist || distZ > maxDist)
            {
                toRemove.Add(chunk.Key);
            }
        }
        foreach (var key in toRemove)
        {
            if (activeChunks[key] != null) 
            {
                Destroy(activeChunks[key].gameObject);
            }
            activeChunks.Remove(key);
        }
        int startX = playerX - renderDistance;
        int endX = playerX + renderDistance + 1;
        int startZ = playerZ - renderDistance;
        int endZ = playerZ + renderDistance + 1;
        for (int x = startX; x != endX; x++)
        {
            for (int z = startZ; z != endZ; z++)
            {
                Vector2Int chunkPos = new Vector2Int(x, z);
                if (activeChunks.ContainsKey(chunkPos) == false)
                {
                    Vector3 spawnPos = new Vector3(x * 16, 0, z * 16);
                    GameObject newChunk = Instantiate(chunkPrefab, spawnPos, Quaternion.identity, transform);
                    Chunk chunkComponent = newChunk.GetComponent<Chunk>();
                    activeChunks.Add(chunkPos, chunkComponent);
                    chunkComponent.Initialize(chunkPos);
                }
            }
        }
    }
}
