using UnityEngine;
using Photon.Pun;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using MyUnityAddons.CustomPhoton;
using MyUnityAddons.Calculations;
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
    [SerializeField] Transform[] defaultSpawnPoints;
    int defaultSpawnIndex = -1;

    RoomSettings roomSettings;

    private void Start()
    {
        if (GameManager.gameManager.offlineMode)
        {
            Destroy(this);
        }
        else
        {
            defaultSpawnIndex++;
            defaultSpawnIndex = Mathf.Clamp(defaultSpawnIndex, 0, defaultSpawnPoints.Length);
            if (teamSpawnPoints.Length == 0 && defaultSpawnPoints.Length > 0)
            {
                teamSpawnPoints = defaultSpawnPoints;
            }

            roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];
            PhotonHashtable playerProperties = new PhotonHashtable
            {
                { "Kills", 0 },
                { "Deaths", 0 }
            };

            PhotonView newPhotonView;
            PhotonTeam playerTeam = PhotonNetwork.LocalPlayer.GetPhotonTeam();
            if ((playerTeam == null || playerTeam.Name != "Spectators") && CustomNetworkHandling.NonSpectatorList.Length < roomSettings.playerLimit)
            {
                GameObject newPlayer;
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
                    default:
                        newPlayer = SpawnPlayer(CustomRandom.GetSpawnPointInCollider(defaultSpawnPoints[defaultSpawnIndex].GetComponent<Collider>(), Vector3.down, ignoreLayers), defaultSpawnPoints[defaultSpawnIndex].rotation);
                        playerProperties["Kills"] = DataManager.playerData.kills;
                        playerProperties["Deaths"] = DataManager.playerData.deaths;
                        break;
                }

                newPhotonView = newPlayer.GetComponent<PhotonView>();
                playerProperties.Add("ViewID", newPhotonView.ViewID);
                if (newPhotonView.IsMine)
                {
                    newPlayer.transform.Find("Camera").gameObject.SetActive(true);
                    newPlayer.transform.Find("Player UI").gameObject.SetActive(true);
                }

                DataManager.playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings", newPlayer.transform);
            }
            else
            {
                GameObject newSpectator = PhotonNetwork.Instantiate(spectatorPrefab.name, Vector3.zero, Quaternion.identity);
                newPhotonView = newSpectator.GetComponent<PhotonView>();
                playerProperties.Add("ViewID", newPhotonView.ViewID);

                DataManager.playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings", newSpectator.transform);
            }

            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
            GameManager.gameManager.UpdatePlayerVariables(newPhotonView);
        }
    }

    private GameObject SpawnPlayer(Vector3 position, Quaternion rotation)
    {
        GameObject newPlayer = PhotonNetwork.Instantiate(playerPrefab.name, position, rotation);
        newPlayer.GetComponent<PhotonView>().RPC("InitializePlayer", RpcTarget.All, new object[] { new float[] { Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1 }, new float[] { Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1 } });
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
            default: // PvE, Co-Op
                int randomSpawnIndex = Random.Range(0, defaultSpawnPoints.Length);
                tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(defaultSpawnPoints[randomSpawnIndex].GetComponent<Collider>(), Vector3.down, ignoreLayers), defaultSpawnPoints[randomSpawnIndex].rotation);
                break;
        }
    }
}