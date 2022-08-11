using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine.UI;
using Photon.Pun.UtilityScripts;
using MyUnityAddons.CustomPhoton;
using System.Linq;

public class WaitingRoom : MonoBehaviourPunCallbacks
{
    readonly byte UpdateUIEventCode = 0;
    readonly byte UpdateTeamsEventCode = 1;
    
    [SerializeField] GameObject playerSlotPrefab;

    [SerializeField] List<StringTransformDictionary> rosterContentList = new List<StringTransformDictionary>();
    private Dictionary<string, Transform> rosterContent = new Dictionary<string, Transform>();

    [SerializeField] DataManager dataManager;
    [SerializeField] PhotonTeamsManager teamManager;

    [SerializeField] GameObject[] ownerElements;

    [SerializeField] Text roomName;
    [SerializeField] Text mapName;

    [SerializeField] Text playerListCount;
    [SerializeField] Text spectatorListCount;
    [SerializeField] Text[] teamsListCount;

    [SerializeField] Transform[] teamsList;

    [SerializeField] Transform playerList;
    [SerializeField] Transform teamsTab;

    Dictionary<string, int> tempTeamsCount = new Dictionary<string, int>()
    {
        { "Team 4", -1 },
        { "Team 3", -1 },
        { "Team 2", -1 },
        { "Team 1", -1 }
    };

    // Start is called before the first frame update
    IEnumerator Start()
    {
        foreach (StringTransformDictionary keyValuePair in rosterContentList)
        {
            rosterContent.Add(keyValuePair.key, keyValuePair.value);
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            foreach (GameObject UIElement in ownerElements)
            {
                if (UIElement.CompareTag("Mode Button"))
                {
                    UIElement.GetComponent<Button>().interactable = false;
                }
                else
                {
                    UIElement.SetActive(false);
                }
            }
        }
        else
        {
            dataManager.currentRoomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];
        }
        UpdateBasicUI();

