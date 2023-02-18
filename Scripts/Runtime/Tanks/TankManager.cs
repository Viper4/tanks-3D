using MyUnityAddons.Calculations;
using MyUnityAddons.CustomPhoton;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class TankManager : MonoBehaviourPunCallbacks
{
    public static TankManager Instance;

    public bool lastCampaignScene = false;
    public Transform tankParent;
    Dictionary<Transform, SaveableLevelObject.TransformInfo> resetInfoDictionary = new Dictionary<Transform, SaveableLevelObject.TransformInfo>();

    [SerializeField] int tankLimit = 12;
    [SerializeField] float deviateChance;
    [SerializeField] int amountDeviationMin;
    [SerializeField] int amountDeviationMax;
    [SerializeField] List<GameObject> tanks;
    List<GameObject> spawnedTanks = new List<GameObject>();
    public Transform spawnParent;
    List<Collider> spawns = new List<Collider>();
    [SerializeField] LayerMask ignoreLayerMask;
    bool checking = false;

    int teamIndex = 0;

    [SerializeField] bool autoInit = true;

    public Dictionary<GameObject, int> tankIndices = new Dictionary<GameObject, int>();

    private void Start()
    {
        Instance = this;

        if (autoInit && !GameManager.Instance.editing)
            Init();

        GameManager.Instance.TankManagerUpdate(lastCampaignScene);
    }

    public void Init()
    {
        foreach (Transform child in spawnParent)
        {
            spawns.Add(child.GetComponent<Collider>());
        }

        resetInfoDictionary.Clear();
        int i = 0;
        foreach (Transform tank in tankParent)
        {
            resetInfoDictionary.Add(tank, new SaveableLevelObject.TransformInfo() { position = tank.position, rotation = tank.rotation });
            tankIndices.Add(tank.gameObject, i);
            i++;
        }

        foreach (int index in GameManager.Instance.destroyedTanks)
        {
            Destroy(tankParent.GetChild(index).gameObject);
        }

        if (GameManager.Instance.editing || (!PhotonNetwork.OfflineMode && !GameManager.Instance.inLobby))
        {
            if (DataManager.roomSettings.fillLobby)
            {
                tankLimit = DataManager.roomSettings.playerLimit - CustomNetworkHandling.NonSpectatorList.Length;
            }
            else
            {
                tankLimit = DataManager.roomSettings.botLimit;
            }

            if (DataManager.roomSettings.mode != "Co-Op")
            {
                if (PhotonNetwork.IsMasterClient && tankLimit > 0)
                {
                    foreach (GameObject tank in tanks.ToList())
                    {
                        if (!DataManager.roomSettings.bots.Contains(tank.name))
                        {
                            tanks.Remove(tank);
                        }
                    }

                    if (tanks.Count > 0 && spawns.Count > 0)
                    {
                        GenerateTanks();
                    }
                }
            }

            if (!PhotonNetwork.OfflineMode && PhotonNetwork.IsMasterClient)
            {
                AllocateOwnershipOfTanks();
            }
        }
    }

    void AllocateOwnershipOfTanks()
    {
        Player[] players = PhotonNetwork.PlayerList;
        int tankCount = tankParent.childCount;
        int playerCount = players.Length;

        int sum = 0;

        int remainder = tankCount % playerCount;
        int quotient = tankCount / playerCount;

        for (int i = 0; i < playerCount; i++)
        {
            int tankAmount;
            if (i < playerCount - 1)
            {
                tankAmount = i < remainder ? quotient + 1 : quotient;
            }
            else
            {
                tankAmount = tankCount - sum;
            }
            for (int j = 0; j < tankAmount; j++)
            {
                if (!GameManager.Instance.destroyedTanks.Contains(j + sum))
                    tankParent.GetChild(j + sum).GetComponent<PhotonView>().TransferOwnership(players[i]);
            }
            sum += tankAmount;
        }
    }

    public void GenerateTanks()
    {
        foreach (GameObject tank in spawnedTanks)
        {
            Destroy(tank);
        }

        int[] distribution = CustomRandom.Distribute(tankLimit, tanks.Count, deviateChance, amountDeviationMin, amountDeviationMax);
        for (int i = 0; i < tanks.Count; i++)
        {
            for (int j = 0; j < distribution[i]; j++)
            {
                SpawnTank(tanks[i]);
            }
        }
    }

    public void SpawnTank(GameObject tank)
    {
        if (tankParent.childCount < tankLimit)
        {
            Collider spawn = spawns[Random.Range(0, spawns.Count)];
            BoxCollider tankCollider = tank.transform.Find("Body").GetComponent<BoxCollider>();

            if (GameManager.Instance.inLobby)
            {
                Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(-180.0f, 180.0f), spawn.transform.up);
                spawnedTanks.Add(Instantiate(tank, CustomRandom.GetSpawnPointInCollider(spawn, -spawn.transform.up, ignoreLayerMask, tankCollider, randomRotation), randomRotation, tankParent));
            }
            else
            {
                PhotonTankView PTV;
                GameObject newTank;

                switch (DataManager.roomSettings.mode)
                {
                    case "FFA":
                        Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(-180.0f, 180.0f), spawn.transform.up);
                        if (PhotonNetwork.OfflineMode)
                        {
                            newTank = Instantiate(tank, CustomRandom.GetSpawnPointInCollider(spawn, -spawn.transform.up, ignoreLayerMask, tankCollider, randomRotation), randomRotation, tankParent);
                        }
                        else
                        {
                            newTank = PhotonNetwork.InstantiateRoomObject(tank.name, CustomRandom.GetSpawnPointInCollider(spawn, -spawn.transform.up, ignoreLayerMask, tankCollider, randomRotation), randomRotation, 0, new object[] { true, true });
                        }
                        break;
                    case "Teams":
                        spawn = PlayerManager.Instance.teamSpawnParent.GetChild(teamIndex).GetComponent<Collider>();
                        if (PhotonNetwork.OfflineMode)
                        {
                            newTank = Instantiate(tank, CustomRandom.GetSpawnPointInCollider(spawn, -spawn.transform.up, ignoreLayerMask, tankCollider, spawn.transform.rotation), spawn.transform.rotation, tankParent);
                            PTV = newTank.GetComponent<PhotonTankView>();
                        }
                        else
                        {
                            newTank = PhotonNetwork.InstantiateRoomObject(tank.name, CustomRandom.GetSpawnPointInCollider(spawn, -spawn.transform.up, ignoreLayerMask, tankCollider, spawn.transform.rotation), spawn.transform.rotation, 0, new object[] { true, false });
                            PTV = newTank.GetComponent<PhotonTankView>();
                        }
                        PTV.teamName = spawn.name;
                        teamIndex++;
                        if (teamIndex >= PlayerManager.Instance.teamSpawnParent.childCount)
                        {
                            teamIndex = 0;
                        }
                        break;
                    default:
                        if (PhotonNetwork.OfflineMode)
                        {
                            newTank = Instantiate(tank, CustomRandom.GetSpawnPointInCollider(spawn, -spawn.transform.up, ignoreLayerMask, tankCollider, spawn.transform.rotation), spawn.transform.rotation, tankParent);
                            PTV = newTank.GetComponent<PhotonTankView>();
                        }
                        else
                        {
                            newTank = PhotonNetwork.InstantiateRoomObject(tank.name, CustomRandom.GetSpawnPointInCollider(spawn, -spawn.transform.up, ignoreLayerMask, tankCollider, spawn.transform.rotation), spawn.transform.rotation, 0, new object[] { true, false });
                            spawnedTanks.Add(newTank);
                            PTV = newTank.GetComponent<PhotonTankView>();
                        }
                        PTV.teamName = "Bots";
                        break;
                }

                spawnedTanks.Add(newTank);
            }
        }
    }

    public void RespawnTank(Transform tankOrigin, float respawnDelay = 3)
    {
        StartCoroutine(RespawnTankRoutine(tankOrigin, respawnDelay));
    }

    IEnumerator RespawnTankRoutine(Transform tankOrigin, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        BoxCollider tankCollider = tankOrigin.Find("Body").GetComponent<BoxCollider>();
        Collider spawn = spawns[Random.Range(0, spawns.Count)];
        switch (DataManager.roomSettings.mode)
        {
            case "FFA":
                tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(spawn, Vector3.down, ignoreLayerMask, tankCollider), Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up));
                break;
            case "Teams":
                int teamSpawnCount = PlayerManager.Instance.teamSpawnParent.childCount;
                spawn = PlayerManager.Instance.teamSpawnParent.GetChild(Random.Range(0, teamSpawnCount)).GetComponent<Collider>();

                string tankTeam = tankOrigin.GetComponent<PhotonTankView>().teamName;
                foreach (Transform child in PlayerManager.Instance.teamSpawnParent)
                {
                    if (child.name == tankTeam)
                    {
                        spawn = child.GetComponent<Collider>();
                        break;
                    }
                }

                tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(spawn, Vector3.down, ignoreLayerMask, tankCollider), spawn.transform.rotation);
                break;
            default:
                tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(spawn, Vector3.down, ignoreLayerMask, tankCollider), spawn.transform.rotation);
                break;
        }

        PhotonView PV = tankOrigin.GetComponent<PhotonView>();

        PV.RPC("ReactivateTank", RpcTarget.All);
        PV.RPC("ResetTrails", RpcTarget.All);
    }

    public void StartCheckTankCount()
    {
        if (!GameManager.Instance.editing)
            StartCoroutine(CheckTankCount());
    }

    // Have to wait before checking childCount since mines can blow up multiple tanks simultaneously
    IEnumerator CheckTankCount()
    {
        if (!checking)
        {
            checking = true;
            yield return new WaitForEndOfFrame();
            if (!GameManager.Instance.inLobby)
            {
                if (tankParent.childCount < 1)
                {
                    GameManager.Instance.frozen = true;

                    if (PhotonNetwork.OfflineMode)
                    {
                        GameManager.Instance.destroyedTanks.Clear();
                        if (lastCampaignScene)
                        {
                            GameManager.Instance.LoadScene("End Scene", 3, true);
                        }
                        else
                        {
                            GameManager.Instance.LoadNextScene(3, true);
                        }
                    }
                    else
                    {
                        if (DataManager.roomSettings.mode == "Co-Op")
                        {
                            GameManager.Instance.destroyedTanks.Clear();
                            GameManager.Instance.frozen = true;

                            PlayerManager.Instance.StopCoroutines();
                        }

                        if (PhotonNetwork.IsMasterClient)
                        {
                            if (lastCampaignScene || GameManager.Instance.totalLives <= 0)
                            {
                                DataManager.roomSettings.map = "End Scene";
                                GameManager.Instance.PhotonLoadScene("End Scene", 3, true);
                            }
                            else
                            {
                                string path = SceneUtility.GetScenePathByBuildIndex(GameManager.Instance.currentScene.buildIndex + 1);
                                string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                                DataManager.roomSettings.map = sceneName;
                                GameManager.Instance.PhotonLoadNextScene(3, true);
                            }

                            PhotonHashtable roomProperties = new PhotonHashtable()
                            {
                                { "roomSettings", DataManager.roomSettings },
                                { "totalLives", GameManager.Instance.totalLives }
                            };
                            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
                        }
                        else
                        {
                            if (lastCampaignScene || GameManager.Instance.totalLives <= 0)
                            {
                                GameManager.Instance.PhotonLoadScene("End Scene", 3, true);
                            }
                            else
                            {
                                GameManager.Instance.PhotonLoadNextScene(3, true);
                            }
                        }
                    }
                }
            }
            else
            {
                if (tankParent.childCount < 2)
                {
                    Time.timeScale = 0.2f;
                    yield return new WaitForSecondsRealtime(4);
                    GameManager.Instance.ResetAutoPlay(2.5f);
                }
            }
            checking = false;
        }
    }

    public void ResetTanks()
    {
        foreach (GameObject tank in spawnedTanks.ToList())
        {
            Destroy(tank);
        }
        spawnedTanks.Clear();
        foreach (Transform tankTransform in tankParent)
        {
            if (resetInfoDictionary.TryGetValue(tankTransform, out SaveableLevelObject.TransformInfo resetInfo))
            {
                tankTransform.SetPositionAndRotation(resetInfo.position, resetInfo.rotation);
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient && !GameManager.Instance.inLobby)
        {
            AllocateOwnershipOfTanks();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient && !GameManager.Instance.inLobby)
        {
            AllocateOwnershipOfTanks();
        }
    }
}
