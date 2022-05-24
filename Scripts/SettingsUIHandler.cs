using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public class SettingsUIHandler : MonoBehaviour
{
    public Dictionary<string, Transform> UIElements = new Dictionary<string, Transform>();
    [SerializeField] CameraControl cameraControl;
    [SerializeField] ForwardRendererData forwardRenderer;
    Transform selectedKeyBind;

    public bool silhouettes = true;

    readonly KeyCode[] mouseKeyCodes = { KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Mouse2, KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6 };

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            UIElements[child.name] = child;

            child.gameObject.SetActive(false);
        }
        
        UIElements["MainMenu"].gameObject.SetActive(true);
    }

    private void Update()
    {
        Event currentEvent = new Event();

        if (selectedKeyBind != null && Event.PopEvent(currentEvent))
        {
            if (currentEvent.isKey)
            {
                transform.parent.GetComponent<PlayerControl>().keyBinds[selectedKeyBind.name] = currentEvent.keyCode;
                selectedKeyBind.Find("Button").GetChild(0).GetComponent<Text>().text = currentEvent.keyCode.ToString();
            }
            else if (currentEvent.isMouse)
            {
                transform.parent.GetComponent<PlayerControl>().keyBinds[selectedKeyBind.name] = mouseKeyCodes[currentEvent.button];
                selectedKeyBind.Find("Button").GetChild(0).GetComponent<Text>().text = mouseKeyCodes[currentEvent.button].ToString();
            }
            selectedKeyBind = null;
        }
    }

    public void ChangeKeyBind(Transform keyBind)
    {
        StartCoroutine(DelayChangeKeyBind(keyBind));
    }
    
    IEnumerator DelayChangeKeyBind(Transform keyBind)
    {
        yield return new WaitWhile(() => Input.GetMouseButtonDown(0));
        selectedKeyBind = keyBind;
    }

    public void ChangeSensitivity(Slider slider)
    {
        cameraControl.sensitivity = slider.value;
        slider.transform.Find("Value Text").GetComponent<Text>().text = slider.value.ToString();
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
