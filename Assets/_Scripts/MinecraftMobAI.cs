using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Photon.Pun;

public class MinecraftMobAI : MonoBehaviourPun
{
    public enum MobBehavior { Zombie, Creeper, Skeleton }
    
    [Header("Тип монстра")]
    public MobBehavior behaviorType;

    [Header("Настройки движения")]
    public float moveSpeed = 3.5f;
    public float targetDetectionRadius = 16f; 
    public float attackDistance = 1.5f;       

    [Header("Настройки Скелета")]
    public GameObject arrowPrefab;            
    public float skeletonBowDistance = 10f;    
    public float skeletonAttackCooldown = 2f;  

    [Header("Настройки Крипера")]
    public float creeperFuseTime = 1.5f;       
    public int explosionRadius = 3;            
    public int explosionDamage = 40;           

    private Transform targetPlayer;
    private bool isAttacking = false;
    private float skeletonTimer;
    private CharacterController cc;

    void Start()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            enabled = false;
            return;
        }

        cc = GetComponent<CharacterController>();
        if (cc == null)
        {
            Debug.LogError($"[MobAI] На объекте {gameObject.name} забыли повесить CharacterController!");
        }
    }

    void Update()
    {
        if (targetPlayer == null)
        {
            FindNearestPlayer();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

        if (distanceToPlayer > targetDetectionRadius && behaviorType != MobBehavior.Creeper)
        {
            targetPlayer = null;
            return;
        }

        Vector3 lookPos = new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z);
        transform.LookAt(lookPos);

        switch (behaviorType)
        {
            case MobBehavior.Zombie:
                HandleZombie(distanceToPlayer);
                break;
            case MobBehavior.Creeper:
                HandleCreeper(distanceToPlayer);
                break;
            case MobBehavior.Skeleton:
                HandleSkeleton(distanceToPlayer);
                break;
        }
    }

    void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDistance = Mathf.Infinity;
        GameObject closestPlayer = null;

        foreach (GameObject player in players)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < closestDistance && dist <= targetDetectionRadius)
            {
                closestDistance = dist;
                closestPlayer = player;
            }
        }

        if (closestPlayer != null)
        {
            targetPlayer = closestPlayer.transform;
        }
    }

    void HandleZombie(float distance)
    {
        if (distance > attackDistance)
        {
            MoveTowardsTarget();
        }
        else if (!isAttacking)
        {
            StartCoroutine(ZombieAttackCooldown());
        }
    }

    IEnumerator ZombieAttackCooldown()
    {
        isAttacking = true;
        Debug.Log("[Zombie] Кусает игрока на 2 сердца!");
        yield return new WaitForSeconds(1.5f); 
        isAttacking = false;
    }

    void HandleSkeleton(float distance)
    {
        if (distance > skeletonBowDistance)
        {
            MoveTowardsTarget();
        }
        else if (distance < 4f)
        {
            Vector3 moveDir = (transform.position - targetPlayer.position).normalized;
            moveDir.y = -9.81f; 
            if (cc != null) cc.Move(moveDir * (moveSpeed * 0.7f) * Time.deltaTime);
        }

        skeletonTimer += Time.deltaTime;
        if (skeletonTimer >= skeletonAttackCooldown && distance <= skeletonBowDistance)
        {
            skeletonTimer = 0f;
            ShootArrow();
        }
    }

    void ShootArrow()
    {
        Debug.Log("[Skeleton] Пустил стрелу в игрока!");
        Vector3 spawnPos = transform.position + transform.forward * 0.5f + Vector3.up * 1f;
        
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Instantiate("Skeleton_Arrow", spawnPos, transform.rotation);
        }
        else
        {
            GameObject arrowPrefabFallback = Resources.Load<GameObject>("Skeleton_Arrow");
            if (arrowPrefabFallback != null) Instantiate(arrowPrefabFallback, spawnPos, transform.rotation);
        }
    }

    void HandleCreeper(float distance)
    {
        if (distance > attackDistance && !isAttacking)
        {
            MoveTowardsTarget();
        }
        else if (distance <= attackDistance && !isAttacking)
        {
            StartCoroutine(CreeperExplosionCountdown());
        }
    }

    IEnumerator CreeperExplosionCountdown()
    {
        isAttacking = true;
        Debug.Log("[Creeper] Тсссссс... ОН ШИПИТ!"); 
        
        float timer = 0f;
        while (timer < creeperFuseTime)
        {
            timer += Time.deltaTime;
            if (targetPlayer == null || Vector3.Distance(transform.position, targetPlayer.position) > attackDistance + 1f)
            {
                Debug.Log("[Creeper] Игрок отбежал, взрыв отменен.");
                isAttacking = false;
                yield break;
            }
            yield return null;
        }

        Explode();
    }

    void Explode()
    {
        Debug.Log("[Creeper] БАБАХ!!!");
        Vector3 explosionCenter = transform.position;

        if (targetPlayer != null)
        {
            float distToPlayer = Vector3.Distance(explosionCenter, targetPlayer.position);
            if (distToPlayer <= explosionRadius + 2f)
            {
                Debug.Log($"[Creeper] Нанес игроку урон взрывом!");
            }
        }

        Vector3Int centerInt = new Vector3Int(
            Mathf.FloorToInt(explosionCenter.x),
            Mathf.FloorToInt(explosionCenter.y),
            Mathf.FloorToInt(explosionCenter.z)
        );

        for (int x = -explosionRadius; x <= explosionRadius; x++)
        {
            for (int y = -explosionRadius; y <= explosionRadius; y++)
            {
                for (int z = -explosionRadius; z <= explosionRadius; z++)
                {
                    Vector3Int blockTargetPos = centerInt + new Vector3Int(x, y, z);
                    
                    if (Vector3.Distance(explosionCenter, new Vector3(blockTargetPos.x, blockTargetPos.y, blockTargetPos.z)) <= explosionRadius)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(new Vector3(blockTargetPos.x + 0.5f, blockTargetPos.y + 5f, blockTargetPos.z + 0.5f), Vector3.down, out hit, 10f))
                        {
                            Chunk chunk = hit.transform.GetComponent<Chunk>();
                            if (chunk != null)
                            {
                                int bx = blockTargetPos.x - chunk.chunkPosition.x * 16;
                                int bz = blockTargetPos.z - chunk.chunkPosition.y * 16;
                                int by = blockTargetPos.y;

                                chunk.ModifyBlock(bx, by, bz, BlockType.Air);
                            }
                        }
                    }
                }
            }
        }

        MobHealth myHealth = GetComponent<MobHealth>();
        if (myHealth != null)
        {
            if (PhotonNetwork.IsConnected) PhotonNetwork.Destroy(gameObject);
            else Destroy(gameObject);
        }
    }

    void MoveTowardsTarget()
    {
        if (cc == null || targetPlayer == null) return;

        Vector3 direction = (targetPlayer.position - transform.position).normalized;
        direction.y = -9.81f; 

        cc.Move(direction * moveSpeed * Time.deltaTime);
    }
}
