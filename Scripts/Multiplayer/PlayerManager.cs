using UnityEngine;
using Photon.Pun;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using MyUnityAddons.CustomPhoton;
using MyUnityAddons.Math;
using Photon.Pun.UtilityScripts;

public class PlayerManager : MonoBehaviour
{
    // Prefabs must be in Resources folder
    [SerializeField] Transform playerPrefab;
    [SerializeField] Transform[] teamPlayerPrefabs;

    [SerializeField] Transform spectatorPrefab;

    [SerializeField] Collider boundingBox;
    [SerializeField] LayerMask ignoreLayers;

    [SerializeField] Transform[] teamSpawnPoints;
    [SerializeField] Transform[] PVESpawnPoints;

    RoomSettings roomSettings;

    private void Start()
    {
        roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];
        PhotonHashtable playerProperties = new PhotonHashtable
        {
            { "Kills", 0 },
            { "Deaths", 0 }
        };

        PhotonTeam playerTeam = PhotonNetwork.LocalPlayer.GetPhotonTeam();
        if ((playerTeam == null || playerTeam.Name != "Spectators") && CustomNetworkHandling.NonSpectatorList.Length < roomSettings.playerLimit)
        {
            GameObject newPlayer = null;
            switch (roomSettings.primaryMode)
            {
                case "FFA":
                    newPlayer = SpawnPlayer(CustomRandom.GetSpawnPointInCollider(boundingBox, Vector3.down, ignoreLayers), Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up));
                    break;
                case "Teams":
                    int teamIndex = -1;
                    for (int i = 0; i < teamSpawnPoints.Length; i++)
                    {
                        if (teamSpawnPoints[i].name == playerTeam.Name)
                        {
                            teamIndex = i;
                            break;
                        }
                    }

                    Vector3 spawnPosition = CustomRandom.GetSpawnPointInCollider(teamSpawnPoints[teamIndex].GetComponent<Collider>(), Vector3.down, ignoreLayers);

                    newPlayer = PhotonNetwork.Instantiate(teamPlayerPrefabs[teamIndex].name, spawnPosition, teamSpawnPoints[teamIndex].rotation);
                    newPlayer.name = teamPlayerPrefabs[teamIndex].name;
                    break;
                case "PvE":
                    int PVESpawnIndex = Random.Range(0, PVESpawnPoints.Length + 1);
                    newPlayer = SpawnPlayer(CustomRandom.GetSpawnPointInCollider(PVESpawnPoints[PVESpawnIndex].GetComponent<Collider>(), Vector3.down, ignoreLayers), PVESpawnPoints[PVESpawnIndex].rotation);
                    break;
            }

            playerProperties.Add("ViewID", newPlayer.GetComponent<PhotonView>().ViewID);
        }
        else
        {
            GameObject newSpectator = PhotonNetwork.Instantiate(spectatorPrefab.name, Vector3.zero, Quaternion.identity);
            playerProperties.Add("ViewID", newSpectator.GetComponent<PhotonView>().ViewID);
        }
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
    }

    private GameObject SpawnPlayer(Vector3 position, Quaternion rotation)
    {
        GameObject newPlayer = PhotonNetwork.Instantiate(playerPrefab.name, position, rotation);
        newPlayer.GetComponent<PhotonView>().RPC("RandomizeMaterialColors", RpcTarget.All);
        newPlayer.name = playerPrefab.name;

        return newPlayer;
    }

    public void RespawnPlayer(Transform tankOrigin)
    {
        PhotonTeam playerTeam = PhotonNetwork.LocalPlayer.GetPhotonTeam();

        switch (roomSettings.primaryMode)
        {
            case "FFA":
                tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(boundingBox, Vector3.down, ignoreLayers), Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up));
                break;
            case "Teams":
                int teamSpawnIndex = -1;
                for (int i = 0; i < teamSpawnPoints.Length; i++)
                {
                    if (teamSpawnPoints[i].name == playerTeam.Name)
                    {
                        teamSpawnIndex = i;
                        break;
                    }
                }

                tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(teamSpawnPoints[teamSpawnIndex].GetComponent<Collider>(), Vector3.down, ignoreLayers), teamSpawnPoints[teamSpawnIndex].rotation);
                break;
            case "PvE":
                int PVESpawnIndex = Random.Range(0, PVESpawnPoints.Length + 1);
                tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(teamSpawnPoints[PVESpawnIndex].GetComponent<Collider>(), Vector3.down, ignoreLayers), teamSpawnPoints[PVESpawnIndex].rotation);
                break;
        }
    }
}