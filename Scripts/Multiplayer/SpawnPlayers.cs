using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnPlayers : MonoBehaviour
{
    // Prefabs must be in Resources folder
    public GameObject playerPrefab;
    public GameObject spectatorPrefab;

    public int playerLimit = 8;
    int playerAmount = 0;
    int spectatorAmount = 0;

    [SerializeField] Collider boundingBox;
    [SerializeField] LayerMask ignoreLayers;

    private void Start()
    {
        if (playerAmount < playerLimit)
        {
            playerAmount++;
            GameObject newPlayer = PhotonNetwork.Instantiate(playerPrefab.name, RandomExtensions.GetSpawnPointInCollider(boundingBox, Vector3.down, ignoreLayers), Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up));
            Color primaryColor = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
            Color secondaryColor = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));

            // Randomizing color of materials on player 
            Transform tankOrigin = newPlayer.transform.Find("Tank Origin");
            MeshRenderer bodyRenderer = tankOrigin.Find("Body").GetComponent<MeshRenderer>();
            MeshRenderer turretRenderer = tankOrigin.Find("Turret").GetComponent<MeshRenderer>();
            MeshRenderer barrelRenderer = tankOrigin.Find("Barrel").GetComponent<MeshRenderer>();
            bodyRenderer.materials[2].color = primaryColor;
            bodyRenderer.materials[0].color = secondaryColor;

            turretRenderer.materials[0].color = secondaryColor;

            barrelRenderer.materials[1].color = primaryColor;
            barrelRenderer.materials[0].color = secondaryColor;
        }
        else
        {
            spectatorAmount++;
            PhotonNetwork.Instantiate(spectatorPrefab.name, Vector3.zero, Quaternion.identity);
        }
    }

    public void RespawnPlayer(Transform tankOrigin)
    {
        tankOrigin.SetPositionAndRotation(RandomExtensions.GetSpawnPointInCollider(boundingBox, Vector3.down, ignoreLayers), Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up));
    }
}