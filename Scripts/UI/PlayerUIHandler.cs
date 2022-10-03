using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PlayerUIHandler : MonoBehaviour
{
    [SerializeField] PlayerControl playerControl;
    BaseUIHandler baseUIHandler;

    private void Start()
    {   
        baseUIHandler = GetComponent<BaseUIHandler>();

        if (!PhotonNetwork.OfflineMode)
        {
            if (playerControl.photonView.IsMine)
            {
                if (((RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"]).primaryMode != "Co-Op")
                {
                    baseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\n" + PhotonNetwork.CurrentRoom.Name;
                    baseUIHandler.UIElements["HUD"].Find("Level Background").GetChild(0).GetComponent<Text>().text = PhotonNetwork.CurrentRoom.Name;
                }
                else
                {
                    baseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\n" + PhotonNetwork.CurrentRoom.Name + "\n" + GameManager.Instance.currentScene.name;
                    baseUIHandler.UIElements["HUD"].Find("Level Background").GetChild(0).GetComponent<Text>().text = PhotonNetwork.CurrentRoom.Name + " | " + GameManager.Instance.currentScene.name;
                }
            }
        }
        else
        {
            baseUIHandler.UIElements["HUD"].Find("Level Background").GetChild(0).GetComponent<Text>().text = GameManager.Instance.currentScene.name;
            baseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Game Paused\n " + GameManager.Instance.currentScene.name;
        }
    }

    private void LateUpdate()
    {
        if (PhotonNetwork.OfflineMode || playerControl.photonView.IsMine)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (baseUIHandler.UIElements["PauseMenu"].gameObject.activeSelf || baseUIHandler.UIElements["Settings"].gameObject.activeSelf)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }

            if (Input.GetKeyDown(DataManager.playerSettings.keyBinds["Shoot"]))
            {
                RectTransform rt = baseUIHandler.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(rt.sizeDelta.x * 1.25f, rt.sizeDelta.y * 1.25f);
            }
            else if (Input.GetKeyUp(DataManager.playerSettings.keyBinds["Shoot"]))
            {
                RectTransform rt = baseUIHandler.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(rt.sizeDelta.x / 1.25f, rt.sizeDelta.y / 1.25f);
            }

            if (playerControl.showHUD)
            {
                baseUIHandler.UIElements["HUD"].gameObject.SetActive(true);

                // Blacking out used bullets and mines
                int bulletLimit = playerControl.GetComponent<FireControl>().bulletLimit;
                int bulletsLeft = bulletLimit - playerControl.GetComponent<FireControl>().firedBullets.Count;
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
                int minesLeft = mineLimit - playerControl.GetComponent<MineControl>().laidMines.Count;
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

                baseUIHandler.UIElements["HUD"].Find("Kills").GetComponent<Text>().text = "Kills: " + DataManager.playerData.kills;
                if (!PhotonNetwork.OfflineMode)
                {
                    baseUIHandler.UIElements["HUD"].Find("Deaths").GetComponent<Text>().text = "Deaths: " + DataManager.playerData.deaths;
                }
                else
                {
                    baseUIHandler.UIElements["HUD"].Find("Lives").GetComponent<Text>().text = "Lives: " + DataManager.playerData.lives;
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
            if (!GameManager.Instance.loadingScreen.gameObject.activeSelf)
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
    }
}
