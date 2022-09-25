using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class SettingsUIHandler : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] UniversalRendererData forwardRenderer;
    [SerializeField] BaseUIHandler baseUIHandler;
    [SerializeField] Camera myCamera;
    Transform selectedKeyBind;

    readonly KeyCode[] mouseKeyCodes = { KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Mouse2, KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6 };

    private void Update()
    {
        Event currentEvent = new Event();

        if (selectedKeyBind != null && Event.PopEvent(currentEvent))
        {
            if (currentEvent.isKey)
            {
                DataManager.playerSettings.keyBinds[selectedKeyBind.name] = currentEvent.keyCode;
                selectedKeyBind.Find("Button").GetChild(0).GetComponent<Text>().text = currentEvent.keyCode.ToString();
            }
            else if (currentEvent.isMouse)
            {
                DataManager.playerSettings.keyBinds[selectedKeyBind.name] = mouseKeyCodes[currentEvent.button];
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
        DataManager.playerSettings.sensitivity = slider.value;
        slider.transform.Find("Value Text").GetComponent<Text>().text = slider.value.ToString();
    }

    public void ChangeCameraSmoothing(Slider slider)
    {
        DataManager.playerSettings.cameraSmoothing = slider.value;
        slider.transform.Find("Value Text").GetComponent<Text>().text = slider.value.ToString();
    }

    public void ChangeFOV(Slider slider)
    {
        DataManager.playerSettings.fieldOfView = slider.value;
        slider.transform.Find("Value Text").GetComponent<Text>().text = slider.value.ToString();
        myCamera.fieldOfView = slider.value;
    }

    public void ChangeMasterVolume(Slider slider)
    {
        DataManager.playerSettings.masterVolume = slider.value;
        slider.transform.Find("Value Text").GetComponent<Text>().text = slider.value.ToString();

        AudioListener.volume = DataManager.playerSettings.masterVolume / 100;
    }

    public void ToggleSilhouettes(Toggle toggle)
    {
        DataManager.playerSettings.silhouettes = toggle.isOn;

        foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
        {
            if (feature.name.Contains("Hidden"))
            {
                feature.SetActive(toggle.isOn);
            }
        }
    }

    public void SetCustomCrosshair(InputField input)
    {
        DataManager.playerSettings.crosshairFileName = input.text;
    }

    public void SetCrosshairScale(InputField input)
    {
        DataManager.playerSettings.crosshairScale = float.Parse(input.text);
    }

    public void SetCrosshairColor(Dropdown dropdown)
    {
        DataManager.playerSettings.crosshairColorIndex = dropdown.value;
    }

    public void SaveSettings(string fileName)
    {
        DataManager.playerSettings.SavePlayerSettings(fileName);
    }

    public void LoadSettings(string fileName)
    {
        DataManager.playerSettings = SaveSystem.LoadPlayerSettings(fileName);
        GameManager.Instance.UpdatePlayerWithSettings(player);

        UpdateSettingsUI();
    }

    public void ResetSettings()
    {
        DataManager.playerSettings = SaveSystem.defaultPlayerSettings;

        UpdateSettingsUI();
    }

    public void UpdateSettingsUI()
    {
        // Updating renderer features
        foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
        {
            if (feature.name == "TankHidden" || feature.name == "BulletHidden")
            {
                feature.SetActive(DataManager.playerSettings.silhouettes);
            }
        }
        // Updating UI elements in settings
        foreach (Transform content in transform.Find("Scroll View").Find("Viewport"))
        {
            switch (content.name)
            {
                case "General":
                    foreach (Transform setting in content)
                    {
                        switch (setting.name)
                        {
                            case "Sensitivity":
                                setting.GetComponent<Slider>().value = DataManager.playerSettings.sensitivity;
                                break;
                            case "FOV":
                                setting.GetComponent<Slider>().value = DataManager.playerSettings.fieldOfView;
                                break;
                        }
                    }
                    break;
                case "Keybinds":
                    foreach (Transform keybind in content)
                    {
                        keybind.Find("Button").GetChild(0).GetComponent<Text>().text = DataManager.playerSettings.keyBinds[keybind.name].ToString();
                    }
                    break;
                case "Video":
                    foreach (Transform setting in content)
                    {
                        switch (setting.name)
                        {
                            case "Silhouettes":
                                setting.GetComponent<Toggle>().isOn = DataManager.playerSettings.silhouettes;
                                break;
                            case "Custom Crosshair":
                                setting.Find("InputField").GetComponent<InputField>().text = DataManager.playerSettings.crosshairFileName;
                                break;
                            case "Crosshair Color":
                                setting.Find("Dropdown").GetComponent<Dropdown>().value = DataManager.playerSettings.crosshairColorIndex;
                                break;
                            case "Crosshair Scale":
                                setting.Find("InputField").GetComponent<InputField>().text = DataManager.playerSettings.crosshairScale.ToString();
                                break;
                        }
                    }
                    break;
                case "Audio":
                    foreach (Transform setting in content)
                    {
                        switch (setting.name)
                        {
                            case "Master Volume":
                                setting.GetComponent<Slider>().value = DataManager.playerSettings.masterVolume;
                                break;
                        }
                    }
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
