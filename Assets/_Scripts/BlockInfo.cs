using UnityEngine;
using Photon.Pun;

public class BlockInfo : MonoBehaviourPun
{
    public ItemData dropItem; 
    public float blockHealth = 3f;
    public void HitBlock(float amount, Inventory playerInventory)
    {
        photonView.RPC("DamageByNetwork", RpcTarget.All, amount);
    }

    [PunRPC]
    void DamageByNetwork(float amount)
    {
        blockHealth -= amount;
        if (blockHealth <= 0)
        {
            if (photonView.IsMine && transform.GetComponent<PhotonView>() != null) 
            {
                
            }
            
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else if (gameObject.GetComponent<PhotonView>() == null)
            {
                Destroy(gameObject);
            }
        }
    }
}
