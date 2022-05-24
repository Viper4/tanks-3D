using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIHandler : MonoBehaviour
{
    public Dictionary<string, Transform> UIElements = new Dictionary<string, Transform>();
    [SerializeField] PlayerControl playerControl;

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            UIElements[child.name] = child;

            child.gameObject.SetActive(false);
        }
        
        if (playerControl == null)
        {
            playerControl = GameObject.Find("Player").GetComponent<PlayerControl>();
        }

        if (cameraControl == null)
        {
            cameraControl = GameObject.Find("Player").GetComponent<CameraControl>();
        }

        UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Game Paused\nLevel " + (SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void Update()
    {
        if (Input.GetKeyDown(playerControl.keyBinds["Shoot"]))
        {
            RectTransform rt = UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x * 1.25f, rt.sizeDelta.y * 1.25f);
        }
        else if (Input.GetKeyUp(playerControl.keyBinds["Shoot"]))
        {
            RectTransform rt = UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x / 1.25f, rt.sizeDelta.y / 1.25f);
        }
    }
    
    public void Resume()
    {
        UIElements["InGame"].gameObject.SetActive(true);
        UIElements["PauseMenu"].gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    public void Pause()
    {
        UIElements["InGame"].gameObject.SetActive(false);
        UIElements["PauseMenu"].gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0;
    }
}
