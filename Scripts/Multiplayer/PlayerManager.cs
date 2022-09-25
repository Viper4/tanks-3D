using UnityEngine;
using Photon.Pun;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using MyUnityAddons.CustomPhoton;
using MyUnityAddons.Calculations;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using ExitGames.Client.Photon;
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

    [SerializeField] Collider freeForAllSpawn;
    [SerializeField] Transform teamSpawnParent;
    [SerializeField] Transform defaultSpawnParent;
    List<Collider> teamSpawns = new List<Collider>();
    List<Collider> defaultSpawns = new List<Collider>();
    [SerializeField] LayerMask ignoreLayerMask;

    RoomSettings roomSettings;

    private void Start()
    {
        if (PhotonNetwork.OfflineMode)
        {
            Destroy(this);
        }
        else
        {
            BoxCollider playerSpawnCollider = playerPrefab.Find("Tank Origin").Find("Body").GetComponent<BoxCollider>();

            Instance = this;
            foreach (Transform child in teamSpawnParent)
            {
                teamSpawns.Add(child.GetComponent<Collider>());
            }
            foreach (Transform child in defaultSpawnParent)
            {
                defaultSpawns.Add(child.GetComponent<Collider>());
            }

            int spawnIndex = Random.Range(0, defaultSpawns.Count);

            if (teamSpawns.Count == 0 && defaultSpawns.Count > 0)
            {
                teamSpawns = defaultSpawns;
            }

            roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];
            PhotonTeam playerTeam = PhotonNetwork.LocalPlayer.GetPhotonTeam();

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
                    GameManager.Instance.frozen = false;
                    Time.timeScale = 1;
                    GameManager.Instance.loadingScreen.gameObject.SetActive(false);
                    DataManager.playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings");
                    GameManager.Instance.UpdatePlayerWithSettings(SpawnSpectator(Vector3.zero, Quaternion.identity));
                }
                else
                {
                    newPlayer = SpawnPlayer(CustomRandom.GetSpawnPointInCollider(defaultSpawns[spawnIndex], Vector3.down, ignoreLayerMask, playerSpawnCollider, defaultSpawns[spawnIndex].transform.rotation), defaultSpawns[spawnIndex].transform.rotation);
                    playerProperties["Kills"] = DataManager.playerData.kills;
                    playerProperties["Deaths"] = DataManager.playerData.deaths;
                }
            }
            else
            {
                if ((playerTeam != null && playerTeam.Name == "Spectators") || CustomNetworkHandling.NonSpectatorList.Length > roomSettings.playerLimit)
                {
                    GameManager.Instance.frozen = false;
                    GameManager.Instance.loadingScreen.gameObject.SetActive(false);
                    DataManager.playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings");
                    GameManager.Instance.UpdatePlayerWithSettings(SpawnSpectator(Vector3.zero, Quaternion.identity));
                }
                else
                {
                    switch (roomSettings.primaryMode)
                    {
                        case "FFA":
                            Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up);
                            newPlayer = SpawnPlayer(CustomRandom.GetSpawnPointInCollider(freeForAllSpawn, Vector3.down, ignoreLayerMask, playerSpawnCollider, randomRotation), randomRotation);
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

                            Vector3 spawnPosition = CustomRandom.GetSpawnPointInCollider(teamSpawns[teamIndex], Vector3.down, ignoreLayerMask, playerSpawnCollider, teamSpawns[teamIndex].transform.rotation);

                            newPlayer = PhotonNetwork.Instantiate(teamPlayerPrefabs[teamIndex].name, spawnPosition, teamSpawns[teamIndex].transform.rotation);
                            newPlayer.name = teamPlayerPrefabs[teamIndex].name;
                            break;
                        default:
                            newPlayer = SpawnPlayer(CustomRandom.GetSpawnPointInCollider(defaultSpawns[spawnIndex], Vector3.down, ignoreLayerMask, playerSpawnCollider, defaultSpawns[spawnIndex].transform.rotation), defaultSpawns[spawnIndex].transform.rotation);
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

    private Transform SpawnSpectator(Vector3 position, Quaternion rotation)
    {
        Transform newSpectator = Instantiate(spectatorPrefab, position, rotation);
        newSpectator.name = spectatorPrefab.name;

        return newSpectator;
    }

    private GameObject SpawnPlayer(Vector3 position, Quaternion rotation)
    {
        Debug.Log("Spawned player");
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
                    tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(freeForAllSpawn, Vector3.down, ignoreLayerMask), Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up));
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

                    tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(teamSpawns[teamSpawnIndex], Vector3.down, ignoreLayerMask), teamSpawns[teamSpawnIndex].transform.rotation);
                    break;
                default: // PvE
                    int randomSpawnIndex = Random.Range(0, defaultSpawns.Count);
                    tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(defaultSpawns[randomSpawnIndex], Vector3.down, ignoreLayerMask), defaultSpawns[randomSpawnIndex].transform.rotation);
                    break;
            }

            PhotonView PV = tankOrigin.parent.GetComponent<PhotonView>();

            PV.RPC("ReactivatePlayer", RpcTarget.All);
            PV.RPC("ResetTrails", RpcTarget.All);
        }
        else
        {
            PhotonNetwork.LocalPlayer.JoinOrSwitchTeam("Spectators");
            Transform camera = tankOrigin.parent.Find("Camera");
            SpawnSpectator(camera.position, camera.rotation);
            PhotonNetwork.Destroy(tankOrigin.parent.gameObject);
        }
    }

    public void StopCoroutines()
    {
        StopAllCoroutines();
    }

    public void OnPlayerDeath(Transform tankOrigin, float respawnDelay = 3)
    {
        if (roomSettings.primaryMode == "Co-Op")
        {
            tankOrigin.parent.SetParent(null);

            GameManager.Instance.totalLives--;
            if (playerParent.childCount < 1)
            {
                GameManager.Instance.frozen = true;
                PhotonNetwork.LocalPlayer.JoinOrSwitchTeam("Players");
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
        Debug.Log("Restart CoOp Game");
        PhotonHashtable roomProperties = new PhotonHashtable()
        {
            { "Total Lives", GameManager.Instance.totalLives }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        if (GameManager.Instance.totalLives > 0)
        {
            GameManager.Instance.PhotonLoadScene(-1, 3, true, false);
        }
        else
        {
            GameManager.Instance.PhotonLoadScene("End Scene", 3, true, false);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        StartCoroutine(DelayedOnPlayerLeftCheck());
    }

    IEnumerator DelayedOnPlayerLeftCheck()
    {
        yield return new WaitForEndOfFrame();
        if (roomSettings.primaryMode == "Co-Op" && transform.childCount < 1)
        {
            Debug.Log("Here");
            GameManager.Instance.frozen = true;
            PhotonNetwork.LocalPlayer.JoinOrSwitchTeam("Players");

            if (PhotonNetwork.IsMasterClient)
            {
                RestartCoOpGame();
            }
        }
    }
}