using UnityEngine;
using System.Collections;
using Photon.Pun;

public class CreeperAI : MonoBehaviourPun
{
    public float moveSpeed = 4f;
    public float detectionRadius = 16f; 
    public float fuseDistance = 2f;       
    public float fuseTime = 1.5f;       
    public int explosionRadius = 3;            

    private Transform targetPlayer;
    private bool isExploding = false;
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

        if (isExploding) return;

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

        if (distance > fuseDistance)
        {
            MoveTowardsTarget();
        }
        else
        {
            StartCoroutine(ExplosionCountdown());
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

    IEnumerator ExplosionCountdown()
    {
        isExploding = true;
        Debug.Log("[CreeperAI] Тсссссс... Крипер шипит!"); 
        
        float timer = 0f;
        while (timer < fuseTime)
        {
            timer += Time.deltaTime;
            if (targetPlayer == null || Vector3.Distance(transform.position, targetPlayer.position) > fuseDistance + 1.5f)
            {
                Debug.Log("[CreeperAI] Игрок отбежал, взрыв отменен.");
                isExploding = false;
                yield break;
            }
            yield return null;
        }

        Explode();
    }

    void Explode()
    {
        Debug.Log("[CreeperAI] БАБАХ!!!");
        Vector3 explosionCenter = transform.position;

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

        if (PhotonNetwork.IsConnected) PhotonNetwork.Destroy(gameObject);
        else Destroy(gameObject);
    }
}
