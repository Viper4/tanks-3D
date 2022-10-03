using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine.UI;
using Photon.Pun.UtilityScripts;
using MyUnityAddons.CustomPhoton;
using System.Text.RegularExpressions;

public class WaitingRoom : MonoBehaviourPunCallbacks
{
    readonly byte UpdateUICode = 0;
    readonly byte UpdateTeamsCode = 1;
    readonly byte LeaveWaitingRoomCode = 2;

    [SerializeField] GameObject playerSlotPrefab;

    [SerializeField] List<StringTransformPair> rosterContentList = new List<StringTransformPair>();
    private Dictionary<string, Transform> rosterContent = new Dictionary<string, Transform>();

    [SerializeField] GameObject[] ownerElements;

    [SerializeField] Text roomName;
    [SerializeField] Text mapName;

    [SerializeField] Text playerListCount;
    [SerializeField] Text spectatorListCount;
    [SerializeField] Text[] teamsListCount;

    [SerializeField] Transform[] teamsList;

    [SerializeField] Transform playerList;
    [SerializeField] Transform teamsTab;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        foreach (StringTransformPair keyValuePair in rosterContentList)
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
            DataManager.roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];
        }
        UpdateBasicUI();

        playerList.gameObject.SetActive(DataManager.roomSettings.primaryMode != "Teams");
        teamsTab.gameObject.SetActive(DataManager.roomSettings.primaryMode == "Teams");
        StartCoroutine(MasterUpdateTeamRosters());
        yield return new WaitForSecondsRealtime(0.2f);
        transform.Find("Loading").gameObject.SetActive(false);
    }

    public void ChangeTeam(string teamName)
    {
        int teamSize = PhotonTeamsManager.Instance.GetTeamMembersCount(teamName);
        
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
        PhotonNetwork.RaiseEvent(LeaveWaitingRoomCode, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        if (DataManager.roomSettings.primaryMode == "Co-Op")
        {
            string campaign = Regex.Match(DataManager.roomSettings.map, @"(.*?)[ ][0-9]+$").Groups[1].ToString();
            DataManager.playerData = SaveSystem.ResetPlayerData(campaign + "PlayerData");
        }
        else
        {
            DataManager.playerData = SaveSystem.defaultPlayerData.Copy(new PlayerData());
        }

        PhotonHashtable roomProperties = new PhotonHashtable
        {
            { "Waiting", false },
            { "RoomSettings", DataManager.roomSettings },
            { "Total Lives", DataManager.roomSettings.totalLives }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        PhotonHashtable playerProperties = new PhotonHashtable()
        {
            { "Original Team", PhotonNetwork.LocalPlayer.GetPhotonTeam().Name }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);

        GameManager.Instance.PhotonLoadScene(DataManager.roomSettings.map, 0, false);
    }

    public void ChangePrimaryMode(string mode) // Accessable only by MasterClient
    {
        if (DataManager.roomSettings.primaryMode != mode)
        {
            DataManager.roomSettings.primaryMode = mode;
            if (mode == "Co-Op")
            {
                if (DataManager.roomSettings.map != "Classic 1" && DataManager.roomSettings.map != "Regular 1")
                {
                    DataManager.roomSettings.map = "Classic 1";
                }
            }
            else
            {
                if (DataManager.roomSettings.map == "Classic 1" || DataManager.roomSettings.map == "Regular 1")
                {
                    DataManager.roomSettings.map = "Classic";
                }
            }
            UpdateRoomSettingsProperty();

            MasterUpdateAllUI();
        }
    }

    public void UpdateRoomSettingsProperty() // Accessable only by MasterClient
    {
        PhotonHashtable roomProperties = new PhotonHashtable
        {
            { "RoomSettings", DataManager.roomSettings },
            { "Total Lives", DataManager.roomSettings.totalLives }
        };
        PhotonNetwork.CurrentRoom.IsVisible = DataManager.roomSettings.isPublic;
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
    }

    public void MasterUpdateAllUI()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            player.AllocatePlayerToTeam();
        }
        PhotonNetwork.RaiseEvent(UpdateUICode, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        UpdateBasicUI();
        StartCoroutine(MasterUpdateTeamRosters());
    }

    private IEnumerator MasterUpdateTeamRosters()
    {
        yield return new WaitForSecondsRealtime(0.15f); // Wait for teams to sync
        PhotonNetwork.RaiseEvent(UpdateTeamsCode, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        UpdateTeamRosters();
    }

    private void UpdateTeamRosters()
    {
        DataManager.roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];
        playerList.gameObject.SetActive(DataManager.roomSettings.primaryMode != "Teams");
        teamsTab.gameObject.SetActive(DataManager.roomSettings.primaryMode == "Teams");
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

        playerListCount.text = PhotonTeamsManager.Instance.GetTeamMembersCount("Players") + "/" + DataManager.roomSettings.playerLimit + " players";
        spectatorListCount.text = PhotonTeamsManager.Instance.GetTeamMembersCount("Spectators") + " spectating";
        for (int i = 0; i < teamsListCount.Length; i++)
        {
            teamsListCount[i].text = PhotonTeamsManager.Instance.GetTeamMembersCount("Team " + (i+1)) + "/" + DataManager.roomSettings.teamSize + " players";
        }
    }

    private void UpdateBasicUI()
    {
        DataManager.roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];

        roomName.text = PhotonNetwork.CurrentRoom.Name;
        mapName.text = DataManager.roomSettings.map + " (" + DataManager.roomSettings.primaryMode + ")";

        for (int i = 0; i < 4; i++)
        {
            if (i + 1 > DataManager.roomSettings.teamLimit)
            {
                teamsList[i].gameObject.SetActive(false);
            }
            else
            {
                teamsList[i].gameObject.SetActive(true);
            }
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
    }

    void OnEvent(EventData eventData)
    {
        if (eventData.Code == UpdateUICode)
        {
            UpdateBasicUI();
        }
        else if (eventData.Code == UpdateTeamsCode)
        {
            UpdateTeamRosters();
        }
        else if (eventData.Code == LeaveWaitingRoomCode)
        {
            DataManager.roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];

            if (DataManager.roomSettings.primaryMode == "Co-Op")
            {
                string campaign = Regex.Match(DataManager.roomSettings.map, @"(.*?)[ ][0-9]+$").Groups[1].ToString();
                DataManager.playerData = SaveSystem.ResetPlayerData(campaign + "PlayerData");
            }
            else
            {
                DataManager.playerData = SaveSystem.defaultPlayerData.Copy(new PlayerData());
            }
            PhotonHashtable playerProperties = new PhotonHashtable()
            {
                { "Original Team", PhotonNetwork.LocalPlayer.GetPhotonTeam().Name }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            otherPlayer.LeaveCurrentTeam();
            StartCoroutine(MasterUpdateTeamRosters());
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
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
    private struct StringTransformPair
    {
        public string key;
        public Transform value;
    }
}
