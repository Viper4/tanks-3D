using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class SettingsUIHandler : MonoBehaviour
{
    [SerializeField] ForwardRendererData forwardRenderer;
    [SerializeField] BaseUIHandler baseUIHandler;
    [SerializeField] DataSystem dataSystem;
    Transform selectedKeyBind;

    readonly KeyCode[] mouseKeyCodes = { KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Mouse2, KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6 };

    private void Start()
    {
        UpdateSettingsUI();
    }

    private void Update()
    {
        Event currentEvent = new Event();

        if (selectedKeyBind != null && Event.PopEvent(currentEvent))
        {
            if (currentEvent.isKey)
            {
                dataSystem.currentSettings.keyBinds[selectedKeyBind.name] = currentEvent.keyCode;
                selectedKeyBind.Find("Button").GetChild(0).GetComponent<Text>().text = currentEvent.keyCode.ToString();
            }
            else if (currentEvent.isMouse)
            {
                dataSystem.currentSettings.keyBinds[selectedKeyBind.name] = mouseKeyCodes[currentEvent.button];
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
        dataSystem.currentSettings.sensitivity = slider.value;
        slider.transform.Find("Value Text").GetComponent<Text>().text = slider.value.ToString();
    }

    public void ChangeMasterVolume(Slider slider)
    {
        dataSystem.currentSettings.masterVolume = slider.value;
        slider.transform.Find("Value Text").GetComponent<Text>().text = slider.value.ToString();

        AudioSource[] allAudioSource = Object.FindObjectsOfType<AudioSource>();
        foreach (AudioSource audioSource in allAudioSource)
        {
            audioSource.volume *= dataSystem.currentSettings.masterVolume / 100;
        }
    }

    public void ToggleSilhouettes(Toggle toggle)
    {
        dataSystem.currentSettings.silhouettes = toggle.isOn;

        foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
        {
            if (feature.name == "TankHidden" || feature.name == "BulletHidden")
            {
                feature.SetActive(toggle.isOn);
            }
        }
    }

    public void ToggleHUD(Toggle toggle)
    {
        dataSystem.currentSettings.showHUD = toggle.isOn;

        baseUIHandler.UIElements["HUD"].gameObject.SetActive(toggle.isOn);
    }

    public void SetCustomCrosshair(InputField input)
    {
        dataSystem.currentSettings.crosshairFileName = input.text;
    }

    public void SetCrosshairScale(InputField input)
    {
        dataSystem.currentSettings.crosshairScale = float.Parse(input.text);
    }

    public void SetCrosshairColor(Dropdown dropdown)
    {
        dataSystem.currentSettings.crosshairColorIndex = dropdown.value;
    }

    public void SaveSettings()
    {
        SaveSystem.SaveSettings("settings.json", dataSystem.currentSettings);
    }

    public void LoadSettings()
    {
        SaveSystem.LoadSettings("settings.json", dataSystem.currentSettings);
        UpdateSettingsUI();
    }

    public void UpdateSettingsUI()
    {
        // Updating renderer features
        foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
        {
            if (feature.name == "TankHidden" || feature.name == "BulletHidden")
            {
                feature.SetActive(dataSystem.currentSettings.silhouettes);
            }
        }
        // Updating UI elements in settings
        foreach (Transform content in baseUIHandler.UIElements["Settings"].Find("Scroll View").Find("Viewport"))  
        {
            switch (content.name)
            {
                case "Game":
                    foreach (Transform setting in content)
                    {
                        switch (setting.name)
                        {
                            case "Sensitivity":
                                setting.GetComponent<Slider>().value = dataSystem.currentSettings.sensitivity;
                                break;
                        }
                    }
                    break;
                case "Keybinds":
                    foreach (Transform keybind in content)
                    {
                        keybind.Find("Button").GetChild(0).GetComponent<Text>().text = dataSystem.currentSettings.keyBinds[keybind.name].ToString();
                    }
                    break;
                case "Video":
                    foreach (Transform setting in content)
                    {
                        switch (setting.name)
                        {
                            case "Silhouettes":
                                setting.GetComponent<Toggle>().isOn = dataSystem.currentSettings.silhouettes;
                                break;
                            case "HUD":
                                setting.GetComponent<Toggle>().isOn = dataSystem.currentSettings.showHUD;
                                break;
                            case "Custom Crosshair":
                                setting.Find("InputField").GetComponent<InputField>().text = dataSystem.currentSettings.crosshairFileName;
                                break;
                            case "Crosshair Color":
                                setting.Find("Dropdown").GetComponent<Dropdown>().value = dataSystem.currentSettings.crosshairColorIndex;
                                break;
                            case "Crosshair Scale":
                                setting.Find("InputField").GetComponent<InputField>().text = dataSystem.currentSettings.crosshairScale.ToString();
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
                                setting.GetComponent<Slider>().value = dataSystem.currentSettings.masterVolume;
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
