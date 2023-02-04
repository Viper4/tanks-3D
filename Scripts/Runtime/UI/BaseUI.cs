using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class BaseUI : MonoBehaviour
{
    public Dictionary<string, Transform> UIElements = new Dictionary<string, Transform>();

    [SerializeField] List<Transform> activeElements = new List<Transform>();

    private void Awake()
    {
        foreach(Transform child in transform)
        {
            UIElements[child.name] = child;

            if(!activeElements.Contains(child))
            {
                child.gameObject.SetActive(false);
            }
            else
            {
                child.gameObject.SetActive(true);
            }
        }
        if(UIElements.ContainsKey("InGame"))
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
        if(UIElements.ContainsKey("PauseMenu") && UIElements["PauseMenu"].gameObject.activeSelf)
        {
            return true;
        }

        if(UIElements.ContainsKey("Settings") && UIElements["Settings"].gameObject.activeSelf)
        {
            return true;
        }

        return false;
    }


    public void LoadScene(string sceneName)
    {
        GameManager.Instance.StopAllLoadRoutines();
        if(PhotonNetwork.OfflineMode)
        {
            GameManager.Instance.LoadScene(sceneName, 0, false, false);
        }
        else
        {
            GameManager.Instance.PhotonLoadScene(sceneName);
        }
    }

    public void ResumeGame()
    {
        string latestFile = SaveSystem.LatestFileInSaveFolder(false, ".playerdata");
        if(latestFile != null)
        {
            DataManager.playerData = SaveSystem.LoadPlayerData(latestFile);
            if(DataManager.playerData.sceneIndex != -1)
            {
                if(PhotonNetwork.OfflineMode)
                {
                    GameManager.Instance.LoadScene(DataManager.playerData.sceneIndex);
                }
                else
                {
                    GameManager.Instance.PhotonLoadScene(DataManager.playerData.sceneIndex);
                }
            }
        }
    }

    public void ResetPlayerData(string fileName)
    {
        DataManager.playerData = SaveSystem.ResetPlayerData(fileName);
        if(!PhotonNetwork.OfflineMode && PhotonNetwork.IsMasterClient)
        {
            PhotonHashtable roomProperties = new PhotonHashtable()
            {
                { "totalLives", ((RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["roomSettings"]).totalLives }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

            PhotonHashtable parameters = new PhotonHashtable()
            {
                { "fileName", fileName }
            };
            PhotonNetwork.RaiseEvent(EventCodes.ResetData, parameters, Photon.Realtime.RaiseEventOptions.Default, ExitGames.Client.Photon.SendOptions.SendUnreliable);
        }
    }

    public void MainMenu()
    {
        GameManager.Instance.MainMenu();
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
