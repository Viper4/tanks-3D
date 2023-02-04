using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Calculations;
using Photon.Pun;
using System.Linq;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class BoostGenerator : MonoBehaviourPunCallbacks
{
    public static BoostGenerator Instance;

    [SerializeField] Collider spawnCollider;
    [SerializeField] List<GameObject> boosts = new List<GameObject>();
    [SerializeField] int boostLimit = 1;
    [SerializeField] LayerMask ignoreLayers;
    List<GameObject> spawnedBoosts = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

        if (!GameManager.Instance.editing)
            Init();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        if (!GameManager.Instance.inLobby)
        {
            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (!GameManager.Instance.inLobby)
        {
            PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
        }
    }

    void OnEvent(EventData eventData)
    {
        if(eventData.Code == EventCodes.SpawnNewBoost)
        {
            SpawnNewBoost();
        }
    }

    public void Init()
    {
        if (DataManager.roomSettings.mode == "FFA" || DataManager.roomSettings.mode == "Teams")
        {
            if (!PhotonNetwork.OfflineMode)
            {
                foreach (GameObject boost in boosts.ToList())
                {
                    if (!DataManager.roomSettings.boosts.Contains(boost.name))
                    {
                        boosts.Remove(boost);
                    }
                }
            }

            if (PhotonNetwork.IsMasterClient)
            {
                boostLimit = DataManager.roomSettings.boostLimit;
                if (boostLimit > 0)
                    SpawnBoosts();
            }
        }
    }

    public void SpawnNewBoost()
    {
        GameObject newBoost = boosts[Random.Range(0, boosts.Count)];
        if(PhotonNetwork.OfflineMode)
        {
            spawnedBoosts.Add(Instantiate(newBoost, CustomRandom.GetSpawnPointInCollider(spawnCollider, Vector3.down, ignoreLayers, newBoost.GetComponent<BoxCollider>(), newBoost.transform.rotation, true), Quaternion.AngleAxis(Random.Range(-180.0f, 180.0f), Vector3.up), transform));
        }
        else
        {
            if(PhotonNetwork.IsMasterClient)
            {
                Boost boostScript = newBoost.GetComponent<Boost>();
                spawnedBoosts.Add(PhotonNetwork.InstantiateRoomObject(newBoost.name, CustomRandom.GetSpawnPointInCollider(spawnCollider, Vector3.down, ignoreLayers, newBoost.GetComponent<BoxCollider>(), newBoost.transform.rotation, true), Quaternion.AngleAxis(Random.Range(-180.0f, 180.0f), Vector3.up), 0, new object[] { Random.Range(boostScript.duration[0], boostScript.duration[1]) }));
            }
            else
            {
                PhotonNetwork.RaiseEvent(EventCodes.SpawnNewBoost, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
            }
        }
    }

    void SpawnBoosts()
    {
        for(int i = 0; i < boostLimit; i++)
        {
            SpawnNewBoost();
        }
    }

    public void ResetBoosts()
    {
        if (PhotonNetwork.OfflineMode)
        {
            foreach (GameObject boost in spawnedBoosts)
            {
                Destroy(boost);
            }
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            foreach (GameObject boost in spawnedBoosts)
            {
                PhotonNetwork.Destroy(boost);
            }
        }
    }
}
