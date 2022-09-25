using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{
    public InputField createInput;
    public InputField joinInput;
    public InputField usernameInput;

    [SerializeField] RectTransform joiningOrCreating;

    [SerializeField] Transform popup;
    Coroutine popupRoutine;

    public void CreateRoom()
    {
        if (CreateInputIsValid() && UsernameInputIsValid())
        {
            PhotonHashtable playerProperties = new PhotonHashtable()
            {
                { "New", true }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
            PhotonHashtable roomProperties = new PhotonHashtable
            {
                { "RoomSettings", DataManager.roomSettings },
                { "Waiting", true },
                { "Ready Players", 0 },
                { "Total Lives", DataManager.roomSettings.totalLives }
            };
            RoomOptions roomOptions = new RoomOptions
            {
                PublishUserId = true,
                CleanupCacheOnLeave = true,
                IsVisible = DataManager.roomSettings.isPublic,
                IsOpen = true,
                CustomRoomProperties = roomProperties
            };

            Debug.Log(roomOptions.IsOpen + " " + roomOptions.IsVisible);

            PhotonNetwork.CreateRoom(createInput.text, roomOptions);

            joiningOrCreating.gameObject.SetActive(true);
            joiningOrCreating.Find("Label").GetComponent<TextMeshProUGUI>().text = "Creating Room...";
        }
    } 
    
    public void JoinRoom()
    {
        if (JoinInputIsValid() && UsernameInputIsValid())
        {
            PhotonHashtable playerProperties = new PhotonHashtable()
            {
                { "New", true }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
            PhotonNetwork.JoinRoom(joinInput.text);

            joiningOrCreating.gameObject.SetActive(true);
            joiningOrCreating.Find("Label").GetComponent<TextMeshProUGUI>().text = "Joining...";
        }
    }

    public void JoinRandomRoom()
    {
        if (UsernameInputIsValid())
        {

            if (PhotonNetwork.CountOfRooms > 0)
            {
                PhotonNetwork.JoinRandomRoom();

                joiningOrCreating.gameObject.SetActive(true);
                joiningOrCreating.Find("Label").GetComponent<TextMeshProUGUI>().text = "Joining...";
            }
            else
            {
                StartShowPopup("No rooms to join", 2.5f);
            }
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
            PhotonNetwork.LoadLevel("Waiting Room");
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
        PhotonNetwork.AutomaticallySyncScene = true;
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
        joiningOrCreating.gameObject.SetActive(false);

        StartShowPopup("Create failed: " + message, 2.5f);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        joiningOrCreating.gameObject.SetActive(false);

        StartShowPopup("Join failed: " + message, 2.5f);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        joiningOrCreating.gameObject.SetActive(false);

        StartShowPopup("Join failed: " + message, 2.5f);
        Debug.Log(returnCode);
    }
}
