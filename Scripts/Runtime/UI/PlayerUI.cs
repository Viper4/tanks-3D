using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] PlayerControl playerControl;
    [SerializeField] GameObject[] bulletIcons;
    [SerializeField] int iconIndex;
    [SerializeField] GameObject mineIcon;
    BaseUI baseUI;
    FireControl fireControl;
    MineControl mineControl;

    Transform bulletsLeftParent;
    Transform minesLeftParent;

    private void Start()
    {   
        baseUI = GetComponent<BaseUI>();
        fireControl = playerControl.GetComponent<FireControl>();
        mineControl = playerControl.GetComponent<MineControl>();

        if(!PhotonNetwork.OfflineMode)
        {
            if(playerControl.photonView.IsMine)
            {
                if(DataManager.roomSettings.mode != "Co-Op")
                {
                    baseUI.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\n" + PhotonNetwork.CurrentRoom.Name;
                    baseUI.UIElements["HUD"].Find("Level Background").GetChild(0).GetComponent<Text>().text = PhotonNetwork.CurrentRoom.Name;
                }
                else
                {
                    baseUI.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\n" + PhotonNetwork.CurrentRoom.Name + "\n" + GameManager.Instance.currentScene.name;
                    baseUI.UIElements["HUD"].Find("Level Background").GetChild(0).GetComponent<Text>().text = PhotonNetwork.CurrentRoom.Name + " | " + GameManager.Instance.currentScene.name;
                }
            }
        }
        else
        {
            baseUI.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Game Paused\n " + GameManager.Instance.currentScene.name;
            baseUI.UIElements["HUD"].Find("Level Background").GetChild(0).GetComponent<Text>().text = GameManager.Instance.currentScene.name;
        }

        bulletsLeftParent = baseUI.UIElements["HUD"].Find("Bullets Left");
        minesLeftParent = baseUI.UIElements["HUD"].Find("Mines Left");
        PhotonChatController.Instance.Resume();
        Resume();
    }

    private void LateUpdate()
    {
        if(PhotonNetwork.OfflineMode || playerControl.photonView.IsMine)
        {
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if (PhotonChatController.Instance.chatBoxActive)
                {
                    PhotonChatController.Instance.Resume();
                }
                else
                {
                    if (baseUI.UIElements["PauseMenu"].gameObject.activeSelf)
                    {
                        Resume();
                    }
                    else
                    {
                        Pause();
                    }
                }
            }

            if(Input.GetKeyDown(DataManager.playerSettings.keyBinds["Shoot"]))
            {
                RectTransform rt = baseUI.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(rt.sizeDelta.x * 1.25f, rt.sizeDelta.y * 1.25f);
            }
            else if(Input.GetKeyUp(DataManager.playerSettings.keyBinds["Shoot"]))
            {
                RectTransform rt = baseUI.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(rt.sizeDelta.x / 1.25f, rt.sizeDelta.y / 1.25f);
            }

            if(playerControl.showHUD)
            {
                baseUI.UIElements["HUD"].gameObject.SetActive(true);

                // Blacking out used bullets and mines
                int bulletLimit = fireControl.bulletLimit;
                int bulletsLeft = bulletLimit - fireControl.firedBullets.Count;
                for(int i = 0; i < bulletLimit; i++)
                {
                    if(i < bulletsLeft)
                    {
                        bulletsLeftParent.GetChild(i).GetComponent<Image>().color = Color.white;
                    }
                    else
                    {
                        bulletsLeftParent.GetChild(i).GetComponent<Image>().color = Color.black;
                    }
                }

                int mineLimit = mineControl.mineLimit;
                int minesLeft = mineLimit - mineControl.laidMines.Count;
                for(int i = 0; i < mineLimit; i++)
                {
                    if(i < minesLeft)
                    {
                        minesLeftParent.GetChild(i).GetComponent<Image>().color = Color.white;
                    }
                    else
                    {
                        minesLeftParent.GetChild(i).GetComponent<Image>().color = Color.black;
                    }
                }

                baseUI.UIElements["HUD"].Find("Kills").GetComponent<Text>().text = "Kills: " + DataManager.playerData.kills;
                if(!PhotonNetwork.OfflineMode)
                {
                    baseUI.UIElements["HUD"].Find("Lives").GetComponent<Text>().text = "Deaths: " + DataManager.playerData.deaths;
                }
                else
                {
                    baseUI.UIElements["HUD"].Find("Lives").GetComponent<Text>().text = "Lives: " + DataManager.playerData.lives;
                }
            }
            else
            {
                baseUI.UIElements["HUD"].gameObject.SetActive(false);
            }
        }
    }

    public void ChangeBulletIconIndex(int index)
    {
        foreach(Transform child in bulletsLeftParent)
        {
            Destroy(child.gameObject);
        }
        iconIndex = index;
        for(int i = 0; i < fireControl.bulletLimit; i++)
        {
            Instantiate(bulletIcons[iconIndex], bulletsLeftParent);
        }
    }

    public void UpdateBulletIcons()
    {
        if(bulletsLeftParent.childCount > fireControl.bulletLimit)
        {
            for(int i = fireControl.bulletLimit; i < bulletsLeftParent.childCount; i++)
            {
                Destroy(bulletsLeftParent.GetChild(i).gameObject);
            }
        }
        else if(bulletsLeftParent.childCount < fireControl.bulletLimit)
        {
            for(int i = 0; i < fireControl.bulletLimit - bulletsLeftParent.childCount; i++)
            {
                Instantiate(bulletIcons[iconIndex], bulletsLeftParent);
            }
        }
    }

    public void UpdateMineIcons()
    {
        if(minesLeftParent.childCount > mineControl.mineLimit)
        {
            for(int i = mineControl.mineLimit; i < minesLeftParent.childCount; i++)
            {
                Destroy(minesLeftParent.GetChild(i).gameObject);
            }
        }
        else if(minesLeftParent.childCount < mineControl.mineLimit)
        {
            for(int i = 0; i < mineControl.mineLimit - minesLeftParent.childCount; i++)
            {
                Instantiate(mineIcon, minesLeftParent);
            }
        }
    }

    public void Resume()
    {
        GameManager.Instance.paused = false;
        baseUI.UIElements["PauseMenu"].gameObject.SetActive(false);
        baseUI.UIElements["Settings"].gameObject.SetActive(false);
        if(baseUI.UIElements.ContainsKey("Change Teams"))
        {
            baseUI.UIElements["Change Teams"].gameObject.SetActive(false);
        }
        if(!PhotonNetwork.OfflineMode)
        {
            baseUI.UIElements["InGame"].gameObject.SetActive(true);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else if (!GameManager.Instance.loadingScreen.gameObject.activeSelf)
        {
            Time.timeScale = 1;
            baseUI.UIElements["InGame"].gameObject.SetActive(true);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void Pause()
    {
        GameManager.Instance.paused = true;
        if(PhotonNetwork.OfflineMode)
        {
            Time.timeScale = 0;
        }
        baseUI.UIElements["InGame"].gameObject.SetActive(false);
        baseUI.UIElements["PauseMenu"].gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Leave()
    {
        PhotonNetwork.LeaveRoom();
    }
}
