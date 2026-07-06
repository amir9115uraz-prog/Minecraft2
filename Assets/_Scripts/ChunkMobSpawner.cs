using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class ChunkMobSpawner : MonoBehaviourPun
{
    [Header("Базовые настройки спавнера")]
    [Range(0f, 1f)] public float spawnChance = 0.015f;
    public int maxTotalMobs = 7;
    public float minDistanceBetweenMobs = 20f;

    [Header("Ограничения радиуса от игрока")]
    public float minSpawnRadiusFromPlayer = 20f;
    public float maxSpawnRadiusFromPlayer = 45f;

    [Header("Имена сетевых префабов")]
    public string zombieName = "Zombie_Mob";
    public string skeletonName = "Skeleton_Mob";
    public string creeperName = "Creeper_Mob";
    public string cowName = "Cow_Mob";
    public string sheepName = "Sheep_Mob";
    public string chickenName = "Chicken_Mob";

              private void Start()
    {
        RenderSettings.fog = true; 
        RenderSettings.fogMode = FogMode.Linear; 
        RenderSettings.fogStartDistance = 110f;   
        RenderSettings.fogEndDistance = 240f;    
        
        RenderSettings.fogColor = new Color(0.85f, 0.88f, 0.92f); 

        if (PhotonNetwork.IsConnected == true)
        {
            if (PhotonNetwork.IsMasterClient == false)
            {
                enabled = false;
                return;
            }
        }

        Debug.Log("[ChunkMobSpawner] Воксельная система спавна мобов запущена!");
    }


    public void OnChunkGenerated(Chunk chunk)
    {
        if (chunk == null)
        {
            return;
        }

        GameObject localPlayerObj = GameObject.FindWithTag("Player");
        if (localPlayerObj == null)
        {
            return;
        }

        float currentRandomRoll = Random.value;
        if (currentRandomRoll > spawnChance)
        {
            return;
        }

        bool isNightTime = false;
        if (DayNightCycle.Instance != null)
        {
            if (DayNightCycle.Instance.IsNight == true)
            {
                isNightTime = true;
            }
        }

        if (isNightTime == true)
        {
            GameObject[] currentEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            int totalActiveEnemies = 0;
            
            if (currentEnemies != null)
            {
                totalActiveEnemies = currentEnemies.Length;
            }

            if (totalActiveEnemies >= maxTotalMobs)
            {
                return;
            }
        }

        int localX = Random.Range(2, 14);
        int localZ = Random.Range(2, 14);

        int chunkGlobalX = chunk.chunkPosition.x * 16;
        int chunkGlobalZ = chunk.chunkPosition.y * 16;

        float calculatedX = localX + chunkGlobalX + 0.5f;
        float calculatedZ = localZ + chunkGlobalZ + 0.5f;

        Vector3 rayStartPos = new Vector3(calculatedX, 100f, calculatedZ);

        Vector3 playerPos = localPlayerObj.transform.position;
        float distanceToPlayer = Vector3.Distance(rayStartPos, playerPos);

        if (distanceToPlayer < minSpawnRadiusFromPlayer)
        {
            return;
        }

        if (distanceToPlayer > maxSpawnRadiusFromPlayer)
        {
            return;
        }

        RaycastHit hit;
        bool hasHitTerrain = Physics.Raycast(rayStartPos, Vector3.down, out hit, 150f);

        if (hasHitTerrain == true)
        {
            Chunk hitChunk = hit.transform.GetComponent<Chunk>();
            if (hitChunk != null)
            {
                Vector3 spawnPosition = hit.point;
                spawnPosition.y = spawnPosition.y + 0.1f;

                GameObject[] allActiveMobs = GameObject.FindGameObjectsWithTag("Enemy");
                bool isTooCloseToOtherMob = false;

                if (allActiveMobs != null)
                {
                    for (int i = 0; i < allActiveMobs.Length; i++)
                    {
                        GameObject currentCheckMob = allActiveMobs[i];
                        if (currentCheckMob != null)
                        {
                            Vector3 mobPosition = currentCheckMob.transform.position;
                            float distanceBetween = Vector3.Distance(spawnPosition, mobPosition);

                            if (distanceBetween < minDistanceBetweenMobs)
                            {
                                isTooCloseToOtherMob = true;
                                break;
                            }
                        }
                    }
                }

                if (isTooCloseToOtherMob == true)
                {
                    return;
                }

                string selectedPrefab = zombieName;
                int randomChoice = Random.Range(0, 3);

                if (randomChoice == 1)
                {
                    selectedPrefab = skeletonName;
                }
                else if (randomChoice == 2)
                {
                    selectedPrefab = creeperName;
                }

                if (isNightTime == false)
                {
                    int animalChoice = Random.Range(0, 3);
                    if (animalChoice == 0)
                    {
                        selectedPrefab = cowName;
                    }
                    else if (animalChoice == 1)
                    {
                        selectedPrefab = sheepName;
                    }
                    else
                    {
                        selectedPrefab = chickenName;
                    }
                }

        
                GameObject loadedPrefab = Resources.Load<GameObject>(selectedPrefab);
                if (loadedPrefab != null)
                {
                    Instantiate(loadedPrefab, spawnPosition, Quaternion.identity);
                }

                Debug.Log("[ChunkMobSpawner] Моб успешно заспавнен!");
            }
        }
    }
}
