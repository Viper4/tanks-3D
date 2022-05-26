using System.Collections;
using System.Collections.Generic;
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
        if (Input.GetKeyDown(playerControl.keyBinds["Shoot"]))
        {
            RectTransform rt = BaseUIHandler.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x * 1.25f, rt.sizeDelta.y * 1.25f);
        }
        else if (Input.GetKeyUp(playerControl.keyBinds["Shoot"]))
        {
            RectTransform rt = BaseUIHandler.UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x / 1.25f, rt.sizeDelta.y / 1.25f);
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
