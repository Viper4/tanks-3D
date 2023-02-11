using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using MyUnityAddons.CustomPhoton;

public class TeamSwitching : MonoBehaviourPunCallbacks
{
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
        if(eventData.Code == EventCodes.UpdateTeams)
        {
            PhotonHashtable parameters = (PhotonHashtable)eventData.Parameters[ParameterCode.Data];
            Invoke(nameof(UpdateRosters),(float)parameters["delay"]);
        }
    }

    public void SetTempTeam()
    {
        if(tempTeamName != null)
        {
            ChangeTeam(tempTeamName);
            ResetTempVariables();
        }
    }

    public void ResetTempVariables()
    {
        if(tempPlayerSlot != null)
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

        if(teamSize < DataManager.roomSettings.teamSize)
        {
            PhotonTeam currentTeam = PhotonNetwork.LocalPlayer.GetPhotonTeam();
            if(currentTeam == null || currentTeam.Name != teamName)
            {
                if(GameManager.Instance.inLobby)
                {
                    PhotonNetwork.LocalPlayer.JoinOrSwitchTeam(teamName);
                }
                else
                {
                    if(teamName == "Spectators")
                    {
                        PhotonNetwork.LocalPlayer.JoinOrSwitchTeam(teamName);
                        PlayerManager.Instance.RespawnAsSpectator(transform.parent);
                    }
                    else if(PhotonTeamsManager.Instance.TryGetTeamByName(teamName, out var team))
                    {
                        PhotonNetwork.LocalPlayer.JoinOrSwitchTeam(teamName);

                        switch(DataManager.roomSettings.mode)
                        {
                            case "Teams":
                                if(currentTeam != null && currentTeam.Name != "Spectators")
                                {
                                    if (GameManager.Instance.canSpawn)
                                    {
                                        PhotonNetwork.Destroy(transform.parent.gameObject);
                                        PlayerManager.Instance.SpawnInLocalPlayer(team);
                                    }
                                }
                                else
                                {
                                    if (GameManager.Instance.canSpawn)
                                    {
                                        PlayerManager.Instance.SpawnInLocalPlayer(team);
                                        Destroy(transform.parent.gameObject);
                                    }
                                }
                                break;
                            default:
                                if(currentTeam == null || currentTeam.Name == "Spectators")
                                {
                                    if (GameManager.Instance.canSpawn)
                                    {
                                        PlayerManager.Instance.SpawnInLocalPlayer(team);
                                        Destroy(transform.parent.gameObject);
                                    }
                                }
                                break;
                        }
                    }
                }
                MasterUpdateRosters();
            }
        }
    }

    public void MasterUpdateRosters()
    {
        PhotonNetwork.RaiseEvent(EventCodes.UpdateTeams, new PhotonHashtable() { { "delay", 0.15f } }, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        Invoke(nameof(UpdateRosters), 0.15f);
    }

    public void UpdateRosters()
    {
        playerList.SetActive(DataManager.roomSettings.mode != "Teams");
        teamsTab.SetActive(DataManager.roomSettings.mode == "Teams");
        foreach(Transform content in rosterContents)
        {
            foreach(Transform child in content)
            {
                Destroy(child.gameObject);
            }
        }

        foreach(Player player in PhotonNetwork.PlayerList)
        {
            PhotonTeam team = player.GetPhotonTeam();
            if(team != null)
            {
                GameObject newPlayerSlot = Instantiate(playerSlotPrefab, rosterContents[rosterContentNames.IndexOf(team.Name)]);
                newPlayerSlot.GetComponent<TextMeshProUGUI>().text = player.NickName;
                if(player == PhotonNetwork.LocalPlayer)
                {
                    currentPlayerSlot = newPlayerSlot;
                }
            }
        }

        if(tempMode && rostersParentObject.activeInHierarchy)
        {
            UpdateTempRosters();
        }

        playerListLabel.text = PhotonTeamsManager.Instance.GetTeamMembersCount("Players") + "/" + DataManager.roomSettings.playerLimit + " players";
        spectatorListLabel.text = PhotonTeamsManager.Instance.GetTeamMembersCount("Spectators") + " spectating";
        for(int i = 0; i < teamListLabels.Length; i++)
        {
            teamListLabels[i].text = PhotonTeamsManager.Instance.GetTeamMembersCount("Team " +(i + 1)) + "/" + DataManager.roomSettings.teamSize + " players";
        }

        for(int i = 0; i < 4; i++)
        {
            if(i + 1 > DataManager.roomSettings.teamLimit)
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
        if(tempPlayerSlot != null)
        {
            Destroy(tempPlayerSlot);
        }

        if(!string.IsNullOrEmpty(tempTeamName))
        {
            PhotonTeam localTeam = PhotonNetwork.LocalPlayer.GetPhotonTeam();
            if(localTeam != null)
            {
                if(localTeam.Name == tempTeamName)
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
