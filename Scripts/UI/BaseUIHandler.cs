using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class BaseUIHandler : MonoBehaviour
{
    public Dictionary<string, Transform> UIElements = new Dictionary<string, Transform>();

    [SerializeField] List<Transform> activeElements = new List<Transform>();

    private void Start()
    {
        foreach (Transform child in transform)
        {
            UIElements[child.name] = child;

            if (!activeElements.Contains(child))
            {
                child.gameObject.SetActive(false);
            }
            else
            {
                child.gameObject.SetActive(true);
            }
        }
        if (UIElements.ContainsKey("InGame"))
        {
            UIElements["HUD"] = UIElements["InGame"].Find("HUD");
            UIElements["Lock Turret"] = UIElements["HUD"].Find("Lock Turret");
            UIElements["Lock Camera"] = UIElements["HUD"].Find("Lock Camera");
            UIElements["Lock Turret"].gameObject.SetActive(false);
            UIElements["Lock Camera"].gameObject.SetActive(false);
        }
    }

    public bool PauseUIActive()
    {
        if (UIElements.ContainsKey("PauseMenu") && UIElements["PauseMenu"].gameObject.activeSelf)
        {
            return true;
        }

        if (UIElements.ContainsKey("Settings") && UIElements["Settings"].gameObject.activeSelf)
        {
            return true;
        }

        return false;
    }


    public void LoadScene(string sceneName)
    {
        GameManager.Instance.StopAllLoadRoutines();
        if (PhotonNetwork.OfflineMode)
        {
            GameManager.Instance.LoadScene(sceneName, 0, false, false);
        }
        else
        {
            PhotonHashtable parameters = new PhotonHashtable()
            {
                { "sceneName", sceneName },
                { "delay", 0 },
                { "save", false },
                { "waitWhilePaused", false }
            };
            PhotonNetwork.RaiseEvent(GameManager.Instance.LoadSceneEventCode, parameters, Photon.Realtime.RaiseEventOptions.Default, ExitGames.Client.Photon.SendOptions.SendUnreliable);
            GameManager.Instance.PhotonLoadScene(sceneName, 0, false, false);
        }
    }

    public void ResumeGame()
    {
        if (DataManager.playerData.sceneIndex != -1)
        {
            if (PhotonNetwork.OfflineMode)
            {
                GameManager.Instance.LoadScene(DataManager.playerData.sceneIndex, 0, false, false);
            }
            else
            {
                PhotonHashtable parameters = new PhotonHashtable()
                {
                    { "sceneIndex", DataManager.playerData.sceneIndex },
                    { "delay", 0 },
                    { "save", false },
                    { "waitWhilePaused", false }
                };
                PhotonNetwork.RaiseEvent(GameManager.Instance.LoadSceneEventCode, parameters, Photon.Realtime.RaiseEventOptions.Default, ExitGames.Client.Photon.SendOptions.SendUnreliable);
                GameManager.Instance.PhotonLoadScene(DataManager.playerData.sceneIndex, 0, false, false);
            }
        }
    }

    public void ResetPlayerData()
    {
        DataManager.playerData = SaveSystem.ResetPlayerData("PlayerData");
        if (!PhotonNetwork.OfflineMode && PhotonNetwork.IsMasterClient)
        {
            PhotonHashtable roomProperties = new PhotonHashtable()
            {
                { "Total Lives", ((RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"]).totalLives }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
            PhotonNetwork.RaiseEvent(GameManager.Instance.ResetDataCode, null, Photon.Realtime.RaiseEventOptions.Default, ExitGames.Client.Photon.SendOptions.SendUnreliable);
        }
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void ActivateElement(Transform element)
    {
        element.gameObject.SetActive(true);
    }

    public void DeactivateElement(Transform element)
    {
        element.gameObject.SetActive(false);
    }
}
