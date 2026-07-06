using UnityEngine;
using Photon.Pun;

public class MobBurn : MonoBehaviourPun
{
    public float burnDamage = 2f;
    public float checkInterval = 1f;

    private float timer;
    private MobHealth mobHealth;

    void Start()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            enabled = false;
            return;
        }
        mobHealth = GetComponent<MobHealth>();
    }

    void Update()
    {
        if (DayNightCycle.Instance == null || mobHealth == null) return;

        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;

            if (DayNightCycle.Instance.IsNight == false)
            {
                if (CheckIfUnderCover() == false)
                {
                    Debug.Log(gameObject.name + " горит на солнце!");
                    mobHealth.TakeDamage(burnDamage);
                }
            }
        }
    }

    bool CheckIfUnderCover()
    {
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 1.8f;

        if (Physics.Raycast(rayStart, Vector3.up, out hit, 100f))
        {
            if (hit.transform.GetComponent<Chunk>() != null)
            {
                return true;
            }
        }
        return false;
    }
}
