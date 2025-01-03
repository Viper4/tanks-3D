using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class ConnectToServer : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.OfflineMode = false;
        PhotonNetwork.GameVersion = Application.version;
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.ConnectUsingSettings();
    }

    public void CancelConnect()
    {
        GameManager.Instance.canceledConnect = true;
        GameManager.Instance.LoadScene("Main Menu", 0, false, false);
    }

    public override void OnConnectedToMaster()
    {
        if(!GameManager.Instance.canceledConnect)
        {
            PhotonNetwork.JoinLobby();

            PhotonChatController.Instance.ConnectToPhotonChat();
            PhotonChatController.Instance.SubscribeToChannel("RegionLobby", 100, true);
        }
    }

    public override void OnJoinedLobby()
    {
        SceneManager.LoadScene("Lobby");
    }
}
