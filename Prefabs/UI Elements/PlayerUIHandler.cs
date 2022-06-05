using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUIHandler : MonoBehaviour
{
    [SerializeField] PlayerControl playerControl;
    BaseUIHandler baseUIHandler;

    private void Start()
    {   
        baseUIHandler = GetComponent<BaseUIHandler>();

        baseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Game Paused\n " + SceneManager.GetActiveScene().name;

        if (playerControl.multiplayerManager.inMultiplayer)
        {
            if (playerControl.multiplayerManager.ViewIsMine())
            {
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
                baseUIHandler.UIElements["InGame"].gameObject.SetActive(false);
            }
        }
    }

    private void LateUpdate()
    {
        if (playerControl.multiplayerManager.ViewIsMine())
        {
            if (Input.GetKeyDown(playerControl.dataSystem.currentSettings.keyBinds["Shoot"]))
            {
                RectTransform rt = baseUIHandler.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(rt.sizeDelta.x * 1.25f, rt.sizeDelta.y * 1.25f);
            }
            else if (Input.GetKeyUp(playerControl.dataSystem.currentSettings.keyBinds["Shoot"]))
            {
                RectTransform rt = baseUIHandler.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(rt.sizeDelta.x / 1.25f, rt.sizeDelta.y / 1.25f);
            }

            if (playerControl.dataSystem.currentSettings.showHUD)
            {
                baseUIHandler.UIElements["HUD"].Find("Level").GetComponent<Text>().text = SceneManager.GetActiveScene().name;

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

                if (playerControl.multiplayerManager.inMultiplayer)
                {
                    baseUIHandler.UIElements["HUD"].Find("Kills").GetComponent<Text>().text = "Kills: " + playerControl.dataSystem.currentPlayerData.kills;
                    baseUIHandler.UIElements["HUD"].Find("Deaths").GetComponent<Text>().text = "Deaths: " + playerControl.dataSystem.currentPlayerData.deaths;
                }
                else
                {
                    baseUIHandler.UIElements["HUD"].Find("Lives").GetComponent<Text>().text = "Lives: " + playerControl.dataSystem.currentPlayerData.lives;
                }
            }
        }
    }
    
    public void Resume()
    {
        playerControl.Paused = false;
        if (!SceneLoader.sceneLoader.loadingScreen.gameObject.activeSelf)
        {
            Time.timeScale = 1;
            baseUIHandler.UIElements["InGame"].gameObject.SetActive(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        baseUIHandler.UIElements["PauseMenu"].gameObject.SetActive(false);
        baseUIHandler.UIElements["Settings"].gameObject.SetActive(false);
    }

    public void Pause()
    {
        playerControl.Paused = true;
        if (!playerControl.multiplayerManager.inMultiplayer)
        {
            Time.timeScale = 0;
        }
        baseUIHandler.UIElements["InGame"].gameObject.SetActive(false);
        baseUIHandler.UIElements["PauseMenu"].gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Disconnect()
    {
        playerControl.multiplayerManager.Disconnect();
    }
}
