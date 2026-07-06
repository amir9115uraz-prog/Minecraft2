using UnityEngine;
using Photon.Pun;

public class DayNightCycle : MonoBehaviourPun
{
    public static DayNightCycle Instance;

    [Header("Настройки времени")]
    public float dayLengthInMinutes = 20f; 
    
    [Range(0f, 1f)]
    public float timeOfDay = 0.25f; 

    [Header("Световые настройки")]
    public Gradient lightColor;
    public Gradient ambientColor;

    public bool IsNight => timeOfDay < 0.2f || timeOfDay > 0.8f;

    private Light sunLight;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        sunLight = GetComponent<Light>();
        
        if (sunLight == null)
        {
            Debug.LogWarning("[DayNightCycle] На этом объекте нет компонента Light! Пожалуйста, добавь его.");
        }
    }

    void Update()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.OfflineMode)
        {
            UpdateTimeLogic();
            return;
        }

        if (!PhotonNetwork.IsMasterClient) return;

        UpdateTimeLogic();

        if (photonView != null)
        {
            photonView.RPC("SyncTime", RpcTarget.Others, timeOfDay);
        }
    }

    void UpdateTimeLogic()
    {
        float timeScale = 1f / (dayLengthInMinutes * 60f);
        timeOfDay += Time.deltaTime * timeScale;

        if (timeOfDay >= 1f) timeOfDay = 0f;

        UpdateWeather();
    }

    [PunRPC]
    void SyncTime(float serverTime)
    {
        timeOfDay = serverTime;
        UpdateWeather();
    }

    void UpdateWeather()
    {
        transform.localRotation = Quaternion.Euler((timeOfDay * 360f) - 90f, 170f, 0f);

        if (sunLight != null)
        {
            if (lightColor != null) sunLight.color = lightColor.Evaluate(timeOfDay);
        }
    }
}
