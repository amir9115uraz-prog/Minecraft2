using UnityEngine;
using TMPro; 
using Photon.Pun;
using Photon.Realtime;
using System.IO;

public class MainMenuLobby : MonoBehaviourPunCallbacks
{
    [Header("Поле ввода Имени / Кода Мира")]
    public TMP_InputField roomCodeInputField; 

    private string savedRoomCode = "DefaultWorld";

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.OfflineMode = false;
        if (roomCodeInputField == null)
        {
            roomCodeInputField = Object.FindAnyObjectByType<TMP_InputField>();
        }
        ShowExistingWorldsInConsole();
    }
    public void CreateRoomByCode()
    {
        string roomCode = "SoloWorld";
        
        if (roomCodeInputField != null && !string.IsNullOrEmpty(roomCodeInputField.text.Trim()))
        {
            roomCode = roomCodeInputField.text.Trim();
        }

        savedRoomCode = roomCode;
        PlayerPrefs.SetString("CurrentWorldName", savedRoomCode);
        PlayerPrefs.Save();
        if (string.IsNullOrEmpty(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime))
        {
            Debug.LogWarning("[PHOTON] AppId не найден! Запуск в ОДИНОЧНОМ ОФФЛАЙН режиме.");
            PhotonNetwork.OfflineMode = true;
            PhotonNetwork.CreateRoom(savedRoomCode, new RoomOptions { MaxPlayers = 1 }, TypedLobby.Default);
            return;
        }

        PhotonNetwork.OfflineMode = false; 
        PhotonNetwork.ConnectUsingSettings();
    }

   
    public void JoinRoomByCode()
    {
        if (roomCodeInputField == null || string.IsNullOrEmpty(roomCodeInputField.text.Trim()))
        {
            roomCodeInputField = Object.FindAnyObjectByType<TMP_InputField>();
            
            if (roomCodeInputField == null || string.IsNullOrEmpty(roomCodeInputField.text.Trim()))
            {
                Debug.LogError("[PHOTON] Критическая ошибка: Введи имя мира друга в текстовое поле, чтобы подключиться!");
                return;
            }
        }

        savedRoomCode = roomCodeInputField.text.Trim();
        PlayerPrefs.SetString("CurrentWorldName", savedRoomCode);
        PlayerPrefs.Save();

        if (string.IsNullOrEmpty(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime))
        {
            Debug.LogError("[PHOTON] Ошибка! Нельзя подключиться к другу без AppId в настройках Photon.");
            return;
        }

        PhotonNetwork.OfflineMode = false;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[PHOTON] Успешно подключились к облаку! Заходим в мир: " + savedRoomCode);
        RoomOptions options = new RoomOptions { MaxPlayers = 4, IsVisible = true, IsOpen = true };
        PhotonNetwork.JoinOrCreateRoom(savedRoomCode, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[PHOTON] Успешно зашли в мир! Название: {PhotonNetwork.CurrentRoom.Name}. Игроков внутри: {PhotonNetwork.CurrentRoom.PlayerCount}");
        
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("SampleScene"); 
        }
    }

    private void ShowExistingWorldsInConsole()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "Worlds");
        if (Directory.Exists(folderPath))
        {
            Debug.Log("[Майнкрафт Сохранения] --- СПИСОК ТВОИХ МИРОВ НА ДИСКЕ ---");
            string[] files = Directory.GetFiles(folderPath, "*_seed.txt");
            foreach (string file in files)
            {
                string worldName = Path.GetFileNameWithoutExtension(file).Replace("_seed", "");
                Debug.Log($"• Найден сохранённый мир: [{worldName}]");
            }
        }
    }
}
