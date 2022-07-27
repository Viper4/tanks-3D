using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class SettingsUIHandler : MonoBehaviour
{
    [SerializeField] UniversalRendererData forwardRenderer;
    [SerializeField] BaseUIHandler baseUIHandler;
    [SerializeField] DataManager dataSystem;
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
                dataSystem.currentPlayerSettings.keyBinds[selectedKeyBind.name] = currentEvent.keyCode;
                selectedKeyBind.Find("Button").GetChild(0).GetComponent<Text>().text = currentEvent.keyCode.ToString();
            }
            else if (currentEvent.isMouse)
            {
                dataSystem.currentPlayerSettings.keyBinds[selectedKeyBind.name] = mouseKeyCodes[currentEvent.button];
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
        dataSystem.currentPlayerSettings.sensitivity = slider.value;
        slider.transform.Find("Value Text").GetComponent<Text>().text = slider.value.ToString();
    }

    public void ChangeMasterVolume(Slider slider)
    {
        dataSystem.currentPlayerSettings.masterVolume = slider.value;
        slider.transform.Find("Value Text").GetComponent<Text>().text = slider.value.ToString();

        AudioSource[] allAudioSource = Object.FindObjectsOfType<AudioSource>();
        foreach (AudioSource audioSource in allAudioSource)
        {
            audioSource.volume *= dataSystem.currentPlayerSettings.masterVolume / 100;
        }
    }

    public void ToggleSilhouettes(Toggle toggle)
    {
        dataSystem.currentPlayerSettings.silhouettes = toggle.isOn;

        foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
        {
            if (feature.name == "TankHidden" || feature.name == "BulletHidden")
            {
                feature.SetActive(toggle.isOn);
            }
        }
    }

    public void SetCustomCrosshair(InputField input)
    {
        dataSystem.currentPlayerSettings.crosshairFileName = input.text;
    }

    public void SetCrosshairScale(InputField input)
    {
        dataSystem.currentPlayerSettings.crosshairScale = float.Parse(input.text);
    }

    public void SetCrosshairColor(Dropdown dropdown)
    {
        dataSystem.currentPlayerSettings.crosshairColorIndex = dropdown.value;
    }

    public void SaveSettings(string fileName)
    {
        dataSystem.currentPlayerSettings.SavePlayerSettings(fileName);
    }

    public void LoadSettings(string fileName)
    {
        dataSystem.currentPlayerSettings = SaveSystem.LoadPlayerSettings(fileName, dataSystem.transform);

        UpdateSettingsUI();
    }

    public void ResetSettings()
    {
        dataSystem.currentPlayerSettings = SaveSystem.defaultPlayerSettings;

        UpdateSettingsUI();
    }

    public void UpdateSettingsUI()
    {
        // Updating renderer features
        foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
        {
            if (feature.name == "TankHidden" || feature.name == "BulletHidden")
            {
                feature.SetActive(dataSystem.currentPlayerSettings.silhouettes);
            }
        }
        // Updating UI elements in settings
        foreach (Transform content in transform.Find("Scroll View").Find("Viewport"))
        {
            switch (content.name)
            {
                case "Game":
                    foreach (Transform setting in content)
                    {
                        switch (setting.name)
                        {
                            case "Sensitivity":
                                setting.GetComponent<Slider>().value = dataSystem.currentPlayerSettings.sensitivity;
                                break;
                        }
                    }
                    break;
                case "Keybinds":
                    foreach (Transform keybind in content)
                    {
                        keybind.Find("Button").GetChild(0).GetComponent<Text>().text = dataSystem.currentPlayerSettings.keyBinds[keybind.name].ToString();
                    }
                    break;
                case "Video":
                    foreach (Transform setting in content)
                    {
                        switch (setting.name)
                        {
                            case "Silhouettes":
                                setting.GetComponent<Toggle>().isOn = dataSystem.currentPlayerSettings.silhouettes;
                                break;
                            case "Custom Crosshair":
                                setting.Find("InputField").GetComponent<InputField>().text = dataSystem.currentPlayerSettings.crosshairFileName;
                                break;
                            case "Crosshair Color":
                                setting.Find("Dropdown").GetComponent<Dropdown>().value = dataSystem.currentPlayerSettings.crosshairColorIndex;
                                break;
                            case "Crosshair Scale":
                                setting.Find("InputField").GetComponent<InputField>().text = dataSystem.currentPlayerSettings.crosshairScale.ToString();
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
                                setting.GetComponent<Slider>().value = dataSystem.currentPlayerSettings.masterVolume;
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
