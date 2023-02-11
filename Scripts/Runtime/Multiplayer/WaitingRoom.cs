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
using TMPro;

public class WaitingRoom : MonoBehaviourPunCallbacks
{
    int readyPlayers = 0;
    [SerializeField] GameObject playerSlotPrefab;

    [SerializeField] GameObject[] ownerElements;

    [SerializeField] Text roomName;
    [SerializeField] Text mapName;

    [SerializeField] TeamSwitching teamSwitcher;

    [SerializeField] GameObject loadingScreen;
    [SerializeField] GameObject customLoadingScreen;
    [SerializeField] TextMeshProUGUI customLoadingLabel;
    [SerializeField] Slider customLoadingProgressBar;
    [SerializeField] TextMeshProUGUI progressBarText;

    LevelInfo tempLevelInfo;

    int currentPacket;
    int totalPackets;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        tempLevelInfo = new LevelInfo()
        {
            levelObjects = new List<LevelObjectInfo>(),
        };

        if(!PhotonNetwork.IsMasterClient)
        {
            foreach(GameObject UIElement in ownerElements)
            {
                if(UIElement.CompareTag("Mode Button"))
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
        loadingScreen.SetActive(false);
    }

    public void StartGame() // Accessable only by MasterClient
    {
        if(DataManager.roomSettings.mode == "Co-Op")
        {
            string campaign = Regex.Match(DataManager.roomSettings.map, @"(.*?)[ ][0-9]+$").Groups[1].ToString();
            DataManager.playerData = SaveSystem.ResetPlayerData(campaign + "PlayerData");
        }
        else
        {
            DataManager.playerData = SaveSystem.defaultPlayerData.Copy(new PlayerData());
        }

        if(DataManager.roomSettings.customMap)
        {
            customLoadingScreen.SetActive(true);
            customLoadingLabel.text = "Uploading Map...";
            
            PhotonHashtable roomProperties = new PhotonHashtable
            {
                { "waiting", false },
                { "roomSettings", DataManager.roomSettings },
                { "totalLives", DataManager.roomSettings.totalLives },
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

            if(PhotonNetwork.CurrentRoom.PlayerCount <= 1)
            {
                GameManager.Instance.LoadScene("Custom");
            }
            else
            {
                LevelInfo levelInfo = SaveSystem.LoadLevel(DataManager.roomSettings.map);
                int packetSize = 5;
                for (int i = 0; i < levelInfo.levelObjects.Count; i += packetSize)
                {
                    PhotonHashtable parameters = new PhotonHashtable()
                    {
                        { "packet", levelInfo.levelObjects.GetRange(i, Mathf.Min(packetSize, levelInfo.levelObjects.Count - i)) }
                    };
                    if (i == 0)
                    {
                        parameters.Add("start", Mathf.CeilToInt(levelInfo.levelObjects.Count / (float)packetSize));
                    }

                    PhotonNetwork.RaiseEvent(EventCodes.LevelObjectUpload, parameters, RaiseEventOptions.Default, SendOptions.SendReliable);
                }

                readyPlayers++;
            }
        }
        else
        {
            PhotonNetwork.RaiseEvent(EventCodes.LeaveWaitingRoom, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);

            PhotonHashtable roomProperties = new PhotonHashtable
            {
                { "waiting", false },
                { "roomSettings", DataManager.roomSettings },
                { "totalLives", DataManager.roomSettings.totalLives }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

            GameManager.Instance.PhotonLoadScene(DataManager.roomSettings.map);
        }
    } 

    public void ChangeMode(string mode) // Accessable only by MasterClient
    {
        if(DataManager.roomSettings.mode != mode)
        {
            DataManager.roomSettings.mode = mode;
            if(mode == "Co-Op")
            {
                if(DataManager.roomSettings.map != "Classic 1" && DataManager.roomSettings.map != "Regular 1")
                {
                    DataManager.roomSettings.map = "Classic 1";
                }
                DataManager.roomSettings.customMap = false;
            }
            else
            {
                if(DataManager.roomSettings.map == "Classic 1" || DataManager.roomSettings.map == "Regular 1")
                {
                    DataManager.roomSettings.map = "Classic";
                }
                DataManager.roomSettings.customMap = false;
            }
            UpdateRoomSettingsProperty();

            MasterUpdateAllUI();
        }
    }

    public void UpdateRoomSettingsProperty() // Accessable only by MasterClient
    {
        PhotonHashtable roomProperties = new PhotonHashtable
        {
            { "roomSettings", DataManager.roomSettings },
            { "totalLives", DataManager.roomSettings.totalLives }
        };
        PhotonNetwork.CurrentRoom.IsVisible = DataManager.roomSettings.isPublic;
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
    }

    public void MasterUpdateAllUI()
    {
        foreach(Player player in PhotonNetwork.PlayerList)
        {
            player.AllocatePlayerToTeam();
        }
        PhotonNetwork.RaiseEvent(EventCodes.UpdateUI, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        UpdateBasicUI();
        teamSwitcher.MasterUpdateRosters();
    }

    private void UpdateBasicUI()
    {
        if(!PhotonNetwork.IsMasterClient)
            DataManager.roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["roomSettings"];

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
        if(eventData.Code == EventCodes.UpdateUI)
        {
            UpdateBasicUI();
        }
        else if(eventData.Code == EventCodes.LeaveWaitingRoom)
        {
            DataManager.roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["roomSettings"];

            if(DataManager.roomSettings.mode == "Co-Op")
            {
                string campaign = Regex.Match(DataManager.roomSettings.map, @"(.*?)[ ][0-9]+$").Groups[1].ToString();
                DataManager.playerData = SaveSystem.ResetPlayerData(campaign + "PlayerData");
            }
            else
            {
                DataManager.playerData = SaveSystem.defaultPlayerData.Copy(new PlayerData());
            }

            if(DataManager.roomSettings.customMap)
            {
                GameManager.Instance.LoadScene("Custom");
            }
        }
        else if(eventData.Code == EventCodes.LevelObjectUpload)
        {
            customLoadingScreen.SetActive(true);
            customLoadingLabel.text = "Downloading Map...";

            PhotonHashtable parameters = (PhotonHashtable)eventData.Parameters[ParameterCode.Data];
            if(parameters.ContainsKey("start"))
            {
                currentPacket = 0;
                totalPackets = (int)parameters["start"];
            }

            currentPacket++;
            List<LevelObjectInfo> packet = (List<LevelObjectInfo>)parameters["packet"];
            foreach(LevelObjectInfo levelObjectInfo in packet)
            {
                tempLevelInfo.levelObjects.Add(levelObjectInfo);
            }
            if(currentPacket >= totalPackets)
            {
                customLoadingLabel.text = "Waiting On Others...";
                SaveSystem.SaveTempLevel(tempLevelInfo);
                PhotonNetwork.RaiseEvent(EventCodes.ReadyToLeave, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
            }
            float progress = currentPacket / (float)totalPackets;
            customLoadingProgressBar.SetValueWithoutNotify(progress);
            progressBarText.text = (Mathf.Round(progress * 10000) / 100) + "%";
        }
        else if(eventData.Code == EventCodes.ReadyToLeave)
        {
            if(PhotonNetwork.IsMasterClient)
            {
                int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
                readyPlayers++;
                float progress = readyPlayers / totalPlayers;
                customLoadingProgressBar.value = progress;
                progressBarText.text = (Mathf.Round(progress * 10000) / 100) + "%";
                if (readyPlayers == totalPlayers)
                {
                    PhotonNetwork.RaiseEvent(EventCodes.LeaveWaitingRoom, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
                    GameManager.Instance.LoadScene("Custom");
                }
            }
        }
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if(PhotonNetwork.IsMasterClient)
        {
            otherPlayer.LeaveCurrentTeam();
            teamSwitcher.MasterUpdateRosters();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if(PhotonNetwork.IsMasterClient)
        {
            foreach(GameObject UIElement in ownerElements)
            {
                if(UIElement.CompareTag("Mode Button"))
                {
                    UIElement.GetComponent<Button>().interactable = true;
                }
                else
                {
                    UIElement.SetActive(true);
                }
            }
        }
        else
        {
            foreach(GameObject UIElement in ownerElements)
            {
                if(UIElement.CompareTag("Mode Button"))
                {
                    UIElement.GetComponent<Button>().interactable = false;
                }
                else
                {
                    UIElement.SetActive(false);
                }
            }
        }
    }
}
