using UnityEngine;
using Photon.Pun;

public class SkeletonAI : MonoBehaviourPun
{
    public float moveSpeed = 3.5f;
    public float detectionRadius = 16f; 
    public float bowDistance = 10f;    
    public float attackCooldown = 2f;  

    private Transform targetPlayer;
    private float attackTimer;
    private CharacterController cc;

    void Start()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            enabled = false;
            return;
        }

        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        GameObject localPlayerObj = GameObject.FindWithTag("Player");
        if (localPlayerObj != null)
        {
            float distToMe = Vector3.Distance(transform.position, localPlayerObj.transform.position);
            if (distToMe > 64f)
            {
                if (Photon.Pun.PhotonNetwork.IsConnected && photonView != null)
                {
                    if (photonView.IsMine) Photon.Pun.PhotonNetwork.Destroy(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
                return;
            }
        }

        if (targetPlayer == null)
        {
            FindNearestPlayer();
            return;
        }

        float distance = Vector3.Distance(transform.position, targetPlayer.position);

        if (distance > detectionRadius)
        {
            targetPlayer = null;
            return;
        }

        Vector3 lookPos = new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z);
        transform.LookAt(lookPos);

        if (distance > bowDistance)
        {
            MoveTowardsTarget();
        }
        else if (distance < 4f)
        {
            Vector3 moveDir = (transform.position - targetPlayer.position).normalized;
            moveDir.y = -9.81f; 
            if (cc != null) cc.Move(moveDir * (moveSpeed * 0.7f) * Time.deltaTime);
        }

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown && distance <= bowDistance)
        {
            attackTimer = 0f;
            ShootArrow();
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
            if (dist < closestDistance && dist <= detectionRadius)
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

    void MoveTowardsTarget()
    {
        if (cc == null || targetPlayer == null) return;

        Vector3 direction = (targetPlayer.position - transform.position).normalized;
        direction.y = -9.81f; 

        cc.Move(direction * moveSpeed * Time.deltaTime);
    }

    void ShootArrow()
    {
        Debug.Log("[SkeletonAI] Пустил стрелу в игрока!");
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
}
