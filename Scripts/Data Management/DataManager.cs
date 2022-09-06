using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using MyUnityAddons.CustomPhoton;

public class DataManager : MonoBehaviourPun
{
    public static PlayerSettings playerSettings = new PlayerSettings();
    public static RoomSettings roomSettings = new RoomSettings();
    public static PlayerData playerData = new PlayerData();

    private void Start()
    {
        playerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings", transform);
        roomSettings = SaveSystem.LoadRoomSettings(SaveSystem.LatestFileInSaveFolder(false, ".roomsettings"));

        RegisterCustomTypes();
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.autoPlay && !GameManager.frozen && Time.timeScale > 0)
        {
            playerData.time += Time.deltaTime;
        }
    }

    void RegisterCustomTypes()
    {
        PhotonPeer.RegisterType(typeof(Transform), (byte)'T', PhotonDataSerialization.ObjectToByteArray, PhotonDataSerialization.ByteArrayToObject);
        PhotonPeer.RegisterType(typeof(RoomSettings), (byte)'R', PhotonDataSerialization.ObjectToByteArray, PhotonDataSerialization.ByteArrayToObject);
    }
}
