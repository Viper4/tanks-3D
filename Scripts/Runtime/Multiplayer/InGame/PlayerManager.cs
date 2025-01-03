using UnityEngine;
using Photon.Pun;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using MyUnityAddons.CustomPhoton;
using MyUnityAddons.Calculations;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    public static PlayerManager Instance;

    // Prefabs must be in Resources folder
    public Transform playerParent;
    [SerializeField] Transform playerPrefab;
    [SerializeField] Transform[] teamPlayerPrefabs;
    [SerializeField] Transform spectatorPrefab;

    public Transform defaultSpawnParent;
    public Transform teamSpawnParent;
    List<Collider> defaultSpawns = new List<Collider>();
    List<Collider> teamSpawns = new List<Collider>();
    [SerializeField] LayerMask ignoreLayerMask;

    [SerializeField] bool autoInit = true;

    BoxCollider playerSpawnCollider;

    int deadPlayers = 0;

    private void Start()
    {
        Instance = this;
        playerSpawnCollider = playerPrefab.Find("Tank Origin").Find("Body").GetComponent<BoxCollider>();

        if (autoInit)
            Init();
    }

    public void Init()
    {
        foreach (Transform child in defaultSpawnParent)
        {
            defaultSpawns.Add(child.GetComponent<Collider>());
        }
        foreach (Transform child in teamSpawnParent)
        {
            teamSpawns.Add(child.GetComponent<Collider>());
        }

        if (!GameManager.Instance.editing && !PhotonNetwork.OfflineMode)
        {
            PhotonTeam playerTeam = PhotonNetwork.LocalPlayer.GetPhotonTeam();

            BoxCollider playerSpawnCollider = playerPrefab.Find("Tank Origin").Find("Body").GetComponent<BoxCollider>();
            Collider spawn = defaultSpawns[Random.Range(0, defaultSpawns.Count)];

            PhotonView newPhotonView;
            GameObject newPlayer = null;
            PhotonHashtable playerProperties = new PhotonHashtable
            {
                { "kills", 0 },
                { "deaths", 0 },
                { "new", false },
            };

            if (DataManager.roomSettings.mode == "Co-Op")
            {
                if ((playerTeam != null && playerTeam.Name == "Spectators") || (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("started") && (bool)PhotonNetwork.CurrentRoom.CustomProperties["started"] && PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("new") && (bool)PhotonNetwork.LocalPlayer.CustomProperties["new"]))
                {
                    Time.timeScale = 1;
                    GameManager.Instance.loadingScreen.gameObject.SetActive(false);
                    DataManager.playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings");
                    GameManager.Instance.UpdatePlayerWithSettings(SpawnSpectator(new Vector3(0, 6, 0), Quaternion.identity));
                }
                else
                {
                    PhotonNetwork.LocalPlayer.JoinOrSwitchTeam("Players");
                    newPlayer = SpawnPlayer(playerPrefab, CustomRandom.GetSpawnPointInCollider(spawn, Vector3.down, ignoreLayerMask, playerSpawnCollider, spawn.transform.rotation), spawn.transform.rotation);
                    playerProperties["kills"] = DataManager.playerData.kills;
                    playerProperties["deaths"] = DataManager.playerData.deaths;
                }
            }
            else
            {
                if ((playerTeam != null && playerTeam.Name == "Spectators") || CustomNetworkHandling.NonSpectatorList.Length > DataManager.roomSettings.playerLimit)
                {
                    GameManager.Instance.loadingScreen.gameObject.SetActive(false);
                    DataManager.playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings");
                    GameManager.Instance.UpdatePlayerWithSettings(SpawnSpectator(new Vector3(0, 6, 0), Quaternion.identity));
                }
                else
                {
                    switch (DataManager.roomSettings.mode)
                    {
                        case "FFA":
                            Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up);
                            newPlayer = SpawnPlayer(playerPrefab, CustomRandom.GetSpawnPointInCollider(spawn, Vector3.down, ignoreLayerMask, playerSpawnCollider, randomRotation), randomRotation);
                            break;
                        case "Teams":
                            int teamIndex = -1;
                            for (int i = 0; i < teamSpawns.Count; i++)
                            {
                                if (teamSpawns[i].name == playerTeam.Name)
                                {
                                    teamIndex = i;
                                    break;
                                }
                            }

                            newPlayer = SpawnPlayer(teamPlayerPrefabs[teamIndex], CustomRandom.GetSpawnPointInCollider(teamSpawns[teamIndex], Vector3.down, ignoreLayerMask, playerSpawnCollider, teamSpawns[teamIndex].transform.rotation), teamSpawns[teamIndex].transform.rotation, false);
                            break;
                        default:
                            newPlayer = SpawnPlayer(playerPrefab, CustomRandom.GetSpawnPointInCollider(spawn, Vector3.down, ignoreLayerMask, playerSpawnCollider, spawn.transform.rotation), spawn.transform.rotation);
                            playerProperties["kills"] = DataManager.playerData.kills;
                            playerProperties["deaths"] = DataManager.playerData.deaths;
                            break;
                    }
                }
            }

            if (newPlayer != null)
            {
                newPhotonView = newPlayer.GetComponent<PhotonView>();
                playerProperties.Add("ViewID", newPhotonView.ViewID);
                GameManager.Instance.UpdatePlayerVariables(newPhotonView);
                DataManager.playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings");
                GameManager.Instance.UpdatePlayerWithSettings(newPlayer.transform);
            }
            else
            {
                DataManager.playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings");
            }

            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
    }

    public GameObject SpawnInLocalPlayer(PhotonTeam team)
    {
        Collider spawn = null;
        if (defaultSpawns.Count > 0)
        {
            spawn = defaultSpawns[Random.Range(0, defaultSpawns.Count)];
        }

        PhotonView newPhotonView;
        GameObject newPlayer;
        PhotonHashtable playerProperties = new PhotonHashtable
        {
            { "kills", 0 },
            { "deaths", 0 },
            { "new", false },
        };
        switch (DataManager.roomSettings.mode)
        {
            case "FFA":
                Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up);
                newPlayer = SpawnPlayer(playerPrefab, CustomRandom.GetSpawnPointInCollider(spawn, Vector3.down, ignoreLayerMask, playerSpawnCollider, randomRotation), randomRotation);
                break;
            case "Teams":
                int teamIndex = -1;
                for (int i = 0; i < teamSpawns.Count; i++)
                {
                    if (teamSpawns[i].name == team.Name)
                    {
                        teamIndex = i;
                        break;
                    }
                }

                newPlayer = SpawnPlayer(teamPlayerPrefabs[teamIndex], CustomRandom.GetSpawnPointInCollider(teamSpawns[teamIndex], Vector3.down, ignoreLayerMask, playerSpawnCollider, teamSpawns[teamIndex].transform.rotation), teamSpawns[teamIndex].transform.rotation, false);
                break;
            default:
                newPlayer = SpawnPlayer(playerPrefab, CustomRandom.GetSpawnPointInCollider(spawn, Vector3.down, ignoreLayerMask, playerSpawnCollider, spawn.transform.rotation), spawn.transform.rotation);
                playerProperties["kills"] = DataManager.playerData.kills;
                playerProperties["deaths"] = DataManager.playerData.deaths;
                break;
        }

        if (newPlayer != null)
        {
            newPhotonView = newPlayer.GetComponent<PhotonView>();
            playerProperties.Add("ViewID", newPhotonView.ViewID);
            GameManager.Instance.UpdatePlayerVariables(newPhotonView);
            DataManager.playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings");
            GameManager.Instance.UpdatePlayerWithSettings(newPlayer.transform);
        }

        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        return newPlayer;
    }

    private Transform SpawnSpectator(Vector3 position, Quaternion rotation)
    {
        PhotonHashtable playerProperties = new PhotonHashtable()
        {
            { "spectator", true }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        Transform newSpectator = Instantiate(spectatorPrefab, position, rotation);
        newSpectator.name = spectatorPrefab.name;

        return newSpectator;
    }

    private GameObject SpawnPlayer(Transform prefab, Vector3 position, Quaternion rotation, bool randomColors = true)
    {
        PhotonHashtable playerProperties = new PhotonHashtable()
        {
            { "spectator", false }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        GameObject newPlayer;
        if (PhotonNetwork.OfflineMode)
        {
            newPlayer = Instantiate(prefab, position, rotation).gameObject;
            newPlayer.transform.SetParent(playerParent);
        }
        else
        {
            newPlayer = PhotonNetwork.Instantiate(prefab.name, position, rotation);
            newPlayer.GetComponent<PhotonView>().RPC("InitializePlayer", RpcTarget.All, new object[] { randomColors, new float[] { Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1 }, new float[] { Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1 } });
        }
        newPlayer.name = prefab.name;
        newPlayer.transform.Find("Camera").gameObject.SetActive(true);
        newPlayer.transform.Find("Player UI").gameObject.SetActive(true);

        return newPlayer;
    }

    public void RespawnAsSpectator(Transform player)
    {
        if (player.GetComponent<PhotonView>() != null)
        {
            Transform camera = player.Find("Camera");
            SpawnSpectator(camera.position, camera.rotation);
            PhotonNetwork.Destroy(player.gameObject);
        }
    }

    IEnumerator RespawnPlayerRoutine(Transform tankOrigin, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        PhotonTeam playerTeam = PhotonNetwork.LocalPlayer.GetPhotonTeam();
        if (DataManager.roomSettings.mode != "Co-Op")
        {
            if (playerTeam.Name == "Spectators")
            {
                RespawnAsSpectator(tankOrigin.parent);
            }
            else
            {
                Collider spawn = defaultSpawns[Random.Range(0, defaultSpawns.Count)];
                switch (DataManager.roomSettings.mode)
                {
                    case "FFA":
                        tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(spawn, Vector3.down, ignoreLayerMask, playerSpawnCollider), Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up));
                        break;
                    case "Teams":
                        int teamSpawnIndex = -1;
                        for (int i = 0; i < teamSpawns.Count; i++)
                        {
                            if (teamSpawns[i].name == playerTeam.Name)
                            {
                                teamSpawnIndex = i;
                                break;
                            }
                        }

                        tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(teamSpawns[teamSpawnIndex], Vector3.down, ignoreLayerMask, playerSpawnCollider), teamSpawns[teamSpawnIndex].transform.rotation);
                        break;
                    default:
                        tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(spawn, Vector3.down, ignoreLayerMask, playerSpawnCollider), spawn.transform.rotation);
                        break;
                }
                PhotonView PV = tankOrigin.parent.GetComponent<PhotonView>();

                PV.RPC("ReactivatePlayer", RpcTarget.All);
                PV.RPC("ResetTrails", RpcTarget.All);
            }
        }
        else
        {
            RespawnAsSpectator(tankOrigin.parent);
        }
    }

    public void StopCoroutines()
    {
        StopAllCoroutines();
    }

    public void OnPlayerDeath(Transform tankOrigin, float respawnDelay = 3)
    {
        if (DataManager.roomSettings.mode == "Co-Op")
        {
            deadPlayers++;
            tankOrigin.parent.SetParent(null);
            GameManager.Instance.totalLives--;
            if (playerParent.childCount < 1)
            {
                GameManager.Instance.frozen = true;
                StopCoroutines();

                if (PhotonNetwork.IsMasterClient)
                {
                    RestartCoOpGame();
                }
            }
            else if (tankOrigin.parent.GetComponent<PhotonView>().IsMine)
            {
                StartCoroutine(RespawnPlayerRoutine(tankOrigin, respawnDelay));
            }
        }
        else if (tankOrigin.parent.GetComponent<PhotonView>().IsMine)
        {
            StartCoroutine(RespawnPlayerRoutine(tankOrigin, respawnDelay));
        }
    }

    void RestartCoOpGame()
    {
        if (deadPlayers >= CustomNetworkHandling.SpectatorList.Length + CustomNetworkHandling.NonSpectatorList.Length && DataManager.roomSettings.resetIfAllDie)
        {
            GameManager.Instance.totalLives = 0;
            PhotonHashtable roomProperties = new PhotonHashtable()
            {
                { "totalLives", 0 }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
            GameManager.Instance.PhotonLoadScene("End Scene", 3, true);
        }
        else
        {
            PhotonHashtable roomProperties = new PhotonHashtable()
            {
                { "totalLives", GameManager.Instance.totalLives }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
            if (GameManager.Instance.totalLives > 0 && CustomNetworkHandling.NonSpectatorList.Length > 0)
            {
                GameManager.Instance.PhotonLoadScene(-1, 3, true);
            }
            else
            {
                GameManager.Instance.PhotonLoadScene("End Scene", 3, true);
            }
        }
        deadPlayers = 0;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        StartCoroutine(DelayedOnPlayerLeftCheck());
    }

    IEnumerator DelayedOnPlayerLeftCheck()
    {
        yield return new WaitForEndOfFrame();
        if (DataManager.roomSettings.mode == "Co-Op" && playerParent.childCount < 1)
        {
            GameManager.Instance.frozen = true;

            if (PhotonNetwork.IsMasterClient)
            {
                RestartCoOpGame();
            }
        }
    }
}