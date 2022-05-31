using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnPlayers : MonoBehaviour
{
    // Prefabs must be in Resources folder
    public GameObject playerPrefab;

    [SerializeField] Collider boundingBox;
    [SerializeField] LayerMask ignoreLayers;

    private void Start()
    {
        PhotonNetwork.Instantiate(playerPrefab.name, RandomExtensions.GetSpawnPointInCollider(boundingBox, Vector3.down, ignoreLayers, playerPrefab.GetComponent<Collider>()), Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up));
    }
}
