using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUIHandler : MonoBehaviour
{
    [SerializeField] DataSystem dataSystem;
    BaseUIHandler baseUIHandler;

    private void Start()
    {   
        baseUIHandler = GetComponent<BaseUIHandler>();

        baseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Game Paused\nLevel " + (SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void Update()
    {
        if (Input.GetKeyDown(dataSystem.currentSettings.keyBinds["Shoot"]))
        {
            RectTransform rt = baseUIHandler.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x * 1.25f, rt.sizeDelta.y * 1.25f);
        }
        else if (Input.GetKeyUp(dataSystem.currentSettings.keyBinds["Shoot"]))
        {
            RectTransform rt = baseUIHandler.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x / 1.25f, rt.sizeDelta.y / 1.25f);
        }

        if (dataSystem.currentSettings.showHUD)
        {
            baseUIHandler.UIElements["HUD"].Find("Level").GetComponent<Text>().text = "Level " + SceneManager.GetActiveScene().buildIndex;
            baseUIHandler.UIElements["HUD"].Find("Lives").GetComponent<Text>().text = "Lives: " + dataSystem.currentPlayerData.lives;

            // Blacking out used bullets and mines
            int bulletLimit = dataSystem.GetComponent<FireControl>().bulletLimit;
            int bulletsLeft = bulletLimit - dataSystem.GetComponent<FireControl>().bulletsFired;
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

            int mineLimit = dataSystem.GetComponent<MineControl>().mineLimit;
            int minesLeft = mineLimit - dataSystem.GetComponent<MineControl>().minesLaid;
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
        }
    }
    
    public void Resume()
    {
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
        baseUIHandler.UIElements["InGame"].gameObject.SetActive(false);
        baseUIHandler.UIElements["PauseMenu"].gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0;
    }
}
