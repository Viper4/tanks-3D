using ExitGames.Client.Photon;
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

public class TankManager : MonoBehaviour
{
    public bool lastCampaignScene = false;
    [SerializeField] Transform tankParent;
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
        foreach (Transform child in teamSpawnParent)
        {
            teamSpawns.Add(child.GetComponent<Collider>());
        }
        foreach (Transform child in PVESpawnParent)
        {
            PVESpawns.Add(child.GetComponent<Collider>());
        }
        if (PhotonNetwork.IsMasterClient && !GameManager.Instance.inLobby)
        {
            roomSettings = ((RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"]);
            for (int i = 0; i < tanks.Count; i++)
            {
                if (!roomSettings.bots.Contains(tanks[i].name))
                {
                    tanks.RemoveAt(i);
                }
            }
            tankLimit = roomSettings.botLimit;
            GenerateTanks(true);
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
        switch (generationMode)
        {
            case GenerationMode.FFA:
                Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(-180.0f, 180.0f), freeForAllSpawn.transform.up);
                Instantiate(tank, CustomRandom.GetSpawnPointInCollider(freeForAllSpawn, -freeForAllSpawn.transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), randomRotation), randomRotation, tankParent);
                break;
            case GenerationMode.Teams:
                Instantiate(tank, CustomRandom.GetSpawnPointInCollider(teamSpawns[teamIndex], -teamSpawns[teamIndex].transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), teamSpawns[teamIndex].transform.rotation), teamSpawns[teamIndex].transform.rotation, tankParent);
                teamIndex++;
                if (teamIndex >= teamSpawns.Count)
                {
                    teamIndex = 0;
                }
                break;
            case GenerationMode.PVE:
                int spawnIndex = Random.Range(0, PVESpawns.Count);
                Instantiate(tank, CustomRandom.GetSpawnPointInCollider(PVESpawns[spawnIndex], -PVESpawns[spawnIndex].transform.up, ignoreLayerMask, tank.transform.Find("Body").GetComponent<BoxCollider>(), PVESpawns[spawnIndex].transform.rotation), PVESpawns[spawnIndex].transform.rotation, tankParent);
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
                // Implement team system for bots
                /*for (int i = 0; i < teamSpawns.Count; i++)
                {
                    if (teamSpawns[i].name == playerTeam.Name)
                    {
                        teamSpawnIndex = i;
                        break;
                    }
                }*/

                tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(teamSpawns[teamSpawnIndex], Vector3.down, ignoreLayerMask), teamSpawns[teamSpawnIndex].transform.rotation);
                break;
            case GenerationMode.PVE:
                int spawnIndex = Random.Range(0, PVESpawns.Count);
                tankOrigin.SetPositionAndRotation(CustomRandom.GetSpawnPointInCollider(PVESpawns[spawnIndex], Vector3.down, ignoreLayerMask), PVESpawns[spawnIndex].transform.rotation);
                break;
        }

        tankOrigin.GetComponent<PhotonView>().RPC("ReactivateTank", RpcTarget.All);

        tankOrigin.Find("TrackMarks").GetComponent<PhotonView>().RPC("ResetTrails", RpcTarget.All);
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
                        RoomSettings roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];
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
