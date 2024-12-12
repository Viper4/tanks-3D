using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using MyUnityAddons.CustomPhoton;

public class DataManager : MonoBehaviourPun
{
    static DataManager Instance;

    public static ChatSettings chatSettings = new ChatSettings();
    public static PlayerSettings playerSettings = new PlayerSettings();
    public static RoomSettings roomSettings = new RoomSettings();
    public static PlayerData playerData = new PlayerData();

    private void Awake()
    {
        if(Instance == null)
        {
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "us";
            chatSettings = SaveSystem.LoadChatSettings("ChatSettings");
            playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings");
            string latestRoomSettingsFile = SaveSystem.LatestFileInSaveFolder(false, ".roomsettings");
            if(latestRoomSettingsFile != null)
            {
                roomSettings = SaveSystem.LoadRoomSettings(latestRoomSettingsFile);
            }
            else
            {
                roomSettings = SaveSystem.defaultRoomSettings;
                SaveSystem.SaveRoomSettings(roomSettings, "DefaultRoomSettings");
            }

            RegisterCustomTypes();

            Instance = this;
        }
        else if(!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["roomSettings"];
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!GameManager.Instance.inLobby && !GameManager.Instance.frozen && Time.timeScale > 0)
        {
            playerData.time += Time.deltaTime;
        }
    }

    void RegisterCustomTypes()
    {
        PhotonPeer.RegisterType(typeof(Transform), (byte)'T', PhotonDataSerialization.ObjectToByteArray, PhotonDataSerialization.ByteArrayToObject);
        PhotonPeer.RegisterType(typeof(RoomSettings), (byte)'R', PhotonDataSerialization.ObjectToByteArray, PhotonDataSerialization.ByteArrayToObject);
        PhotonPeer.RegisterType(typeof(List<LevelObjectInfo>), (byte)'L', PhotonDataSerialization.ObjectToByteArray, PhotonDataSerialization.ByteArrayToObject);
    }
}
