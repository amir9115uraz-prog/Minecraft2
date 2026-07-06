using UnityEngine;
using Photon.Pun;

public class CraftingToggle : MonoBehaviourPun
{
    private GameObject craftingWindow; 
    private bool isWindowOpen = false;
    private MonoBehaviour[] scriptsToToggle;
    private bool componentsFound = false;

    void Start()
    {
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine)
        {
            enabled = false;
            return;
        }
        craftingWindow = GameObject.Find("CraftingWindow");
        
        if (craftingWindow == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Transform cwTransform = canvas.transform.Find("CraftingWindow");
                if (cwTransform != null) craftingWindow = cwTransform.gameObject;
            }
        }

        scriptsToToggle = GetComponents<MonoBehaviour>();

        if (craftingWindow != null)
        {
            craftingWindow.SetActive(false); 
            LockCursor();
            componentsFound = true;
            Debug.Log("[CraftingToggle] Система успешно инициализирована автоматически!");
        }
        else
        {
            Debug.LogError("[CraftingToggle] Критическая ошибка: Объект 'CraftingWindow' не найден в Иерархии (Hierarchy)! Проверь имя.");
        }
    }

    void Update()
    {
        if (!componentsFound) return;
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleCraftingWindow();
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && isWindowOpen)
        {
            ToggleCraftingWindow();
        }
    }

    void ToggleCraftingWindow()
    {
        isWindowOpen = !isWindowOpen;
        if (craftingWindow != null) craftingWindow.SetActive(isWindowOpen);

        if (isWindowOpen)
        {
            UnlockCursor();
        }
        else
        {
            LockCursor(); 
        }
        foreach (MonoBehaviour script in scriptsToToggle)
        {
            if (script == null || script == this || script is PhotonView) continue;
            
            script.enabled = !isWindowOpen;
        }
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
