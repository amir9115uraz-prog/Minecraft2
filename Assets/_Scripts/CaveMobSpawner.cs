using UnityEngine;
using Photon.Pun;

public class CaveMobSpawner : MonoBehaviour
{
    [Header("Префабы монстров")]
    public GameObject zombiePrefab; 

    [Header("Настройки спавна")]
    public float spawnInterval = 4f; 
    public int maxMobsAroundPlayer = 10;
    public float minSpawnRadius = 10f;
    public float maxSpawnRadius = 25f;

    private float timer;
    private Transform playerTransform;

    void Start()
    {
        timer = spawnInterval;
    }

        void Update()
    {
       
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom)
        {
            return;
        }

       
        if (playerTransform == null)
        {
            if (PlayerHealth.LocalPlayerInstance != null)
            {
                playerTransform = PlayerHealth.LocalPlayerInstance.transform;
            }
            else
            {
            
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = spawnInterval;
            TrySpawnMonsterInCave();
        }
    }


    void TrySpawnMonsterInCave()
    {
        GameObject[] mobs = GameObject.FindGameObjectsWithTag("Enemy");
        if (mobs.Length >= maxMobsAroundPlayer) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(minSpawnRadius, maxSpawnRadius);
        int spawnX = Mathf.FloorToInt(playerTransform.position.x + Mathf.Cos(angle) * distance);
        int spawnZ = Mathf.FloorToInt(playerTransform.position.z + Mathf.Sin(angle) * distance);

        int spawnY = Random.Range(10, 45);

        if (WorldGenerator.Instance != null)
        {
            Vector3 spawnPosition = new Vector3(spawnX + 0.5f, spawnY + 0.2f, spawnZ + 0.5f);
            
            if (zombiePrefab != null)
            {
                PhotonNetwork.Instantiate(zombiePrefab.name, spawnPosition, Quaternion.identity);
                Debug.Log($"[CaveMobSpawner] Зомби гарантированно создан под землей на высоте: {spawnY}");
            }
        }
    }
}
