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
    [SerializeField] int tankLimit = 12;
    [SerializeField] float deviateChance;
    [SerializeField] int amountDeviationMin;
    [SerializeField] int amountDeviationMax;
    [SerializeField] List<GameObject> tanks;
    [SerializeField] Collider freeForAllSpawn;
    [SerializeField] Transform teamSpawnParent;
    [SerializeField] Transform PVESpawnParent;
    List<Collider> teamSpawns = new List<Collider>();
    List<Collider> PVESpawns = new List<Collider>();
    [SerializeField] LayerMask ignoreLayerMask;
    bool checking = false;

    int teamIndex = 0;

    public enum GenerationMode
    {
        FFA,
        Teams,
        PVE
    }
    [SerializeField] GenerationMode generationMode;

    private void Start()
    {
        Instance = this;

        foreach(Transform child in teamSpawnParent)
        {
            teamSpawns.Add(child.GetComponent<Collider>());
        }
        foreach(Transform child in PVESpawnParent)
        {
            PVESpawns.Add(child.GetComponent<Collider>());
        }

        if(!PhotonNetwork.OfflineMode && !GameManager.Instance.inLobby)
        {
            if(DataManager.roomSettings.fillLobby)
            {
                tankLimit = DataManager.roomSettings.playerLimit - CustomNetworkHandling.NonSpectatorList.Length;
            }
            else
            {
                tankLimit = DataManager.roomSettings.botLimit;
            }

            if(DataManager.roomSettings.mode != "Co-Op")
            {
                if(PhotonNetwork.IsMasterClient && DataManager.roomSettings.botLimit > 0)
                {
                    foreach(GameObject tank in tanks.ToList())
                    {
                        if(!DataManager.roomSettings.bots.Contains(tank.name))
                        {
                            tanks.Remove(tank);
                        }
                    }
                    if(tanks.Count > 0)
                    {
                        GenerateTanks(true);
                    }
                }
            }
            else
            {
                foreach(Transform tank in tankParent)
                {
                    tank.GetComponent<TargetSystem>().enemyParents.Add(PlayerManager.Instance.playerParent);
                }
            }

            if(PhotonNetwork.IsMasterClient)
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

        for(int i = 0; i < playerCount; i++)
        {
            int tankAmount;
            if(i < playerCount - 1)
            {
                tankAmount = i < remainder ? quotient + 1 : quotient;
            }
            else
            {
                tankAmount = tankCount - sum;
            }
            for(int j = 0; j < tankAmount; j++)
            {
                tankParent.GetChild(j + sum).GetComponent<PhotonView>().TransferOwnership(players[i]);
            }
            sum += tankAmount;
        }
    }

    public void GenerateTanks(bool cleanup = false)
    {
        if(cleanup)
        {
            foreach(Transform child in tankParent)
            {
                Destroy(child.gameObject);
            }
        }

        int[] distribution = CustomRandom.Distribute(tankLimit, tanks.Count, deviateChance, amountDeviationMin, amountDeviationMax);
        for(int i = 0; i < tanks.Count; i++)
        {
            for(int j = 0; j < distribution[i]; j++)
            {
                SpawnTank(tanks[i]);
            }
        }
    }

    public void SpawnTank(GameObject tank)
    {
        PhotonTankView PTV;
        if(DataManager.roomSettings.fillLobby)
        {
            tankLimit = DataManager.roomSettings.playerLimit - CustomNetworkHandling.NonSpectatorList.Length;
        }
        else
        {
            tankLimit = DataManager.roomSettings.botLimit;
        }

        if(tankParent.childCount < tankLimit)
        {
            switch(generationMode)
            {
                case GenerationMode.FFA:
                    Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(-180.0f, 180.0f), freeForAllSpawn.transform.up);
                    if(GameManager.Instance.inLobby)
                    {
                        TargetSystem targetSystem = Instantiate(tank, CustomRandom.GetSpawnPointInCollider(freeForAllSpawn, -freeForAllSpawn.transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), randomRotation), randomRotation, tankParent).GetComponent<TargetSystem>();
                        targetSystem.enemyParents.Add(tankParent);
                    }
                    else
                    {
                        PhotonNetwork.InstantiateRoomObject(tank.name, CustomRandom.GetSpawnPointInCollider(freeForAllSpawn, -freeForAllSpawn.transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), randomRotation), randomRotation, 0, new object[] { true, true });
                    }
                    break;
                case GenerationMode.Teams:
                    if(GameManager.Instance.inLobby)
                    {
                        PTV = Instantiate(tank, CustomRandom.GetSpawnPointInCollider(teamSpawns[teamIndex], -teamSpawns[teamIndex].transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), teamSpawns[teamIndex].transform.rotation), teamSpawns[teamIndex].transform.rotation, tankParent).GetComponent<PhotonTankView>();
                        PTV.GetComponent<TargetSystem>().enemyParents.Add(tankParent);
                    }
                    else
                    {
                        PTV = PhotonNetwork.InstantiateRoomObject(tank.name, CustomRandom.GetSpawnPointInCollider(teamSpawns[teamIndex], -teamSpawns[teamIndex].transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), teamSpawns[teamIndex].transform.rotation), teamSpawns[teamIndex].transform.rotation, 0, new object[] { true, false }).GetComponent<PhotonTankView>();
                    }
                    PTV.teamName = teamSpawns[teamIndex].name;
                    teamIndex++;
                    if(teamIndex >= teamSpawns.Count)
                    {
                        teamIndex = 0;
                    }
                    break;
                case GenerationMode.PVE:
                    int spawnIndex = Random.Range(0, PVESpawns.Count);
                    if(GameManager.Instance.inLobby)
                    {
                        PTV = Instantiate(tank, CustomRandom.GetSpawnPointInCollider(PVESpawns[spawnIndex], -PVESpawns[spawnIndex].transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), PVESpawns[spawnIndex].transform.rotation), PVESpawns[spawnIndex].transform.rotation, tankParent).GetComponent<PhotonTankView>();
                        PTV.GetComponent<TargetSystem>().enemyParents.Add(tankParent);
                    }
                    else
                    {
                        PTV = PhotonNetwork.InstantiateRoomObject(tank.name, CustomRandom.GetSpawnPointInCollider(PVESpawns[spawnIndex], -PVESpawns[spawnIndex].transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), PVESpawns[spawnIndex].transform.rotation), PVESpawns[spawnIndex].transform.rotation, 0, new object[] { true, false }).GetComponent<PhotonTankView>();
                    }
                    PTV.teamName = "PVE Tanks";
                    break;
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

        switch(generationMode)
        {
            case GenerationMode.FFA:
                tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(freeForAllSpawn, Vector3.down, ignoreLayerMask), Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up));
                break;
            case GenerationMode.Teams:
                int teamSpawnIndex = Random.Range(0, teamSpawns.Count);

                string tankTeam = tankOrigin.GetComponent<PhotonTankView>().teamName;
                for(int i = 0; i < teamSpawns.Count; i++)
                {
                    if(teamSpawns[i].name == tankTeam)
                    {
                        teamSpawnIndex = i;
                        break;
                    }
                }

                tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(teamSpawns[teamSpawnIndex], Vector3.down, ignoreLayerMask), teamSpawns[teamSpawnIndex].transform.rotation);
                break;
            case GenerationMode.PVE:
                int spawnIndex = Random.Range(0, PVESpawns.Count);
                tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(PVESpawns[spawnIndex], Vector3.down, ignoreLayerMask), PVESpawns[spawnIndex].transform.rotation);
                break;
        }

        PhotonView PV = tankOrigin.GetComponent<PhotonView>();

        PV.RPC("ReactivateTank", RpcTarget.All);
        PV.RPC("ResetTrails", RpcTarget.All);
    }

    public void StartCheckTankCount()
    {
        StartCoroutine(CheckTankCount());
    }

    // Have to wait before checking childCount since mines can blow up multiple tanks simultaneously
    IEnumerator CheckTankCount()
    {
        if(!checking)
        {
            checking = true;
            yield return new WaitForEndOfFrame();
            if(!GameManager.Instance.inLobby)
            {
                if(tankParent.childCount < 1)
                {
                    GameManager.Instance.frozen = true;

                    if(PhotonNetwork.OfflineMode)
                    {
                        if(lastCampaignScene)
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
                        if(DataManager.roomSettings.mode == "Co-Op")
                        {
                            GameManager.Instance.frozen = true;

                            PlayerManager.Instance.StopCoroutines();
                        }

                        if(PhotonNetwork.IsMasterClient)
                        {
                            if(lastCampaignScene || GameManager.Instance.totalLives <= 0)
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
                                { "RoomSettings", DataManager.roomSettings },
                                { "Total Lives", GameManager.Instance.totalLives }
                            };
                            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
                        }
                        else
                        {
                            if(lastCampaignScene || GameManager.Instance.totalLives <= 0)
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
                if(tankParent.childCount < 2)
                {
                    Time.timeScale = 0.2f;
                    yield return new WaitForSecondsRealtime(4);
                    StartCoroutine(GameManager.Instance.ResetAutoPlay(2.5f));
                }
            }
            checking = false;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if(PhotonNetwork.IsMasterClient && !GameManager.Instance.inLobby)
        {
            AllocateOwnershipOfTanks();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if(PhotonNetwork.IsMasterClient && !GameManager.Instance.inLobby)
        {
            AllocateOwnershipOfTanks();
        }
    }
}
