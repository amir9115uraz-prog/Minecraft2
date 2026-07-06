using UnityEngine;
using Photon.Pun;

public class MobSpawner : MonoBehaviourPun
{
    [Header("Префабы Мобвов (Должны лежать в папке Resources!)")]
    public string zombiePrefabName = "Zombie_Mob";
    public string skeletonPrefabName = "Skeleton_Mob";
    public string creeperPrefabName = "Creeper_Mob";

    [Header("Настройки спавна")]
    public float spawnInterval = 10f; 
    public float minSpawnRadius = 15f; 
    public float maxSpawnRadius = 35f;

    private float spawnTimer;
    private Transform localPlayer;

    void Update()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;
        if (DayNightCycle.Instance == null || !DayNightCycle.Instance.IsNight) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            TrySpawnMobGroup();
        }
    }

    void TrySpawnMobGroup()
    {
        if (localPlayer == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) localPlayer = playerObj.transform;
            return;
        }

        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minSpawnRadius, maxSpawnRadius);
        Vector3 spawnPos = new Vector3(localPlayer.position.x + randomCircle.x, localPlayer.position.y + 10f, localPlayer.position.z + randomCircle.y);
        RaycastHit hit;
        if (Physics.Raycast(spawnPos, Vector3.down, out hit, 20f))
        {
            Vector3 finalSpawnPos = hit.point + Vector3.up * 0.1f;
            int randomMob = Random.Range(0, 3);
            string selectedPrefab = zombiePrefabName;

            if (randomMob == 1) selectedPrefab = skeletonPrefabName;
            else if (randomMob == 2) selectedPrefab = creeperPrefabName;

            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Instantiate(selectedPrefab, finalSpawnPos, Quaternion.identity);
            }
            else
            {
                GameObject prefab = Resources.Load<GameObject>(selectedPrefab);
                if (prefab != null) Instantiate(prefab, finalSpawnPos, Quaternion.identity);
            }
            
            Debug.Log($"[MobSpawner] Ночь! Заспавнен моб: {selectedPrefab} в точке {finalSpawnPos}");
        }
    }
}
