using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using System.Text.RegularExpressions;

public class PlayerUIHandler : MonoBehaviour
{
    [SerializeField] PlayerControl playerControl;
    BaseUIHandler baseUIHandler;

    private void Start()
    {   
        baseUIHandler = GetComponent<BaseUIHandler>();

        if (!PhotonNetwork.OfflineMode)
        {
            if (playerControl.clientManager.PV.IsMine)
            {

                if (((RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"]).primaryMode != "Co-Op")
                {
                    baseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\n" + PhotonNetwork.CurrentRoom.Name;
                    baseUIHandler.UIElements["HUD"].Find("Level").GetComponent<Text>().text = PhotonNetwork.CurrentRoom.Name;
                }
                else
                {
                    string sceneName = SceneManager.GetActiveScene().name;
                    baseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\n" + PhotonNetwork.CurrentRoom.Name + "\n" + sceneName;
                    baseUIHandler.UIElements["HUD"].Find("Level").GetComponent<Text>().text = PhotonNetwork.CurrentRoom.Name + " | " + sceneName;
                }

                GameObject[] allUIs = GameObject.FindGameObjectsWithTag("PlayerUI");
                foreach (GameObject UI in allUIs)
                {
                    if (UI != gameObject)
                    {
                        UI.SetActive(false);
                    }
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            string sceneName = SceneManager.GetActiveScene().name;
            baseUIHandler.UIElements["HUD"].Find("Level").GetComponent<Text>().text = sceneName;
            baseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Game Paused\n " + sceneName;
        }
    }

    private void LateUpdate()
    {
        if (PhotonNetwork.OfflineMode || playerControl.clientManager.PV.IsMine)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (baseUIHandler.UIElements["PauseMenu"].gameObject.activeSelf)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }

            if (Input.GetKeyDown(playerControl.myData.currentPlayerSettings.keyBinds["Shoot"]))
            {
                RectTransform rt = baseUIHandler.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(rt.sizeDelta.x * 1.25f, rt.sizeDelta.y * 1.25f);
            }
            else if (Input.GetKeyUp(playerControl.myData.currentPlayerSettings.keyBinds["Shoot"]))
            {
                RectTransform rt = baseUIHandler.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(rt.sizeDelta.x / 1.25f, rt.sizeDelta.y / 1.25f);
            }

            if (playerControl.showHUD)
            {
                baseUIHandler.UIElements["HUD"].gameObject.SetActive(true);

                // Blacking out used bullets and mines
                int bulletLimit = playerControl.GetComponent<FireControl>().bulletLimit;
                int bulletsLeft = bulletLimit - playerControl.GetComponent<FireControl>().bulletsFired;
                for (int i = 0; i < bulletLimit; i++)
                {
                    if (i < bulletsLeft)
                    {
                        baseUIHandler.UIElements["HUD"].Find("Bullets Left").GetChild(i).GetComponent<Image>().color = Color.white;
                    }
                    else
                    {
                        baseUIHandler.UIElements["HUD"].Find("Bullets Left").GetChild(i).GetComponent<Image>().color = Color.black;
                    }
                }

                int mineLimit = playerControl.GetComponent<MineControl>().mineLimit;
                int minesLeft = mineLimit - playerControl.GetComponent<MineControl>().minesLaid;
                for (int i = 0; i < mineLimit; i++)
                {
                    if (i < minesLeft)
                    {
                        baseUIHandler.UIElements["HUD"].Find("Mines Left").GetChild(i).GetComponent<Image>().color = Color.white;
                    }
                    else
                    {
                        baseUIHandler.UIElements["HUD"].Find("Mines Left").GetChild(i).GetComponent<Image>().color = Color.black;
                    }
                }

                baseUIHandler.UIElements["HUD"].Find("Kills").GetComponent<Text>().text = "Kills: " + playerControl.myData.currentPlayerData.kills;
                if (!PhotonNetwork.OfflineMode)
                {
                    baseUIHandler.UIElements["HUD"].Find("Deaths").GetComponent<Text>().text = "Deaths: " + playerControl.myData.currentPlayerData.deaths;
                }
                else
                {
                    baseUIHandler.UIElements["HUD"].Find("Lives").GetComponent<Text>().text = "Lives: " + playerControl.myData.currentPlayerData.lives;
                }
            }
            else
            {
                baseUIHandler.UIElements["HUD"].gameObject.SetActive(false);
            }
        }
    }
    
    public void Resume()
    {
        playerControl.Paused = false;
        baseUIHandler.UIElements["PauseMenu"].gameObject.SetActive(false);
        baseUIHandler.UIElements["Settings"].gameObject.SetActive(false);
        if (!PhotonNetwork.OfflineMode)
        {
            baseUIHandler.UIElements["InGame"].gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (!GameManager.gameManager.loadingScreen.gameObject.activeSelf)
            {
                Time.timeScale = 1;
                baseUIHandler.UIElements["InGame"].gameObject.SetActive(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void Pause()
    {
        playerControl.Paused = true;
        if (PhotonNetwork.OfflineMode)
        {
            Time.timeScale = 0;
        }
        baseUIHandler.UIElements["InGame"].gameObject.SetActive(false);
        baseUIHandler.UIElements["PauseMenu"].gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Leave()
    {
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("Lobby");
    }
}
