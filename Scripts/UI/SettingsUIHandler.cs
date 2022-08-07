using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class SettingsUIHandler : MonoBehaviour
{
    [SerializeField] DataManager dataManager;
    [SerializeField] UniversalRendererData forwardRenderer;
    [SerializeField] BaseUIHandler baseUIHandler;
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
                dataManager.currentPlayerSettings.keyBinds[selectedKeyBind.name] = currentEvent.keyCode;
                selectedKeyBind.Find("Button").GetChild(0).GetComponent<Text>().text = currentEvent.keyCode.ToString();
            }
            else if (currentEvent.isMouse)
            {
                dataManager.currentPlayerSettings.keyBinds[selectedKeyBind.name] = mouseKeyCodes[currentEvent.button];
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
        dataManager.currentPlayerSettings.sensitivity = slider.value;
        slider.transform.Find("Value Text").GetComponent<Text>().text = slider.value.ToString();
    }

    public void ChangeMasterVolume(Slider slider)
    {
        dataManager.currentPlayerSettings.masterVolume = slider.value;
        slider.transform.Find("Value Text").GetComponent<Text>().text = slider.value.ToString();

        AudioSource[] allAudioSource = Object.FindObjectsOfType<AudioSource>();
        foreach (AudioSource audioSource in allAudioSource)
        {
            audioSource.volume *= dataManager.currentPlayerSettings.masterVolume / 100;
        }
    }

    public void ToggleSilhouettes(Toggle toggle)
    {
        dataManager.currentPlayerSettings.silhouettes = toggle.isOn;

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
        dataManager.currentPlayerSettings.crosshairFileName = input.text;
    }

    public void SetCrosshairScale(InputField input)
    {
        dataManager.currentPlayerSettings.crosshairScale = float.Parse(input.text);
    }

    public void SetCrosshairColor(Dropdown dropdown)
    {
        dataManager.currentPlayerSettings.crosshairColorIndex = dropdown.value;
    }

    public void SaveSettings(string fileName)
    {
        dataManager.currentPlayerSettings.SavePlayerSettings(fileName);
    }

    public void LoadSettings(string fileName)
    {
        dataManager.currentPlayerSettings = SaveSystem.LoadPlayerSettings(fileName, dataManager.transform);

        UpdateSettingsUI();
    }

    public void ResetSettings()
    {
        dataManager.currentPlayerSettings = SaveSystem.defaultPlayerSettings;

        UpdateSettingsUI();
    }

    public void UpdateSettingsUI()
    {
        // Updating renderer features
        foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
        {
            if (feature.name == "TankHidden" || feature.name == "BulletHidden")
            {
                feature.SetActive(dataManager.currentPlayerSettings.silhouettes);
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
                                setting.GetComponent<Slider>().value = dataManager.currentPlayerSettings.sensitivity;
                                break;
                        }
                    }
                    break;
                case "Keybinds":
                    foreach (Transform keybind in content)
                    {
                        keybind.Find("Button").GetChild(0).GetComponent<Text>().text = dataManager.currentPlayerSettings.keyBinds[keybind.name].ToString();
                    }
                    break;
                case "Video":
                    foreach (Transform setting in content)
                    {
                        switch (setting.name)
                        {
                            case "Silhouettes":
                                setting.GetComponent<Toggle>().isOn = dataManager.currentPlayerSettings.silhouettes;
                                break;
                            case "Custom Crosshair":
                                setting.Find("InputField").GetComponent<InputField>().text = dataManager.currentPlayerSettings.crosshairFileName;
                                break;
                            case "Crosshair Color":
                                setting.Find("Dropdown").GetComponent<Dropdown>().value = dataManager.currentPlayerSettings.crosshairColorIndex;
                                break;
                            case "Crosshair Scale":
                                setting.Find("InputField").GetComponent<InputField>().text = dataManager.currentPlayerSettings.crosshairScale.ToString();
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
                                setting.GetComponent<Slider>().value = dataManager.currentPlayerSettings.masterVolume;
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
