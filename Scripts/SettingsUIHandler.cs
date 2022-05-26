using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class SettingsUIHandler : MonoBehaviour
{
    [SerializeField] ForwardRendererData forwardRenderer;
    Transform selectedKeyBind;

    readonly KeyCode[] mouseKeyCodes = { KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Mouse2, KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6 };

    private void Update()
    {
        Event currentEvent = new Event();

        if (selectedKeyBind != null && Event.PopEvent(currentEvent))
        {
            if (currentEvent.isKey)
            {
                SaveSystem.currentSettings.keyBinds[selectedKeyBind.name] = currentEvent.keyCode;
                selectedKeyBind.Find("Button").GetChild(0).GetComponent<Text>().text = currentEvent.keyCode.ToString();
            }
            else if (currentEvent.isMouse)
            {
                SaveSystem.currentSettings.keyBinds[selectedKeyBind.name] = mouseKeyCodes[currentEvent.button];
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
        SaveSystem.currentSettings.sensitivity = slider.value;
        slider.transform.Find("Value Text").GetComponent<Text>().text = slider.value.ToString();
    }

    public void ToggleSilhouettes(Toggle toggle)
    {
        SaveSystem.currentSettings.silhouettes = toggle.isOn;

        foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
        {
            if (feature.name == "TankHidden" || feature.name == "BulletHidden")
            {
                feature.SetActive(SaveSystem.currentSettings.silhouettes);
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
                feature.SetActive(SaveSystem.currentSettings.silhouettes);
            }
        }
        // Updating UI elements in settings
        foreach (Transform content in BaseUIHandler.UIElements["Settings"].Find("Scroll View").Find("Viewport"))
        {
            switch (content.name)
            {
                case "Game":
                    foreach (Transform setting in content)
                    {
                        switch (setting.name)
                        {
                            case "Sensitivity":
                                setting.GetComponent<Slider>().value = SaveSystem.currentSettings.sensitivity;
                                break;
                        }
                    }
                    break;
                case "Keybinds":
                    foreach (Transform keybind in content)
                    {
                        keybind.Find("Button").GetChild(0).GetComponent<Text>().text = SaveSystem.currentSettings.keyBinds[keybind.name].ToString();
                    }
                    break;
                case "Video":
                    foreach (Transform setting in content)
                    {
                        switch (setting.name)
                        {
                            case "Silhouettes":
                                setting.GetComponent<Toggle>().isOn = SaveSystem.currentSettings.silhouettes;
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
