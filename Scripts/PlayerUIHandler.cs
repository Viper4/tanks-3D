using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUIHandler : MonoBehaviour
{
    [SerializeField] PlayerControl playerControl;

    private void Awake()
    {   
        if (playerControl == null)
        {
            playerControl = GameObject.Find("Player").GetComponent<PlayerControl>();
        }

        BaseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Game Paused\nLevel " + (SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void Update()
    {
        if (Input.GetKeyDown(SaveSystem.currentSettings.keyBinds["Shoot"]))
        {
            RectTransform rt = BaseUIHandler.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x * 1.25f, rt.sizeDelta.y * 1.25f);
        }
        else if (Input.GetKeyUp(SaveSystem.currentSettings.keyBinds["Shoot"]))
        {
            RectTransform rt = BaseUIHandler.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x / 1.25f, rt.sizeDelta.y / 1.25f);
        }

        if (SaveSystem.currentSettings.showHUD)
        {
            BaseUIHandler.UIElements["HUD"].Find("Level").GetComponent<Text>().text = "Level " + SceneManager.GetActiveScene().buildIndex;
            BaseUIHandler.UIElements["HUD"].Find("Lives").GetComponent<Text>().text = "Lives: " + SaveSystem.currentPlayerData.lives;

            // Blacking out used bullets and mines
            int bulletLimit = playerControl.GetComponent<FireControl>().bulletLimit;
            int bulletsLeft = bulletLimit - playerControl.GetComponent<FireControl>().bulletsFired;
            for (int i = 0; i < bulletLimit; i++)
            {
                if (i < bulletsLeft)
                {
                    BaseUIHandler.UIElements["HUD"].Find("Bullets Left").GetChild(i).GetComponent<Image>().color = Color.white;
                }
                else
                {
                    BaseUIHandler.UIElements["HUD"].Find("Bullets Left").GetChild(i).GetComponent<Image>().color = Color.black;
                }
            }

            int mineLimit = playerControl.GetComponent<MineControl>().mineLimit;
            int minesLeft = mineLimit - playerControl.GetComponent<MineControl>().minesLaid;
            for (int i = 0; i < mineLimit; i++)
            {
                if (i < minesLeft)
                {
                    BaseUIHandler.UIElements["HUD"].Find("Mines Left").GetChild(i).GetComponent<Image>().color = Color.white;
                }
                else
                {
                    BaseUIHandler.UIElements["HUD"].Find("Mines Left").GetChild(i).GetComponent<Image>().color = Color.black;
                }
            }
        }
    }
    
    public void Resume()
    {
        BaseUIHandler.UIElements["InGame"].gameObject.SetActive(true);
        BaseUIHandler.UIElements["PauseMenu"].gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    public void Pause()
    {
        BaseUIHandler.UIElements["InGame"].gameObject.SetActive(false);
        BaseUIHandler.UIElements["PauseMenu"].gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0;
    }
}
