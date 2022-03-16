using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public class UIHandler : MonoBehaviour
{
    public Dictionary<string, Transform> UIElements = new Dictionary<string, Transform>();
    [SerializeField] PlayerControl playerControl;
    [SerializeField] CameraControl cameraControl;

    Transform selectedKeyBind;

    [SerializeField] ForwardRendererData forwardRenderer;

    public bool silhouettes = true;

    private void Awake()
    {
        if(playerControl == null)
        {
            playerControl = GameObject.Find("Player").GetComponent<PlayerControl>();
        }

        if (cameraControl == null)
        {
            cameraControl = GameObject.Find("Player").GetComponent<CameraControl>();
        }

        foreach (Transform child in transform)
        {
            UIElements[child.name] = child;

            if(child.name != "InGame")
            {
                child.gameObject.SetActive(false);
            }
        }
        UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Game Paused\nLevel " + (SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void Update()
    {
        if (!UIElements["Settings"].gameObject.activeSelf)
        {
            selectedKeyBind = null;
        }

        if (Input.GetKeyDown(playerControl.keyBinds["Shoot"]) || Input.GetMouseButtonDown(0))
        {
            RectTransform rt = UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x * 1.25f, rt.sizeDelta.y * 1.25f);
        }
        else if (Input.GetKeyUp(playerControl.keyBinds["Shoot"]) || Input.GetMouseButtonUp(0))
        {
            RectTransform rt = UIElements["InGame"].Find("Reticle").GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x / 1.25f, rt.sizeDelta.y / 1.25f);
        }
    }

    void OnGUI()
    {
        Event e = Event.current;
        if (selectedKeyBind != null && e.isKey)
        {
            transform.parent.GetComponent<PlayerControl>().keyBinds[selectedKeyBind.name] = e.keyCode;
            selectedKeyBind.Find("Button").GetChild(0).GetComponent<Text>().text = e.keyCode.ToString();
            selectedKeyBind = null;
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
        Time.timeScale = 0;
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

    public void ChangeKeyBind(Transform keyBind)
    {
        selectedKeyBind = keyBind;
    }

    public void ChangeSensitivity(Slider slider)
    {
        cameraControl.sensitivity = slider.value;
    }

    public void ToggleSilhouettes(Toggle toggle)
    {
        silhouettes = toggle.isOn;

        foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
        {
            if (feature.name == "TankHidden" || feature.name == "BulletHidden")
            {
                feature.SetActive(silhouettes);
            }
        }
    }

    public void SaveSettings()
    {
        SaveSystem.SaveSettings("settings.json");
    }

    public void LoadSettings()
    {
        SaveSystem.LoadSettings("settings.json");
    }

    public void UpdateSettingsUI()
    {
        // Updating renderer features
        foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
        {
            if (feature.name == "TankHidden" || feature.name == "BulletHidden")
            {
                feature.SetActive(silhouettes);
            }
        }
        // Updating UI elements in settings
        foreach (Transform content in UIElements["Settings"].Find("Scroll View").Find("Viewport"))
        {
            switch (content.name)
            {
                case "Game":
                    foreach (Transform setting in content)
                    {
                        switch (setting.name)
                        {
                            case "Sensitivity":
                                setting.GetComponent<Slider>().value = cameraControl.sensitivity;
                                break;
                        }
                    }
                    break;
                case "Keybinds":
                    foreach (Transform keybind in content)
                    {
                        keybind.Find("Button").GetChild(0).GetComponent<Text>().text = playerControl.keyBinds[keybind.name].ToString();
                    }
                    break;
                case "Video":
                    foreach (Transform setting in content)
                    {
                        switch (setting.name)
                        {
                            case "Silhouettes":
                                setting.GetComponent<Toggle>().isOn = silhouettes;
                                break;
                        }
                    }
                    break;
                case "Audio":

                    break;
            }
        }
    }

    public void SwitchScrollContent(RectTransform newContent)
    {
        ScrollRect scrollView = newContent.parent.parent.GetComponent<ScrollRect>();

        scrollView.content.gameObject.SetActive(false);
        newContent.gameObject.SetActive(true);
        scrollView.content = newContent;
    }
}
