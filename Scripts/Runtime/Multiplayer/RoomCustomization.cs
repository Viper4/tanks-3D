using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomCustomization : MonoBehaviour
{
    [SerializeField] RectTransform mapSelection;
    [SerializeField] RectTransform FFASettings;
    [SerializeField] RectTransform teamSettings;
    [SerializeField] RectTransform PVESettings;
    [SerializeField] RectTransform CoOpSettings;

    private void Start()
    {
        UpdateSettingsUI();
    }

    public void TogglePublic(Toggle toggle)
    {
        DataManager.roomSettings.isPublic = toggle.isOn;
    }

    public void ChangeMap(Dropdown dropdown)
    {
        DataManager.roomSettings.map = dropdown.options[dropdown.value].text;
        DataManager.roomSettings.customMap = dropdown.value > 1;
    }

    public void ChangeCampaign(Dropdown dropdown)
    {
        DataManager.roomSettings.map = dropdown.options[dropdown.value].text + " 1";
    }

    public void ChangeMode(Dropdown dropdown)
    {
        string option = dropdown.options[dropdown.value].text;
        DataManager.roomSettings.mode = option;

        FFASettings.gameObject.SetActive(option == "FFA" || option == "Teams");
        PVESettings.gameObject.SetActive(option == "PvE");
        teamSettings.gameObject.SetActive(option == "Teams");

        if(option == "Co-Op")
        {
            CoOpSettings.gameObject.SetActive(true);
            if (!GameManager.Instance.editing)
                mapSelection.gameObject.SetActive(false);

            if(DataManager.roomSettings.map != "Classic 1" && DataManager.roomSettings.map != "Regular 1")
            {
                DataManager.roomSettings.map = "Classic 1";
            }
        }
        else
        {
            CoOpSettings.gameObject.SetActive(false);
            if(!GameManager.Instance.editing)
                mapSelection.gameObject.SetActive(true);

            if(DataManager.roomSettings.map == "Classic 1" || DataManager.roomSettings.map == "Regular 1")
            {
                DataManager.roomSettings.map = "Classic";
            }
        }
    }

    public void ChangeTeamAmount(Dropdown dropdown)
    {
        int.TryParse(dropdown.options[dropdown.value].text, out DataManager.roomSettings.teamLimit);
    }

    public void ChangeTeamSize(InputField input)
    {
        int.TryParse(input.text, out DataManager.roomSettings.teamSize);
    }

    public void ChangeDifficulty(Dropdown dropdown)
    {
        DataManager.roomSettings.difficulty = dropdown.value;
    }

    public void ChangePlayerLimit(InputField input)
    {
        int.TryParse(input.text, out DataManager.roomSettings.playerLimit);
    }

    public void ChangeBotSelection(MultiDropdown multiDropdown)
    {
        DataManager.roomSettings.bots.Clear();
        foreach(int value in multiDropdown.values)
        {
            DataManager.roomSettings.bots.Add(multiDropdown.options[value].text);
        }
    }

    public void ChangeBotLimit(InputField input)
    {
        int.TryParse(input.text, out DataManager.roomSettings.botLimit);
    }

    public void ChangeFillLobby(Toggle toggle)
    {
        DataManager.roomSettings.fillLobby = toggle.isOn;
    }

    public void ChangeBoostSelection(MultiDropdown multiDropdown)
    {
        DataManager.roomSettings.boosts.Clear();
        foreach(int value in multiDropdown.values)
        {
            DataManager.roomSettings.boosts.Add(multiDropdown.options[value].text);
        }
    }

    public void ChangeBoostLimit(InputField input)
    {
        int.TryParse(input.text, out DataManager.roomSettings.boostLimit);
    }

    public void ChangeTotalLives(TMP_InputField input)
    {
        int.TryParse(input.text, out DataManager.roomSettings.totalLives);
    }

    public void UpdateSettingsUI()
    {
        GameObject[] allUISettings = GameObject.FindGameObjectsWithTag("UI Setting");
        foreach(GameObject setting in allUISettings)
        {
            switch(setting.name)
            {
                case "Is Public":
                    setting.GetComponent<Toggle>().isOn = DataManager.roomSettings.isPublic;
                    break;
                case "Map Dropdown":
                    Dropdown settingDropdown = setting.GetComponent<Dropdown>();
                    settingDropdown.options.RemoveRange(2, settingDropdown.options.Count - 2);
                    List<Dropdown.OptionData> customMaps = new List<Dropdown.OptionData>();
                    foreach(string level in SaveSystem.FilesInSaveFolder(false, ".level"))
                    {
                        customMaps.Add(new Dropdown.OptionData() { text = level });
                    }
                    settingDropdown.AddOptions(customMaps);
                    SetValueToOption(setting.GetComponent<Dropdown>(), DataManager.roomSettings.map);
                    break;
                case "Map Preview":

                    break;
                case "Mode Dropdown":
                    SetValueToOption(setting.GetComponent<Dropdown>(), DataManager.roomSettings.mode);
                    ChangeMode(setting.GetComponent<Dropdown>()); // Enable/disable gameobjects
                    break;
                case "Team Limit":
                    SetValueToOption(setting.GetComponent<Dropdown>(), DataManager.roomSettings.teamLimit.ToString());
                    break;
                case "Team Size":
                    setting.GetComponent<InputField>().text = DataManager.roomSettings.teamSize.ToString();
                    break;
                case "Difficulty":
                    setting.GetComponent<Dropdown>().value = DataManager.roomSettings.difficulty;
                    break;
                case "Player Limit":
                    setting.GetComponent<InputField>().text = DataManager.roomSettings.playerLimit.ToString();
                    break;
                case "Bot Selection":
                    SetValuesToOptions(setting.GetComponent<MultiDropdown>(), DataManager.roomSettings.bots);
                    break;
                case "Bot Limit":
                    setting.GetComponent<InputField>().text = DataManager.roomSettings.botLimit.ToString();
                    break;
                case "Fill Lobby":
                    setting.GetComponent<Toggle>().isOn = DataManager.roomSettings.fillLobby;
                    break;
                case "Boost Selection":
                    SetValuesToOptions(setting.GetComponent<MultiDropdown>(), DataManager.roomSettings.boosts);
                    break;
                case "Boost Limit":
                    setting.GetComponent<InputField>().text = DataManager.roomSettings.boostLimit.ToString();
                    break;
            }
        }
    }

    private void SetValueToOption(Dropdown dropdown, string optionText)
    {
        for(int i = 0; i < dropdown.options.Count; i++)
        {
            if(dropdown.options[i].text == optionText)
            {
                dropdown.value = i;
                break;
            }
        }
    }

    private void SetValuesToOptions(MultiDropdown multiDropdown, List<string> optionTexts)
    {
        for(int i = 0; i < optionTexts.Count; i++)
        {
            for(int j = 0; j < multiDropdown.options.Count; j++)
            {
                if(multiDropdown.options[j].text == optionTexts[i])
                {
                    multiDropdown.AddValue(j);
                    break;
                }
            }
        }
    }
}
