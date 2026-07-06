using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;

public class WorldListMenu : MonoBehaviour
{
    [Header("Настройки UI")]
    public Transform contentPanel;      
    public GameObject worldButtonPrefab; 

    [Header("Имя игровой сцены")]
    public string gameSceneName = "SampleScene";

    void Start()
    {
        RefreshWorldList();
    }
    public void RefreshWorldList()
    {
        if (contentPanel == null || worldButtonPrefab == null)
        {
            Debug.LogError("[WorldListMenu] Ошибка: Не привязаны Content Panel или World Button Prefab в Инспекторе!");
            return;
        }
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
        string folderPath = Path.Combine(Application.persistentDataPath, "Worlds");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            return; 
        }
        string[] files = Directory.GetFiles(folderPath, "*_seed.txt");

        foreach (string file in files)
        {
            string worldName = Path.GetFileNameWithoutExtension(file).Replace("_seed", "");
            GameObject newButtonObj = Instantiate(worldButtonPrefab, contentPanel);
            newButtonObj.name = "WorldBtn_" + worldName;
            TMP_Text buttonText = newButtonObj.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = worldName;
            }
            else
            {
                Text simpleText = newButtonObj.GetComponentInChildren<Text>();
                if (simpleText != null) simpleText.text = worldName;
            }
            Button btn = newButtonObj.GetComponent<Button>();
            if (btn != null)
            {
                string capturedWorldName = worldName; 
                btn.onClick.AddListener(() => SelectAndLoadWorld(capturedWorldName));
            }
        }
    }
    void SelectAndLoadWorld(string worldName)
    {
        Debug.Log($"[WorldListMenu] Выбран мир: '{worldName}'. Запускается загрузка блоков...");
        PlayerPrefs.SetString("CurrentWorldName", worldName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }
}
