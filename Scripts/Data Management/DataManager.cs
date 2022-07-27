using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using MyUnityAddons.CustomPhoton;

public class DataManager : MonoBehaviourPun, IPunObservable
{
    public PlayerSettings currentPlayerSettings = new PlayerSettings();
    public RoomSettings currentRoomSettings = new RoomSettings();
    public PlayerData currentPlayerData = new PlayerData();

    public bool timing = false;

    private void Start()
    {
        currentPlayerSettings = SaveSystem.LoadPlayerSettings("PlayerSettings", transform);
        currentRoomSettings = SaveSystem.LoadRoomSettings(SaveSystem.LatestFileInSaveFolder(false, ".roomsettings"));

        RegisterCustomTypes();
    }

    // Update is called once per frame
    void Update()
    {
        if (timing)
        {
            currentPlayerData.time += Time.deltaTime;
        }
    }

    void RegisterCustomTypes()
    {
        PhotonPeer.RegisterType(typeof(Transform), (byte)'T', PhotonDataSerialization.ObjectToByteArray, PhotonDataSerialization.ByteArrayToObject);
        PhotonPeer.RegisterType(typeof(RoomSettings), (byte)'R', PhotonDataSerialization.ObjectToByteArray, PhotonDataSerialization.ByteArrayToObject);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentPlayerData.kills);
            stream.SendNext(currentPlayerData.deaths);
        }
        else if (stream.IsReading)
        {
            currentPlayerData.kills = (int)stream.ReceiveNext();
            currentPlayerData.deaths = (int)stream.ReceiveNext();
        }
    }
}
