using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Photon.Pun;

public class WorldSaveManager : MonoBehaviour
{
    public static WorldSaveManager Instance;
    private Dictionary<Vector3Int, BlockType> modifiedBlocks = new Dictionary<Vector3Int, BlockType>();
    private string savePath;
    private string currentWorldName;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        currentWorldName = PlayerPrefs.GetString("CurrentWorldName", "DefaultWorld");
        savePath = Path.Combine(Application.persistentDataPath, "Worlds", currentWorldName + "_blocks.dat");

        LoadWorldData();
    }

    public BlockType GetSavedBlockOrOriginal(int x, int y, int z, BlockType originalType)
    {
        Vector3Int pos = new Vector3Int(x, y, z);
        if (modifiedBlocks.ContainsKey(pos))
        {
            return modifiedBlocks[pos];
        }
        return originalType;
    }

    public void RegisterBlockChange(int x, int y, int z, BlockType newType)
    {
        Vector3Int pos = new Vector3Int(x, y, z);
        
        if (modifiedBlocks.ContainsKey(pos))
            modifiedBlocks[pos] = newType;
        else
            modifiedBlocks.Add(pos, newType);
    }

    public void SaveWorldData()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        try
        {
            string directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            using (BinaryWriter writer = new BinaryWriter(File.Open(savePath, FileMode.Create)))
            {
                writer.Write(modifiedBlocks.Count);

                foreach (var kvp in modifiedBlocks)
                {
                    writer.Write(kvp.Key.x);
                    writer.Write(kvp.Key.y);
                    writer.Write(kvp.Key.z);
                    writer.Write((int)kvp.Value); 
                }
            }
            Debug.Log($"[WorldSaveManager] Мир '{currentWorldName}' успешно сохранен! Записано блоков: {modifiedBlocks.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError("[WorldSaveManager] Ошибка при сохранении мира: " + e.Message);
        }
    }
    private void LoadWorldData()
    {
        modifiedBlocks.Clear();

        if (!File.Exists(savePath))
        {
            Debug.Log("[WorldSaveManager] Файл сохранения не найден. Создается чистый мир.");
            return;
        }

        try
        {
            using (BinaryReader reader = new BinaryReader(File.Open(savePath, FileMode.Open)))
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    int x = reader.ReadInt32();
                    int y = reader.ReadInt32();
                    int z = reader.ReadInt32();
                    BlockType type = (BlockType)reader.ReadInt32();

                    modifiedBlocks.Add(new Vector3Int(x, y, z), type);
                }
            }
            Debug.Log($"[WorldSaveManager] Мир '{currentWorldName}' успешно загружен с диска! Найдено измененных блоков: {modifiedBlocks.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError("[WorldSaveManager] Ошибка при загрузке мира: " + e.Message);
        }
    }

    private void OnApplicationQuit()
    {
        SaveWorldData();
    }
}
