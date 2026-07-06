using UnityEngine;
using Photon.Pun;

public class SheepEntity : MonoBehaviourPun
{
    [Header("Характеристики овцы")]
    public float health = 10f;
    public float moveSpeed = 2f;
    
    private Vector3 walkTarget;
    private float chooseTargetTimer = 0f;
    private CharacterController controller;
    
    private Vector3 knockbackVelocity;
    private float knockbackResetSpeed = 8f;
    private float verticalVelocity = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        chooseTargetTimer = Random.Range(3f, 8f);
        walkTarget = transform.position;
    }

    void Update()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine) return;

        if (knockbackVelocity.magnitude > 0.05f)
        {
            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, Time.deltaTime * knockbackResetSpeed);
        }
        else
        {
            knockbackVelocity = Vector3.zero;
        }

        chooseTargetTimer -= Time.deltaTime;
        if (chooseTargetTimer <= 0f) 
        {
            chooseTargetTimer = Random.Range(3f, 8f);
            walkTarget = transform.position + new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
        }

        if (controller != null && controller.enabled)
        {
            Vector3 dir = (walkTarget - transform.position).normalized;
            
            if (controller.isGrounded)
            {
                verticalVelocity = -2f; 
            }
            else
            {
                verticalVelocity -= 12f * Time.deltaTime; 
            }

            Vector3 finalMovement = new Vector3(dir.x, 0f, dir.z) * moveSpeed;
            finalMovement += knockbackVelocity;
            finalMovement.y = verticalVelocity;

            controller.Move(finalMovement * Time.deltaTime);

            if (knockbackVelocity.magnitude < 1f && new Vector3(dir.x, 0f, dir.z) != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }
        }
    }
    public void TakeDamage(float dmg, Inventory playerInv)
    {
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RPC("RpcApplyDamage", RpcTarget.All);
        }
        else
        {
            RpcApplyDamage();
        }
    }

    [PunRPC]
    void RpcApplyDamage()
    {
        health -= 2f; 
        Debug.Log($"[SheepEntity] Сетевое ХП овцы: {health}/10");
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 pushDirection = (transform.position - cam.transform.position);
            pushDirection.y = 0f; 
            pushDirection = pushDirection.normalized;

            knockbackVelocity = pushDirection * 14f;
            verticalVelocity = 6f; 
        }

        if (health <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        GameObject localPlayer = GameObject.FindWithTag("Player");
        if (localPlayer != null)
        {
            Inventory playerInv = localPlayer.GetComponent<Inventory>();
            if (playerInv != null)
            {
                ItemData muttonItem = playerInv.FindItemInCache("Mutton");
                if (muttonItem != null) playerInv.AddItem(muttonItem, 1);
            }
        }
        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                GetComponent<Collider>().enabled = false;
                foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
