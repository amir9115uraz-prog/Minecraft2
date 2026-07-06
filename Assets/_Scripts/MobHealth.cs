using UnityEngine;
using Photon.Pun;

public class MobHealth : MonoBehaviourPunCallbacks
{
    public enum MobType { Zombie, Skeleton, Creeper, Cow, Sheep, Chicken }
    
    [Header("Настройки Моба")]
    public MobType mobType;
    public float maxHealth = 20f;
    private float currentHealth;

    [Header("Ссылки на ItemData Лута")]
    public ItemData rottenFlesh;  
    public ItemData gunpowder;    
    public ItemData bone;         
    public ItemData bow;          
    public ItemData rawBeef;      
    public ItemData rawMutton;    
    public ItemData rawChicken;   

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage);
        }
        else
        {
            RPC_TakeDamage(damage);
        }
    }

    [PunRPC]
    void RPC_TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"[MobHealth] {gameObject.name} получил {damage} урона. Осталось HP: {currentHealth}");

        
        }
        
    }

