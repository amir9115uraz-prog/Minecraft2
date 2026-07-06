using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviourPunCallbacks, IPunObservable
{
    public static GameObject LocalPlayerInstance;

    [Header("Настройки здоровья")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Интерфейс (UI)")]
    public GameObject heartContainer; 
    public GameObject respawnPanel;   
    public Button respawnButton;     

    private float lastYPosition;
    private bool isGroundedBefore;
    private CharacterController characterController; 
    private bool isDead = false;
    private List<Image> heartImages = new List<Image>();
    private float invulnerabilityTimer = 0f;
    private PlayerController playerControllerScript;
    private Inventory playerInventory;

    void Awake()
    {
        currentHealth = maxHealth;
        characterController = GetComponent<CharacterController>();
        playerInventory = GetComponent<Inventory>();

        if (photonView == null || photonView.IsMine)
        {
            LocalPlayerInstance = this.gameObject;
        }
    }

    void Start()
    {
        if (photonView.IsMine)
        {
            if (respawnPanel == null)
            {
                Canvas mainCanvas = Object.FindAnyObjectByType<Canvas>();
                if (mainCanvas != null)
                {
                    foreach (Transform child in mainCanvas.transform.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name == "RespawnPanel")
                        {
                            respawnPanel = child.gameObject;
                            break;
                        }
                    }
                }
            }

            if (respawnButton == null && respawnPanel != null) 
                respawnButton = respawnPanel.GetComponentInChildren<Button>(true);

            if (respawnPanel != null) respawnPanel.SetActive(false);
            
            if (respawnButton != null) 
            {
                respawnButton.onClick.RemoveAllListeners();
                respawnButton.onClick.AddListener(RpcRespawn);
            }
            
            FindAndSetupHearts();
            StartCoroutine(SafeSpawnCoroutine(0, 0));
        }
    }

    void FindAndSetupHearts()
    {
        if (heartContainer == null) heartContainer = GameObject.Find("HeartContainer");
        
        if (heartContainer != null)
        {
            heartImages.Clear();
            Image[] images = heartContainer.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject != heartContainer)
                {
                    heartImages.Add(img);
                }
            }
        }
        UpdateHeartsUI();
    }

    IEnumerator SafeSpawnCoroutine(int x, int z)
    {
        if (characterController != null) characterController.enabled = false;
        transform.position = new Vector3(x, 120f, z);
        yield return new WaitForSeconds(2.0f);

        float finalY = 85f;
        if (WorldGenerator.Instance != null)
        {
            finalY = WorldGenerator.Instance.GetSpawnHeightAt(x, z);
            if (finalY < 10f) finalY = 75f;
        }

        transform.position = new Vector3(x, finalY + 1f, z);
        yield return new WaitForFixedUpdate();

        if (characterController != null) 
        {
            characterController.enabled = true;
            lastYPosition = transform.position.y;
        }
        
        invulnerabilityTimer = 3f; 
    }

    void Update()
    {
        if (!photonView.IsMine || isDead) return;

        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
            return; 
        }

        
       
        {
            BlockPlacer placerScript = GetComponentInChildren<BlockPlacer>();
            if (placerScript != null)
            {
                var fields = placerScript.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                int activeIndex = 0;
                foreach (var f in fields)
                {
                    if (f.Name == "selectedSlot") activeIndex = (int)f.GetValue(placerScript);
                }

                if (activeIndex >= 0 && activeIndex < playerInventory.slots.Count)
                {
                    var currentSlot = playerInventory.slots[activeIndex];
                 
                    if (currentSlot != null && currentSlot.item != null && currentSlot.item.itemID.ToLower() == "mutton")
                    {
                        currentHealth += 20f; 
                        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
                        
                        currentSlot.count--; 
                        if (currentSlot.count <= 0) currentSlot.Clear();
                        
                        playerInventory.UpdateAllUISlots();
                        UpdateHeartsUI();
                        Debug.Log("[PlayerHealth] Игрок съел баранину и восстановил 20 HP!");
                    }
                }
            }
        }

        if (playerControllerScript == null) playerControllerScript = GetComponent<PlayerController>();

        if (characterController != null && characterController.enabled)
        {
            if (characterController.isGrounded && !isGroundedBefore && playerControllerScript != null)
            {
                float impactVelocity = playerControllerScript.VerticalVelocity;

                if (impactVelocity < -12f)
                {
                    float rawDamage = Mathf.Abs(impactVelocity) * 1.2f; 
                    float finalDamage = Mathf.Clamp(rawDamage, 5f, maxHealth);

                    TakeDamage(finalDamage);
                    Debug.Log($"[PlayerHealth] Приземление. Скорость: {impactVelocity}. Урон: {finalDamage}");
                }
            }
            isGroundedBefore = characterController.isGrounded;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RpcTakeDamage", RpcTarget.All, damage);
        }
        else
        {
            RpcTakeDamage(damage);
        }
    }

    [PunRPC]
    void RpcTakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        UpdateHeartsUI();

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    void UpdateHeartsUI()
    {
        if (!photonView.IsMine || heartImages == null || heartImages.Count == 0) return;

        float healthPerHeart = maxHealth / heartImages.Count;

        for (int i = 0; i < heartImages.Count; i++)
        {
            float heartHealthValue = (i + 1) * healthPerHeart;

            if (currentHealth >= heartHealthValue)
            {
                heartImages[i].enabled = true;
                heartImages[i].fillAmount = 1f; 
            }
            else if (currentHealth >= heartHealthValue - (healthPerHeart / 2f))
            {
                heartImages[i].enabled = true;
                heartImages[i].fillAmount = 0.5f; 
            }
            else
            {
                heartImages[i].enabled = false; 
            }
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log("[PlayerHealth] Игрок погиб!");

        if (photonView.IsMine)
        {
            if (respawnPanel != null) respawnPanel.SetActive(true);
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var placer = GetComponentInChildren<BlockPlacer>();
            var breaker = GetComponentInChildren<BlockBreaker>();
            
            if (placer != null) placer.enabled = false;
            if (breaker != null) breaker.enabled = false;
        }
    }

    public void RpcRespawn()
    {
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RespawnLogic", RpcTarget.All);
        }
        else
        {
            RespawnLogic();
        }
    }

    [PunRPC]
    void RespawnLogic()
    {
        isDead = false;
        currentHealth = maxHealth;
        
        if (photonView.IsMine)
        {
            if (respawnPanel != null) respawnPanel.SetActive(false);
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var placer = GetComponentInChildren<BlockPlacer>();
            var breaker = GetComponentInChildren<BlockBreaker>();
            if (placer != null) placer.enabled = true;
            if (breaker != null) breaker.enabled = true;
            
            StartCoroutine(SafeSpawnCoroutine(0, 0));
        }

        UpdateHeartsUI();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentHealth);
            stream.SendNext(isDead);
        }
        else
        {
            currentHealth = (float)stream.ReceiveNext();
            isDead = (bool)stream.ReceiveNext();
            if (!photonView.IsMine) UpdateHeartsUI();
        }
        }
            
        }