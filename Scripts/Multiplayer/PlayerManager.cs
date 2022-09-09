using UnityEngine;
using Photon.Pun;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using MyUnityAddons.CustomPhoton;
using MyUnityAddons.Calculations;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections;

public class PlayerManager : MonoBehaviourPunCallbacks
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
        if (GameManager.Instance.offlineMode)
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
            PhotonTeam playerTeam = PhotonNetwork.LocalPlayer.GetPhotonTeam();

            if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("New"))
            {
                Debug.Log(playerTeam + " " + ((bool)PhotonNetwork.LocalPlayer.CustomProperties["New"]).ToString());
            }

            PhotonView newPhotonView;
            GameObject newPlayer = null;
            PhotonHashtable playerProperties = new PhotonHashtable
            {
                { "Kills", 0 },
                { "Deaths", 0 },
                { "New", false }
            };

            if (roomSettings.primaryMode == "Co-Op")
            {
                if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Started") && (bool)PhotonNetwork.CurrentRoom.CustomProperties["Started"] && PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("New") && (bool)PhotonNetwork.LocalPlayer.CustomProperties["New"])
                {
                    GameManager.frozen = false;
                    Time.timeScale = 1;
                    GameManager.Instance.loadingScreen.gameObject.SetActive(false);
                    DataManager.playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings", SpawnSpectator(Vector3.zero, Quaternion.identity));
                }
                else
                {
                    newPlayer = SpawnPlayer(CustomRandom.GetSpawnPointInCollider(defaultSpawnPoints[defaultSpawnIndex].GetComponent<Collider>(), Vector3.down, ignoreLayers), defaultSpawnPoints[defaultSpawnIndex].rotation);
                    playerProperties["Kills"] = DataManager.playerData.kills;
                    playerProperties["Deaths"] = DataManager.playerData.deaths;
                }
            }
            else
            {
                if ((playerTeam != null && playerTeam.Name == "Spectators") || CustomNetworkHandling.NonSpectatorList.Length > roomSettings.playerLimit)
                {
                    GameManager.frozen = false;
                    GameManager.Instance.loadingScreen.gameObject.SetActive(false);
                    DataManager.playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings", SpawnSpectator(Vector3.zero, Quaternion.identity));
                }
                else
                {
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
                }
            }

            if (newPlayer != null)
            {
                newPhotonView = newPlayer.GetComponent<PhotonView>();
                playerProperties.Add("ViewID", newPhotonView.ViewID);
                GameManager.Instance.UpdatePlayerVariables(newPhotonView);
                DataManager.playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings", newPlayer.transform);
            }
            else
            {
                DataManager.playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings", null);
            }

            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
    }

    private Transform SpawnSpectator(Vector3 position, Quaternion rotation)
    {
        Transform newSpectator = Instantiate(spectatorPrefab, position, rotation);
        newSpectator.name = spectatorPrefab.name;

        return newSpectator;
    }

    private GameObject SpawnPlayer(Vector3 position, Quaternion rotation)
    {
        GameObject newPlayer = PhotonNetwork.Instantiate(playerPrefab.name, position, rotation);
        newPlayer.GetComponent<PhotonView>().RPC("InitializePlayer", RpcTarget.All, new object[] { new float[] { Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1 }, new float[] { Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1 } });
        newPlayer.name = playerPrefab.name;
        newPlayer.transform.Find("Camera").gameObject.SetActive(true);
        newPlayer.transform.Find("Player UI").gameObject.SetActive(true);

        return newPlayer;
    }

    IEnumerator RespawnPlayerRoutine(Transform tankOrigin, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        PhotonTeam playerTeam = PhotonNetwork.LocalPlayer.GetPhotonTeam();
        if (roomSettings.primaryMode != "Co-Op")
        {
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
                default: // PvE
                    int randomSpawnIndex = Random.Range(0, defaultSpawnPoints.Length);
                    tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(defaultSpawnPoints[randomSpawnIndex].GetComponent<Collider>(), Vector3.down, ignoreLayers), defaultSpawnPoints[randomSpawnIndex].rotation);
                    break;
            }

            tankOrigin.parent.GetComponent<PlayerControl>().Dead = false;
            tankOrigin.GetComponent<CapsuleCollider>().enabled = true;

            tankOrigin.Find("Body").gameObject.SetActive(true);
            tankOrigin.Find("Turret").gameObject.SetActive(true);
            tankOrigin.Find("Barrel").gameObject.SetActive(true);

            tankOrigin.Find("TrackMarks").GetComponent<PhotonView>().RPC("ResetTrails", RpcTarget.All);
        }
        else
        {
            PhotonNetwork.LocalPlayer.JoinOrSwitchTeam("Spectators");
            Transform camera = tankOrigin.parent.Find("Camera");
            Debug.Log("LOOK HERE LOOK HERE: " + camera.name + ": " + camera.rotation);
            SpawnSpectator(camera.position, camera.rotation);
            PhotonNetwork.Destroy(tankOrigin.parent.gameObject);
        }
    }

    public void OnPlayerDeath(Transform tankOrigin, float respawnDelay = 3)
    {
        if (roomSettings.primaryMode == "Co-Op" && transform.childCount < 1)
        {
            RestartCoOpGame();
        }
        else
        {
            StartCoroutine(RespawnPlayerRoutine(tankOrigin, respawnDelay));
        }
    }

    void RestartCoOpGame()
    {
        GameManager.frozen = true;
        if ((int)PhotonNetwork.CurrentRoom.CustomProperties["Total Lives"] > 0)
        {
            GameManager.Instance.PhotonLoadScene(-1, 3, true, false);
            PhotonHashtable parameters = new PhotonHashtable
            {
                { "sceneIndex", -1 },
                { "delay", 3 },
                { "save", true },
                { "waitWhilePaused", false }
            };
            PhotonNetwork.RaiseEvent(GameManager.Instance.LoadSceneEventCode, parameters, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        }
        else
        {
            GameManager.Instance.PhotonLoadScene("End Scene", 3, true, false);
            PhotonHashtable parameters = new PhotonHashtable
            {
                { "sceneName", "End Scene" },
                { "delay", 3 },
                { "save", true },
                { "waitWhilePaused", false }
            };
            PhotonNetwork.RaiseEvent(GameManager.Instance.LoadSceneEventCode, parameters, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (roomSettings.primaryMode == "Co-Op" && PhotonNetwork.IsMasterClient && transform.childCount <= 1)
        {
            RestartCoOpGame();
        }
    }
}