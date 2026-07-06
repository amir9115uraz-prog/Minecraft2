using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class BlockBreaker : MonoBehaviour
{
    public Camera playerCamera;
    public GameObject crackPrefab; 
    public float breakDistance = 5f;

    private BlockBreakVisualizer crackManager;
    private BlockDropCalculator dropCalculator;
    private Chunk targetChunk;
    private Vector3Int targetBlockPos;
    private float breakProgress = 0f;
    private float timeToBreak = 1.5f; 
    private Inventory playerInventory;

    void Start()
    {
        playerInventory = Object.FindAnyObjectByType<Inventory>();
        crackManager = gameObject.AddComponent<BlockBreakVisualizer>();
        crackManager.crackPrefab = crackPrefab;
        dropCalculator = gameObject.AddComponent<BlockDropCalculator>();
        dropCalculator.playerInventory = playerInventory;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, breakDistance))
            {
                SheepEntity sheep = hit.transform.GetComponentInParent<SheepEntity>();
                if (sheep == null) sheep = hit.transform.GetComponent<SheepEntity>();

                if (sheep != null)
                {
                    sheep.TakeDamage(5f, playerInventory);
                    ResetBreaking();
                    return; 
                }

                PlayerHealth enemyHealth = hit.transform.GetComponentInParent<PlayerHealth>();
                if (enemyHealth == null) enemyHealth = hit.transform.GetComponent<PlayerHealth>();

                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(20f); 
                    ResetBreaking();
                    return; 
                }
            }
        }

        if (Input.GetMouseButton(0))
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, breakDistance))
            {
                if (hit.transform.GetComponentInParent<SheepEntity>() != null || hit.transform.GetComponent<SheepEntity>() != null ||
                    hit.transform.GetComponentInParent<PlayerHealth>() != null || hit.transform.GetComponent<PlayerHealth>() != null)
                {
                    ResetBreaking();
                    return;
                }

                Chunk chunk = hit.transform.GetComponent<Chunk>();
                if (chunk != null)
                {
                    Vector3 point = hit.point - hit.normal * 0.5f;
                    int bx = Mathf.FloorToInt(point.x) - chunk.chunkPosition.x * 16;
                    int bz = Mathf.FloorToInt(point.z) - chunk.chunkPosition.y * 16;
                    int by = Mathf.FloorToInt(point.y);

                    Vector3Int blockPos = new Vector3Int(bx, by, bz);
                    
                    if (chunk != targetChunk || blockPos != targetBlockPos)
                    {
                        ResetBreaking();
                        targetChunk = chunk;
                        targetBlockPos = blockPos;
                        
                        Vector3 worldBlockPos = new Vector3(
                            Mathf.FloorToInt(point.x) + 0.5f, 
                            Mathf.FloorToInt(point.y) + 0.5f, 
                            Mathf.FloorToInt(point.z) + 0.5f
                        );
                        
                        crackManager.SpawnCrackEffect(worldBlockPos);
                    }

                    breakProgress += Time.deltaTime;
                    crackManager.UpdateCrackScale(breakProgress, timeToBreak);

                    if (breakProgress >= timeToBreak)
                    {
                        DestroyBlockInChunk(targetChunk, targetBlockPos);
                        ResetBreaking();
                    }
                }
                else
                {
                    ResetBreaking();
                }
            }
            else
            {
                ResetBreaking();
            }
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            ResetBreaking();
        }
    }

    void ResetBreaking()
    {
        breakProgress = 0f;
        targetChunk = null;
        targetBlockPos = new Vector3Int(-999, -999, -999);
        if (crackManager != null)
        {
            crackManager.ClearCrackEffect();
        }
    }

    void DestroyBlockInChunk(Chunk chunk, Vector3Int pos)
    {
        if (chunk == null || dropCalculator == null) return;

        BlockType blockType = chunk.GetBlockFromMap(pos.x, pos.y, pos.z);
        Debug.Log($"[BlockBreaker] Чтение карты Чанка. Тип разрушаемого блока: '{blockType}'");

        if (blockType == BlockType.Air || blockType == BlockType.Bedrock) return;

        Vector3 spawnPos = new Vector3(
            pos.x + chunk.chunkPosition.x * 16 + 0.5f,
            pos.y + 0.3f, 
            pos.z + chunk.chunkPosition.y * 16 + 0.5f
        );

        dropCalculator.SpawnAndCollectDrop(blockType, spawnPos);
        chunk.ModifyBlock(pos.x, pos.y, pos.z, BlockType.Air); 
    }
}

public class BlockBreakVisualizer : MonoBehaviour
{
    public GameObject crackPrefab;
    private GameObject currentCrackObject;

    public void SpawnCrackEffect(Vector3 position)
    {
        ClearCrackEffect();
        if (crackPrefab != null)
        {
            currentCrackObject = Instantiate(crackPrefab, position, Quaternion.identity);
        }
    }

    public void UpdateCrackScale(float progress, float maxTime)
    {
        if (currentCrackObject != null)
        {
            float scaleFactor = 1.01f + (progress / maxTime) * 0.02f;
            currentCrackObject.transform.localScale = Vector3.one * scaleFactor;
        }
    }

    public void ClearCrackEffect()
    {
        if (currentCrackObject != null)
        {
            Destroy(currentCrackObject);
            currentCrackObject = null;
        }
    }
}

public class BlockDropCalculator : MonoBehaviour
{
    public Inventory playerInventory;

    public void SpawnAndCollectDrop(BlockType blockType, Vector3 spawnPosition)
    {
        string prefabName = "Dropped_Dirt"; 
        string targetItemAssetID = "Dirt";

        string typeName = blockType.ToString().ToLower();
        
        if (typeName.Contains("log") || typeName.Contains("wood") || typeName.Contains("tree") || blockType == BlockType.OakLog)
        {
            prefabName = "Dropped_Wood"; 
            targetItemAssetID = "OakLog";
        }
        else if (typeName.Contains("stone") || blockType == BlockType.Stone)
        {
            prefabName = "Dropped_Stone";
            targetItemAssetID = "Stone";
        }
        else if (typeName.Contains("plank"))
        {
            prefabName = "Dropped_Planks";
            targetItemAssetID = "OakPlanks";
        }

        SpawnNetworkPrefab(prefabName, spawnPosition);
        AddItemToInventory(targetItemAssetID);
    }

    private void SpawnNetworkPrefab(string name, Vector3 pos)
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Instantiate(name, pos, Quaternion.identity);
        }
        else
        {
            GameObject prefab = Resources.Load<GameObject>(name);
            if (prefab != null) Instantiate(prefab, pos, Quaternion.identity);
        }
    }

    private void AddItemToInventory(string assetID)
    {
        if (playerInventory != null)
        {
            ItemData dropItem = playerInventory.FindItemInCache(assetID);
            if (dropItem != null)
            {
                playerInventory.AddItem(dropItem, 1);
                Debug.Log($"[BlockBreaker] В инвентарь успешно добавлен предмет: '{assetID}'");
            }
            else
            {
                Debug.LogWarning($"[BlockBreaker] Ошибка: Ассет предмета '{assetID}' не найден в папки Resources!");
            }
        }
    }
}

public class NetworkBreakData
{
    public Vector3Int TargetPosition { get; set; }
    public BlockType BlockDataType { get; set; }
    public double Timestamp { get; set; }

    public NetworkBreakData(Vector3Int position, BlockType type)
    {
        TargetPosition = position;
        BlockDataType = type;
        Timestamp = PhotonNetwork.Time;
    }
}