        playerList.gameObject.SetActive(dataManager.currentRoomSettings.primaryMode != "Teams");
        teamsTab.gameObject.SetActive(dataManager.currentRoomSettings.primaryMode == "Teams");
        AllocatePlayerToTeam(PhotonNetwork.LocalPlayer);
        yield return new WaitUntil(() => PhotonNetwork.LocalPlayer.GetPhotonTeam() != null);
        StartCoroutine(MasterUpdateTeamRosters());
        yield return new WaitForSecondsRealtime(0.2f);
        transform.Find("Loading").gameObject.SetActive(false);
    }

    public void ChangeTeam(string teamName)
    {
        int teamSize = teamManager.GetTeamMembersCount(teamName);
        
        if (teamSize < ((RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"]).teamSize)
        {
            PhotonTeam currentTeam = PhotonNetwork.LocalPlayer.GetPhotonTeam();
            if (currentTeam != null)
            {
                if (currentTeam.Name != teamName)
                {
                    PhotonNetwork.LocalPlayer.SwitchTeam(teamName);
                }
                else
                {
                    return;
                }
            }
            else
            {
                PhotonNetwork.LocalPlayer.JoinTeam(teamName);
            }

            StartCoroutine(MasterUpdateTeamRosters());
        }
    }

    public void StartGame() // Accessable only by MasterClient
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        PhotonHashtable roomProperties = new PhotonHashtable
        {
            ["Waiting"] = false,
            ["RoomSettings"] = dataManager.currentRoomSettings
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

        PhotonNetwork.LoadLevel(dataManager.currentRoomSettings.map);
    }

    public void ChangePrimaryMode(string mode) // Accessable only by MasterClient
    {
        if (dataManager.currentRoomSettings.primaryMode != mode)
        {
            dataManager.currentRoomSettings.primaryMode = mode;
            UpdateRoomSettingsProperty();

            tempTeamsCount["Team 4"] = teamManager.GetTeamMembersCount("Team 4");
            tempTeamsCount["Team 3"] = teamManager.GetTeamMembersCount("Team 3");
            tempTeamsCount["Team 2"] = teamManager.GetTeamMembersCount("Team 2");
            tempTeamsCount["Team 1"] = teamManager.GetTeamMembersCount("Team 1");
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                AllocatePlayerToTeam(player);
            }

            MasterUpdateBasicUI();
        }
    }
    
    private void AllocatePlayerToTeam(Player player)
    {
        PhotonTeam currentTeam = player.GetPhotonTeam();

        switch (((RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"]).primaryMode)
        {
            case "Teams":
                if (tempTeamsCount.Values.Sum() < dataManager.currentRoomSettings.playerLimit)
                {
                    PhotonTeam smallestTeam = null;
                    foreach (string key in tempTeamsCount.Keys)
                    {
                        if (smallestTeam == null)
                        {
                            if (tempTeamsCount[key] < dataManager.currentRoomSettings.teamSize)
                            {
                                teamManager.TryGetTeamByName(key, out smallestTeam);
                            }
                        }
                        else
                        {
                            if (tempTeamsCount[smallestTeam.Name] < dataManager.currentRoomSettings.teamSize && tempTeamsCount[key] <= tempTeamsCount[smallestTeam.Name])
                            {
                                teamManager.TryGetTeamByName(key, out smallestTeam);
                            }
                        }
                    }
                    if (smallestTeam != null)
                    {
                        if (currentTeam != null)
                        {
                            if (currentTeam.Name == "Players")
                            {
                                tempTeamsCount[smallestTeam.Name]++;
                                player.SwitchTeam(smallestTeam);
                                return;
                            }
                        }
                        else
                        {
                            tempTeamsCount[smallestTeam.Name]++;
                            player.JoinTeam(smallestTeam);
                            return;
                        }
                    }
                }
                break;
            default: // FFA, PvE, Co-Op
                if (teamManager.GetTeamMembersCount("Players") < dataManager.currentRoomSettings.playerLimit)
                {
                    if (currentTeam != null)
                    {
                        if (currentTeam.Name == "Players")
                        {
                            return;
                        }
                        if (currentTeam.Name.Contains("Team"))
                        {
                            player.SwitchTeam("Players");
                            return;
                        }
                    }
                    else
                    {
                        Debug.Log("Joined Team");
                        player.JoinTeam("Players");
                        return;
                    }
                }
                break;
        }

        player.JoinOrSwitchTeam("Spectators");
    }

    public void UpdateRoomSettingsProperty() // Accessable only by MasterClient
    {
        PhotonHashtable roomProperties = new PhotonHashtable
        {
            ["RoomSettings"] = dataManager.currentRoomSettings
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        StartCoroutine(MasterUpdateTeamRosters());
    }

    private IEnumerator MasterUpdateTeamRosters()
    {
        yield return new WaitForSecondsRealtime(0.15f); // Wait for teams to sync
        Debug.Log("Updated Team Rosters");
        PhotonNetwork.RaiseEvent(UpdateTeamsEventCode, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        UpdateTeamRosters();
    }

    private void UpdateTeamRosters()
    {
        RoomSettings currentRoomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];
        foreach (Transform content in rosterContent.Values)
        {
            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            PhotonTeam team = player.GetPhotonTeam();
            if (team != null && rosterContent.ContainsKey(team.Name))
            {
                GameObject newPlayerSlot = Instantiate(playerSlotPrefab, rosterContent[team.Name]);
                newPlayerSlot.GetComponent<Text>().text = player.NickName;
            }
        }

        playerListCount.text = teamManager.GetTeamMembersCount("Players") + "/" + currentRoomSettings.playerLimit + " players";
        spectatorListCount.text = teamManager.GetTeamMembersCount("Spectators") + " spectating";
        for (int i = 0; i < teamsListCount.Length; i++)
        {
            teamsListCount[i].text = teamManager.GetTeamMembersCount("Team " + (i+1)) + "/" + currentRoomSettings.teamSize + " players";
        }
    }

    public void MasterUpdateBasicUI()
    {
        PhotonNetwork.RaiseEvent(UpdateUIEventCode, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        UpdateBasicUI();
    }

    private void UpdateBasicUI()
    {
        RoomSettings currentRoomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];

        roomName.text = PhotonNetwork.CurrentRoom.Name;
        mapName.text = currentRoomSettings.map;

        tempTeamsCount = new Dictionary<string, int>()
        {
            { "Team 4", teamManager.GetTeamMembersCount("Team 4") },
            { "Team 3", teamManager.GetTeamMembersCount("Team 3") },
            { "Team 2", teamManager.GetTeamMembersCount("Team 2") },
            { "Team 1", teamManager.GetTeamMembersCount("Team 1") }
        };
        for (int i = 0; i < 4; i++)
        {
            if (i + 1 > currentRoomSettings.teamLimit)
            {
                tempTeamsCount.Remove("Team " + i);
                teamsList[i].gameObject.SetActive(false);
            }
            else
            {
                teamsList[i].gameObject.SetActive(true);
            }
        }
    }

    private void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
    }

    private void OnEvent(EventData eventData)
    {
        if (eventData.Code == UpdateUIEventCode)
        {
            Debug.Log("Raised UpdateUIEventCode");
            UpdateBasicUI();
        }
        else if (eventData.Code == UpdateTeamsEventCode)
        {
            Debug.Log("Raised UpdateTeamsEventCode");
            UpdateTeamRosters();
        }
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LocalPlayer.LeaveCurrentTeam();
        StartCoroutine(MasterUpdateTeamRosters());

        SceneManager.LoadScene("Lobby");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Here");
        if (PhotonNetwork.IsMasterClient)
        {
            otherPlayer.LeaveCurrentTeam();
            StartCoroutine(MasterUpdateTeamRosters());
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log("Switched master client to " + newMasterClient.NickName);
        if (PhotonNetwork.LocalPlayer == newMasterClient)
        {
            foreach (GameObject UIElement in ownerElements)
            {
                if (UIElement.CompareTag("Mode Button"))
                {
                    UIElement.GetComponent<Button>().interactable = true;
                }
                else
                {
                    UIElement.SetActive(true);
                }
            }
        }
    }

    [System.Serializable]
    private struct StringTransformDictionary
    {
        public string key;
        public Transform value;
    }
}