using MyUnityAddons.Calculations;
using MyUnityAddons.CustomPhoton;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class TankManager : MonoBehaviour
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

    RoomSettings roomSettings;

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
        if (PhotonNetwork.PrefabPool is DefaultPool pool && tanks != null)
        {
            foreach (GameObject prefab in tanks)
            {
                if (!pool.ResourceCache.ContainsKey(prefab.name))
                {
                    pool.ResourceCache.Add(prefab.name, prefab);
                }
            }
        }

        foreach (Transform child in teamSpawnParent)
        {
            teamSpawns.Add(child.GetComponent<Collider>());
        }
        foreach (Transform child in PVESpawnParent)
        {
            PVESpawns.Add(child.GetComponent<Collider>());
        }

        if (!PhotonNetwork.OfflineMode)
        {
            roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];
            tankLimit = roomSettings.botLimit;

            if (roomSettings.primaryMode != "Co-Op" && roomSettings.botLimit > 0)
            {
                if (PhotonNetwork.IsMasterClient && !GameManager.Instance.inLobby)
                {
                    foreach (GameObject tank in tanks.ToList())
                    {
                        if (!roomSettings.bots.Contains(tank.name))
                        {
                            tanks.Remove(tank);
                        }
                    }
                    if (tanks.Count > 0)
                    {
                        GenerateTanks(true);
                    }
                }
            }
        }
    }

    public void GenerateTanks(bool cleanup = false)
    {
        if (cleanup)
        {
            foreach (Transform child in tankParent)
            {
                Destroy(child.gameObject);
            }
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
        PhotonTankView PTV;

        switch (generationMode)
        {
            case GenerationMode.FFA:
                Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(-180.0f, 180.0f), freeForAllSpawn.transform.up);
                if (GameManager.Instance.inLobby)
                {
                    TargetSystem targetSystem = Instantiate(tank, CustomRandom.GetSpawnPointInCollider(freeForAllSpawn, -freeForAllSpawn.transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), randomRotation), randomRotation, tankParent).GetComponent<TargetSystem>();
                    targetSystem.enemyParents.Add(tankParent);
                }
                else
                {
                    TargetSystem targetSystem = PhotonNetwork.InstantiateRoomObject(tank.name, CustomRandom.GetSpawnPointInCollider(freeForAllSpawn, -freeForAllSpawn.transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), randomRotation), randomRotation).GetComponent<TargetSystem>();
                    targetSystem.enemyParents.Add(PlayerManager.Instance.playerParent);
                    targetSystem.enemyParents.Add(tankParent);
                }
                break;
            case GenerationMode.Teams:
                if (GameManager.Instance.inLobby)
                {
                    PTV = Instantiate(tank, CustomRandom.GetSpawnPointInCollider(teamSpawns[teamIndex], -teamSpawns[teamIndex].transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), teamSpawns[teamIndex].transform.rotation), teamSpawns[teamIndex].transform.rotation, tankParent).GetComponent<PhotonTankView>();
                }
                else
                {
                    PTV = PhotonNetwork.InstantiateRoomObject(tank.name, CustomRandom.GetSpawnPointInCollider(teamSpawns[teamIndex], -teamSpawns[teamIndex].transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), teamSpawns[teamIndex].transform.rotation), teamSpawns[teamIndex].transform.rotation).GetComponent<PhotonTankView>();
                }
                PTV.teamName = teamSpawns[teamIndex].name;
                PTV.GetComponent<TargetSystem>().enemyParents.Add(tankParent);
                teamIndex++;
                if (teamIndex >= teamSpawns.Count)
                {
                    teamIndex = 0;
                }
                break;
            case GenerationMode.PVE:
                int spawnIndex = Random.Range(0, PVESpawns.Count);
                if (GameManager.Instance.inLobby)
                {
                    PTV = Instantiate(tank, CustomRandom.GetSpawnPointInCollider(PVESpawns[spawnIndex], -PVESpawns[spawnIndex].transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), PVESpawns[spawnIndex].transform.rotation), PVESpawns[spawnIndex].transform.rotation, tankParent).GetComponent<PhotonTankView>();
                }
                else
                {
                    PTV = PhotonNetwork.InstantiateRoomObject(tank.name, CustomRandom.GetSpawnPointInCollider(PVESpawns[spawnIndex], -PVESpawns[spawnIndex].transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), PVESpawns[spawnIndex].transform.rotation), PVESpawns[spawnIndex].transform.rotation).GetComponent<PhotonTankView>();
                }
                PTV.teamName = "PVE Tanks";
                PTV.GetComponent<TargetSystem>().enemyParents.Add(tankParent);
                break;
        }
    }

    public void RespawnTank(Transform tankOrigin, float respawnDelay = 3)
    {
        StartCoroutine(RespawnTankRoutine(tankOrigin, respawnDelay));
    }

    IEnumerator RespawnTankRoutine(Transform tankOrigin, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        switch (generationMode)
        {
            case GenerationMode.FFA:
                tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(freeForAllSpawn, Vector3.down, ignoreLayerMask), Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up));
                break;
            case GenerationMode.Teams:
                int teamSpawnIndex = Random.Range(0, teamSpawns.Count);

                string tankTeam = tankOrigin.GetComponent<PhotonTankView>().teamName;
                for (int i = 0; i < teamSpawns.Count; i++)
                {
                    if (teamSpawns[i].name == tankTeam)
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
                        if (roomSettings.primaryMode == "Co-Op")
                        {
                            GameManager.Instance.frozen = true;
                            PhotonNetwork.LocalPlayer.JoinOrSwitchTeam("Players");

                            FindObjectOfType<PlayerManager>().StopCoroutines();
                        }

                        if (PhotonNetwork.IsMasterClient)
                        {
                            if (lastCampaignScene || GameManager.Instance.totalLives <= 0)
                            {
                                roomSettings.map = "End Scene";
                                GameManager.Instance.PhotonLoadScene("End Scene", 3, true, false);
                            }
                            else
                            {
                                roomSettings.map = SceneManager.GetSceneByBuildIndex(GameManager.Instance.currentScene.buildIndex + 1).name;
                                Debug.Log(roomSettings.map + " / " + SceneManager.GetSceneByBuildIndex(GameManager.Instance.currentScene.buildIndex + 1).name);
                                GameManager.Instance.PhotonLoadNextScene(3, true);
                            }

                            Debug.Log("Set total lives");
                            PhotonHashtable roomProperties = new PhotonHashtable()
                            {
                                { "RoomSettings", roomSettings },
                                { "Total Lives", GameManager.Instance.totalLives }
                            };
                            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
                        }
                        else
                        {
                            if (lastCampaignScene || GameManager.Instance.totalLives <= 0)
                            {
                                GameManager.Instance.PhotonLoadScene("End Scene", 3, true, false);
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
                    StartCoroutine(GameManager.Instance.ResetAutoPlay(2.5f));
                }
            }
            checking = false;
        }
    }
}
