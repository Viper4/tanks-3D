using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Calculations;
using Photon.Pun;
using System.Linq;

public class BoostGenerator : MonoBehaviour
{
    public static BoostGenerator Instance;

    [SerializeField] Collider spawnCollider;
    [SerializeField] List<GameObject> boosts = new List<GameObject>();
    [SerializeField] int boostLimit = 1;
    [SerializeField] LayerMask ignoreLayers;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

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
            SpawnBoosts();
        }
    }

    public void SpawnNewBoost()
    {
        GameObject newBoost = boosts[Random.Range(0, boosts.Count)];
        if (PhotonNetwork.OfflineMode)
        {
            Instantiate(newBoost, CustomRandom.GetSpawnPointInCollider(spawnCollider, Vector3.down, ignoreLayers, newBoost.GetComponent<BoxCollider>(), newBoost.transform.rotation, true), Quaternion.AngleAxis(Random.Range(-180.0f, 180.0f), Vector3.up), transform);
        }
        else
        {
            Boost boostScript = newBoost.GetComponent<Boost>();
            PhotonNetwork.InstantiateRoomObject(newBoost.name, CustomRandom.GetSpawnPointInCollider(spawnCollider, Vector3.down, ignoreLayers, newBoost.GetComponent<BoxCollider>(), newBoost.transform.rotation, true), Quaternion.AngleAxis(Random.Range(-180.0f, 180.0f), Vector3.up), 0, new object[] { Random.Range(boostScript.duration[0], boostScript.duration[1]) });
        }
    }

    void SpawnBoosts()
    {
        for (int i = 0; i < boostLimit; i++)
        {
            SpawnNewBoost();
        }
    }
}
