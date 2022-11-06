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
    readonly byte LeaveWaitingRoomCode = 2;

    [SerializeField] GameObject playerSlotPrefab;

    [SerializeField] GameObject[] ownerElements;

    [SerializeField] Text roomName;
    [SerializeField] Text mapName;

    [SerializeField] TeamSwitching teamSwitcher;

    // Start is called before the first frame update
    IEnumerator Start()
    {
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

        yield return new WaitForSecondsRealtime(0.2f);
        UpdateBasicUI();
        teamSwitcher.MasterUpdateRosters();
        transform.Find("Loading").gameObject.SetActive(false);
    }

    public void StartGame() // Accessable only by MasterClient
    {
        PhotonNetwork.RaiseEvent(LeaveWaitingRoomCode, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        if (DataManager.roomSettings.mode == "Co-Op")
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

        GameManager.Instance.PhotonLoadScene(DataManager.roomSettings.map, 0, false);
    }

    public void ChangeMode(string mode) // Accessable only by MasterClient
    {
        if (DataManager.roomSettings.mode != mode)
        {
            DataManager.roomSettings.mode = mode;
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
        teamSwitcher.MasterUpdateRosters();
    }

    private void UpdateBasicUI()
    {
        DataManager.roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];

        roomName.text = PhotonNetwork.CurrentRoom.Name;
        mapName.text = DataManager.roomSettings.map + " (" + DataManager.roomSettings.mode + ")";

        teamSwitcher.UpdateRosters();
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
        else if (eventData.Code == LeaveWaitingRoomCode)
        {
            DataManager.roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];

            if (DataManager.roomSettings.mode == "Co-Op")
            {
                string campaign = Regex.Match(DataManager.roomSettings.map, @"(.*?)[ ][0-9]+$").Groups[1].ToString();
                DataManager.playerData = SaveSystem.ResetPlayerData(campaign + "PlayerData");
            }
            else
            {
                DataManager.playerData = SaveSystem.defaultPlayerData.Copy(new PlayerData());
            }
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
            teamSwitcher.MasterUpdateRosters();
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
}
