using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using CustomExtensions;

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

        if (Input.GetKeyDown(KeyCode.L))
        {
            byte[] byteArray = PhotonExtensions.ObjectToByteArray(currentRoomSettings);

            RoomSettings deserializedRoomSettings = (RoomSettings)PhotonExtensions.ByteArrayToObject(byteArray);
            Debug.Log(deserializedRoomSettings.map);
        }
    }

    void RegisterCustomTypes()
    {
        PhotonPeer.RegisterType(typeof(Transform), (byte)'T', PhotonExtensions.ObjectToByteArray, PhotonExtensions.ByteArrayToObject);
        PhotonPeer.RegisterType(typeof(RoomSettings), (byte)'R', PhotonExtensions.ObjectToByteArray, PhotonExtensions.ByteArrayToObject);
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
            currentPlayerData.kills = (int)stream.ReceiveNext();
        }
    }
}

[System.Serializable]
public struct SerializableTransform
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 localScale;
}
