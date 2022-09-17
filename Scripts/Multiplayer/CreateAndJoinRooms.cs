using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using MyUnityAddons.CustomPhoton;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{
    public InputField createInput;
    public InputField joinInput;
    public InputField usernameInput;

    [SerializeField] Transform popup;
    Coroutine popupRoutine;

    public void CreateRoom()
    {
        if (CreateInputIsValid() && UsernameInputIsValid())
        {
            RoomOptions roomOptions = new RoomOptions
            {
                PublishUserId = true,
                CleanupCacheOnLeave = true,
                IsVisible = DataManager.roomSettings.isPublic,
            };
            PhotonNetwork.CreateRoom(createInput.text, roomOptions);
            PhotonHashtable playerProperties = new PhotonHashtable()
            {
                { "New", true }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
    } 
    
    public void JoinRoom()
    {
        if (JoinInputIsValid() && UsernameInputIsValid())
        {
            PhotonNetwork.JoinRoom(joinInput.text);
            PhotonHashtable playerProperties = new PhotonHashtable()
            {
                { "New", true }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
    }

    public void JoinRandomRoom()
    {
        if (UsernameInputIsValid())
        {
            PhotonNetwork.JoinRandomRoom();
        }
    }

    bool CreateInputIsValid()
    {
        if (createInput.text.Length == 0)
        {
            StartShowPopup("Room name is empty", 2.5f);
            return false;
        }
        return true;
    }

    bool JoinInputIsValid()
    {
        if (joinInput.text.Length == 0)
        {
            StartShowPopup("Room name is empty", 2.5f);
            return false;
        }
        return true;
    }

    bool UsernameInputIsValid()
    {
        if (usernameInput.text.Length == 0)
        {
            StartShowPopup("Username is empty", 2.5f);
            return false;
        }
        return true;
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.NickName = GetUniqueUsername(usernameInput.text);

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.AutomaticallySyncScene = true;

            PhotonHashtable roomProperties = new PhotonHashtable
            {
                { "RoomSettings", DataManager.roomSettings },
                { "Waiting", true },
                { "Ready Players", 0 },
                { "Total Lives", DataManager.roomSettings.totalLives }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

            StartCoroutine(DelayedPhotonLoad("Waiting Room"));
        }
        else
        {
            if ((bool)PhotonNetwork.CurrentRoom.CustomProperties["Waiting"])
            {
                PhotonNetwork.LoadLevel("Waiting Room");
            }
            else
            {
                Debug.Log(((RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"]).map);
                PhotonNetwork.LoadLevel(((RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"]).map);
            }
        }
    }

    string GetUniqueUsername(string username)
    {
        List<string> usernames = new List<string>();
        foreach (Player player in PhotonNetwork.PlayerListOthers)
        {
            usernames.Add(player.NickName);
        }

        if (usernames.Contains(username))
        {
            for (int i = 0; i < 20; i++)
            {
                string newUsername = username + " (" + (i + 1) + ")";
                if (!usernames.Contains(newUsername))
                {
                    return newUsername;
                }
            }
        }
        return username;
    }

    IEnumerator DelayedPhotonLoad(string sceneName)
    {
        yield return new WaitForSecondsRealtime(0.2f); // Wait until CustomProperties are synched and updated across the network
        PhotonNetwork.LoadLevel(sceneName);
    }

    IEnumerator ShowPlaceholderError(InputField input, string message, float delay)
    {
        Transform placeholderError = input.transform.Find("PlaceholderError");
        placeholderError.GetComponent<Text>().text = message;
        placeholderError.gameObject.SetActive(true);
        input.placeholder.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(delay);

        placeholderError.gameObject.SetActive(false);
        input.placeholder.gameObject.SetActive(true);
    }

    void StartShowPopup(string message, float delay)
    {
        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
        }
        popupRoutine = StartCoroutine(ShowPopup(message, delay));
    }

    IEnumerator ShowPopup(string message, float delay)
    {
        popup.gameObject.SetActive(true);
        popup.GetChild(0).GetComponent<TMP_Text>().text = message;
        yield return new WaitForSecondsRealtime(delay);
        popup.gameObject.SetActive(false);
        popupRoutine = null;
    }

    public void LeaveLobby()
    {
        PhotonNetwork.Disconnect();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        StartShowPopup("Create failed: " + message, 2.5f);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        StartShowPopup("Join failed: " + message, 2.5f);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        StartShowPopup("Join failed: " + message, 2.5f);
    }
}
