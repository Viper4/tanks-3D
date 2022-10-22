using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MyUnityAddons.CustomPhoton;

public class TeamSwitching : MonoBehaviourPunCallbacks
{
    readonly byte UpdateTeamsCode = 1;

    [SerializeField] List<string> rosterContentNames = new List<string>();
    [SerializeField] List<Transform> rosterContents = new List<Transform>();

    [SerializeField] GameObject playerSlotPrefab;

    [SerializeField] GameObject playerList;
    [SerializeField] GameObject teamsTab;
    [SerializeField] GameObject spectatorList;

    [SerializeField] Text playerListLabel;
    [SerializeField] Text spectatorListLabel;
    [SerializeField] Text[] teamListLabels;

    [SerializeField] bool tempMode;
    [SerializeField] GameObject rostersParentObject;
    [SerializeField] Color nextPlayerSlotColor;
    [SerializeField] Color previousPlayerSlotColor;
    string tempTeamName;
    GameObject currentPlayerSlot;
    GameObject tempPlayerSlot;

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
        if (eventData.Code == UpdateTeamsCode)
        {
            UpdateRosters();
            if (tempMode && rostersParentObject.activeInHierarchy)
            {
                UpdateTempRosters();
            }
        }
    }

    public void SetTempTeam()
    {
        if (tempTeamName != null)
        {
            ChangeTeam(tempTeamName);
            ResetTempVariables();
        }
    }

    public void ResetTempVariables()
    {
        if (tempPlayerSlot != null)
        {
            currentPlayerSlot.GetComponent<TextMeshProUGUI>().color = playerSlotPrefab.GetComponent<TextMeshProUGUI>().color;
            Destroy(tempPlayerSlot);
            tempTeamName = null;
        }
    }

    public void ChangeTempTeam(string teamName)
    {
        tempTeamName = teamName;
        UpdateTempRosters();
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

            StartCoroutine(MasterUpdateRosters());

            if (!GameManager.Instance.inLobby)
            {
                if (teamName == "Spectators")
                {
                    PlayerManager.Instance.RespawnAsSpectator(transform.parent);
                }
                else if ((currentTeam == null || currentTeam.Name == "Spectators") && PhotonTeamsManager.Instance.TryGetTeamByName(teamName, out var team))
                {
                    PlayerManager.Instance.SpawnInLocalPlayer(team);
                    Destroy(transform.parent.gameObject);
                }
            }
        }
    }

    public void MasterStartUpdateRosters()
    {
        StartCoroutine(MasterUpdateRosters());
    }

    private IEnumerator MasterUpdateRosters()
    {
        yield return new WaitForSecondsRealtime(0.15f); // Wait for teams to sync
        PhotonNetwork.RaiseEvent(UpdateTeamsCode, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        UpdateRosters();
    }

    public void UpdateRosters()
    {
        DataManager.roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];
        playerList.SetActive(DataManager.roomSettings.mode != "Teams");
        teamsTab.SetActive(DataManager.roomSettings.mode == "Teams");
        foreach (Transform content in rosterContents)
        {
            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            PhotonTeam team = player.GetPhotonTeam();
            if (team != null)
            {
                currentPlayerSlot = Instantiate(playerSlotPrefab, rosterContents[rosterContentNames.IndexOf(team.Name)]);
                currentPlayerSlot.GetComponent<TextMeshProUGUI>().text = player.NickName;
            }
        }

        if (tempMode && rostersParentObject.activeInHierarchy)
        {
            UpdateTempRosters();
        }

        playerListLabel.text = PhotonTeamsManager.Instance.GetTeamMembersCount("Players") + "/" + DataManager.roomSettings.playerLimit + " players";
        spectatorListLabel.text = PhotonTeamsManager.Instance.GetTeamMembersCount("Spectators") + " spectating";
        for (int i = 0; i < teamListLabels.Length; i++)
        {
            teamListLabels[i].text = PhotonTeamsManager.Instance.GetTeamMembersCount("Team " + (i + 1)) + "/" + DataManager.roomSettings.teamSize + " players";
        }

        for (int i = 0; i < 4; i++)
        {
            if (i + 1 > DataManager.roomSettings.teamLimit)
            {
                teamsTab.transform.GetChild(i).gameObject.SetActive(false);
            }
            else
            {
                teamsTab.transform.GetChild(i).gameObject.SetActive(true);
            }
        }
    }

    private void UpdateTempRosters()
    {
        if (tempPlayerSlot != null)
        {
            Destroy(tempPlayerSlot);
        }

        if (tempTeamName != null)
        {
            PhotonTeam localTeam = PhotonNetwork.LocalPlayer.GetPhotonTeam();
            if (localTeam != null)
            {
                if (localTeam.Name == tempTeamName)
                {
                    ResetTempVariables();
                    return;
                }
                else
                {
                    currentPlayerSlot.GetComponent<TextMeshProUGUI>().color = previousPlayerSlotColor;
                }
            }

            tempPlayerSlot = Instantiate(playerSlotPrefab, rosterContents[rosterContentNames.IndexOf(tempTeamName)]);
            TextMeshProUGUI tempPlayerSlotTMP = tempPlayerSlot.GetComponent<TextMeshProUGUI>();
            tempPlayerSlotTMP.text = PhotonNetwork.NickName;
            tempPlayerSlotTMP.color = nextPlayerSlotColor;
        }
    }
}