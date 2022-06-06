using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class ClientManager : MonoBehaviourPunCallbacks
{
    public bool inMultiplayer = false;

    [SerializeField] PhotonView view;
    [SerializeField] DataSystem dataSystem;

    // Start is called before the first frame update
    void Start()
    {
        if (inMultiplayer)
        {
            if (view.IsMine)
            {
                // Making sure other players can see this client's kills, deaths, etc
                view.RPC("LoadPlayerDataForAll", RpcTarget.All);
            }

            EngineSoundManager[] allEngineSounds = FindObjectsOfType<EngineSoundManager>();
            foreach (EngineSoundManager engineSound in allEngineSounds)
            {
                engineSound.UpdateMasterVolume(dataSystem.currentSettings.masterVolume);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateSoundManagerOnClient(SoundManager soundManager)
    {
        soundManager.UpdateVolume(dataSystem.currentSettings.masterVolume);
    }

    [PunRPC]
    void LoadPlayerDataForAll()
    {
        SaveSystem.LoadPlayerData("PlayerData.json", dataSystem.currentPlayerData);
    }

    public bool ViewIsMine()
    {
        return view.IsMine;
    }

    public void Disconnect()
    {
        if (view.IsMine)
        {
            PhotonNetwork.Disconnect();
            SceneLoader.sceneLoader.LoadScene("Main Menu");
        }
    }
}
