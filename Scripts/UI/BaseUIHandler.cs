using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public void LoadNextScene(float delay)
    {
        GameManager.gameManager.LoadNextScene(delay);
    }

    public void LoadScene(string sceneName)
    {
        if (PhotonNetwork.OfflineMode)
        {
            GameManager.gameManager.LoadScene(sceneName, 0, false, false);
        }
        else
        {
            Hashtable parameters = new Hashtable
            {
                { "sceneName", sceneName },
                { "delay", 0 },
                { "save", false },
                { "waitWhilePaused", false }
            };
            PhotonNetwork.RaiseEvent(GameManager.gameManager.LoadSceneEventCode, parameters, Photon.Realtime.RaiseEventOptions.Default, ExitGames.Client.Photon.SendOptions.SendUnreliable);
        }
    }

    public void MainMenu()
    {
        if (!PhotonNetwork.OfflineMode)
        {
            PhotonNetwork.Disconnect();
        }
        GameManager.gameManager.LoadScene("Main Menu", 0, false, false);
    }

    public void ResetPlayerData()
    {
        SaveSystem.ResetPlayerData("PlayerData");
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
