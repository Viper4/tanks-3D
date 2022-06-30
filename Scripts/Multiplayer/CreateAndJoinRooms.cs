using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{
    [SerializeField] DataManager dataManager;

    public InputField createInput;
    public InputField joinInput;
    public InputField usernameInput;

    Coroutine createPlaceholderRoutine;
    Coroutine joinPlaceholderRoutine;
    Coroutine usernamePlaceholderRoutine;

    public void CreateRoom()
    {
        if (CreateInputIsValid() && UsernameInputIsValid())
        {
            RoomOptions roomOptions = new RoomOptions
            {
                PublishUserId = true,
            };
            PhotonNetwork.CreateRoom(createInput.text, roomOptions);
        }
    } 
    
    public void JoinRoom()
    {
        if (JoinInputIsValid() && UsernameInputIsValid())
        {
            PhotonNetwork.JoinRoom(joinInput.text);
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
            if (createPlaceholderRoutine != null)
            {
                StopCoroutine(createPlaceholderRoutine);
            }
            createPlaceholderRoutine = StartCoroutine(ShowPlaceholderError(createInput, "Room name is empty", 2.5f));
            return false;
        }
        return true;
    }

    bool JoinInputIsValid()
    {
        if (joinInput.text.Length == 0)
        {
            if (joinPlaceholderRoutine != null)
            {
                StopCoroutine(joinPlaceholderRoutine);
            }
            joinPlaceholderRoutine = StartCoroutine(ShowPlaceholderError(joinInput, "Room name is empty", 2.5f));
            return false;
        }
        return true;
    }

    bool UsernameInputIsValid()
    {
        if (usernameInput.text.Length == 0)
        {
            if (usernamePlaceholderRoutine != null)
            {
                StopCoroutine(usernamePlaceholderRoutine);
            }
            usernamePlaceholderRoutine = StartCoroutine(ShowPlaceholderError(usernameInput, "Username is empty", 2.5f));
            return false;
        }
        return true;
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.NickName = usernameInput.text;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.AutomaticallySyncScene = true;

            PhotonHashtable roomProperties = new PhotonHashtable();
            roomProperties.Add("RoomSettings", dataManager.currentRoomSettings);
            //roomProperties.Add("Waiting", true);
            roomProperties.Add("Waiting", false);
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

            StartCoroutine(DelayedPhotonLoad()); // Have to wait until CustomProperties are synched and updated across the network
        }
        else
        {
            if ((bool)PhotonNetwork.CurrentRoom.CustomProperties["Waiting"])
            {
                PhotonNetwork.LoadLevel("Waiting Room");
            }
            else
            {
                PhotonNetwork.LoadLevel(((RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"]).map);
            }
        }
    }

    IEnumerator DelayedPhotonLoad()
    {
        yield return new WaitForSecondsRealtime(0.25f);
        PhotonNetwork.LoadLevel(dataManager.currentRoomSettings.map);
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

    public void LeaveLobby()
    {
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Disconnected: " + cause);
        GameManager.gameManager.LoadScene("Main Menu");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Failed to create room '" + createInput.text + "': " + message);
        if (createPlaceholderRoutine != null)
        {
            StopCoroutine(createPlaceholderRoutine);
        }
        createPlaceholderRoutine = StartCoroutine(ShowPlaceholderError(createInput, "Create failed", 2.5f));
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Failed to join room '" + joinInput.text + "': " + message);
        if (joinPlaceholderRoutine != null)
        {
            StopCoroutine(joinPlaceholderRoutine);
        }
        joinPlaceholderRoutine = StartCoroutine(ShowPlaceholderError(joinInput, "Join failed", 2.5f));
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Failed to join a random room: " + message);
        if (joinPlaceholderRoutine != null)
        {
            StopCoroutine(joinPlaceholderRoutine);
        }
        joinPlaceholderRoutine = StartCoroutine(ShowPlaceholderError(joinInput, "Join failed", 2.5f));
    }
}
