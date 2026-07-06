using UnityEngine;
using Photon.Pun;

public class DroppedBlock : MonoBehaviourPun
{
    [Header("Какой предмет лежит внутри кубика")]
    public ItemData itemInside; 
    
    [Header("Настройки подбора (Minecraft-style)")]
    public float rotationSpeed = 60f;
    public float pickupDistance = 2.5f;  
    public float flySpeed = 8f;        

    private Transform playerTransform;
    private bool isPickedUp = false;

    void Start()
    {

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true; 
        }
    }

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        if (isPickedUp) return;

        if (playerTransform == null)
        {
            InventoryController localInventory = FindObjectOfType<InventoryController>();
            if (localInventory != null)
            {
                playerTransform = localInventory.transform;
            }
            return; 
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance <= pickupDistance)
        {
            Vector3 targetPos = playerTransform.position + Vector3.up * 0.5f; 
            transform.position = Vector3.MoveTowards(transform.position, targetPos, flySpeed * Time.deltaTime);
            if (distance <= 0.5f)
            {
                TryPutInInventory();
            }
        }
    }

    void TryPutInInventory()
    {
        if (isPickedUp || playerTransform == null) return;

        InventoryController inv = playerTransform.GetComponent<InventoryController>();
        
        if (inv != null && itemInside != null)
        {
            isPickedUp = true; 
            bool success = inv.AddItem(itemInside);

            if (success)
            {
                Debug.Log($"[DroppedBlock] Магнит успешно засосал блок: {itemInside.itemName}!");

               
                if (PhotonNetwork.IsConnected && photonView != null)
                {
                    if (photonView.IsMine)
                    {
                        PhotonNetwork.Destroy(gameObject);
                    }
                    else
                    {
                        photonView.RPC("RequestDestroyBlock", RpcTarget.MasterClient);
                    }
                }
                else
                {
                    Destroy(gameObject); 
                }
            }
            else
            {
                isPickedUp = false; 
            }
        }
    }

    [PunRPC]
    void RequestDestroyBlock()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
