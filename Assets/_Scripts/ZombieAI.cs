using UnityEngine;
using System.Collections;
using Photon.Pun;

public class ZombieAI : MonoBehaviourPun
{
    public float moveSpeed = 3.5f;
    public float detectionRadius = 16f; 
    public float attackDistance = 1.5f;       
    public float attackCooldown = 1.5f;

    private Transform targetPlayer;
    private bool isAttacking = false;
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

        if (distance > attackDistance)
        {
            MoveTowardsTarget();
        }
        else if (!isAttacking)
        {
            StartCoroutine(AttackRoutine());
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

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        Debug.Log("[ZombieAI] Зомби бьет игрока на 2 сердца!");
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }
}
