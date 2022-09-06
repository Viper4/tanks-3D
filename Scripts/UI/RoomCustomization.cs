using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomCustomization : MonoBehaviour
{
    [SerializeField] RectTransform mapSelection;

    [SerializeField] RectTransform FFASettings;
    [SerializeField] RectTransform teamSettings;
    [SerializeField] RectTransform PvESettings;
    [SerializeField] RectTransform CoOpSettings;

    [SerializeField] RectTransform roundSettings;

    public void TogglePublic(Toggle toggle)
    {
        DataManager.roomSettings.isPublic = toggle.isOn;
    }

    public void ChangeMap(Dropdown dropdown)
    {
        DataManager.roomSettings.map = dropdown.options[dropdown.value].text;
    }

    public void ChangeCampaign(Dropdown dropdown)
    {
        DataManager.roomSettings.map = dropdown.options[dropdown.value].text + " 1";
    }

    public void ChangePrimaryMode(Dropdown dropdown)
    {
        string option = dropdown.options[dropdown.value].text;
        DataManager.roomSettings.primaryMode = option;

        FFASettings.gameObject.SetActive(option == "FFA");
        PvESettings.gameObject.SetActive(option == "PvE");
        teamSettings.gameObject.SetActive(option == "Teams");

        if (option == "Co-Op")
        {
            CoOpSettings.gameObject.SetActive(true);
            mapSelection.gameObject.SetActive(false);

            DataManager.roomSettings.map = "Classic 1";
        }
        else
        {
            CoOpSettings.gameObject.SetActive(false);
            mapSelection.gameObject.SetActive(true);

            DataManager.roomSettings.map = "Classic";
        }
    }

    public void ChangeSecondaryMode(Dropdown dropdown)
    {
        string option = dropdown.options[dropdown.value].text;
        DataManager.roomSettings.secondaryMode = option;

        switch (option)
        {
            case "Endless":
                roundSettings.gameObject.SetActive(false);
                break;
            case "Rounds":
                roundSettings.gameObject.SetActive(true);
                break;
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

    public void ChangeWaveSize(Dropdown dropdown)
    {
        DataManager.roomSettings.waveSize = dropdown.value;
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
        foreach (int value in multiDropdown.values)
        {
            DataManager.roomSettings.bots.Add(multiDropdown.options[value].text);
        }
    }

    public void ChangeRoundAmount(InputField input)
    {
        int.TryParse(input.text, out DataManager.roomSettings.roundAmount);
    }

    public void ChangeBotLimit(InputField input)
    {
        int.TryParse(input.text, out DataManager.roomSettings.botLimit);
    }

    public void ChangeFillLobby(Toggle toggle)
    {
        DataManager.roomSettings.fillLobby = toggle.isOn;
    }

    public void UpdateSettingsUI()
    {
        GameObject[] allUISettings = GameObject.FindGameObjectsWithTag("UI Setting");
        foreach (GameObject setting in allUISettings)
        {
            switch (setting.name)
            {
                case "Map Dropdown":
                    SetValueToOption(setting.GetComponent<Dropdown>(), DataManager.roomSettings.map);
                    break;
                case "Map Preview":

                    break;
                case "Mode 1 Dropdown":
                    SetValueToOption(setting.GetComponent<Dropdown>(), DataManager.roomSettings.primaryMode);
                    ChangePrimaryMode(setting.GetComponent<Dropdown>()); // Enable/disable gameobjects
                    break;
                case "Mode 2 Dropdown":
                    SetValueToOption(setting.GetComponent<Dropdown>(), DataManager.roomSettings.secondaryMode);
                    ChangeSecondaryMode(setting.GetComponent<Dropdown>()); // Enable/disable gameobjects
                    break;
                case "Team Limit":
                    SetValueToOption(setting.GetComponent<Dropdown>(), DataManager.roomSettings.teamLimit.ToString());
                    break;
                case "Team Size":
                    setting.GetComponent<InputField>().text = DataManager.roomSettings.teamSize.ToString();
                    break;
                case "Wave Size":
                    SetValueToOption(setting.GetComponent<Dropdown>(), DataManager.roomSettings.waveSize.ToString());
                    break;
                case "Difficulty":
                    setting.GetComponent<Dropdown>().value = DataManager.roomSettings.difficulty;
                    break;
                case "Round Amount":
                    setting.GetComponent<InputField>().text = DataManager.roomSettings.roundAmount.ToString();
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
            }
        }
    }

    private void SetValueToOption(Dropdown dropdown, string optionText)
    {
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (dropdown.options[i].text == optionText)
            {
                dropdown.value = i;
                break;
            }
        }
    }

    private void SetValuesToOptions(MultiDropdown multiDropdown, List<string> optionTexts)
    {
        for (int i = 0; i < optionTexts.Count; i++)
        {
            for (int j = 0; j < multiDropdown.options.Count; j++)
            {
                if (multiDropdown.options[j].text == optionTexts[i])
                {
                    multiDropdown.values.Add(j);
                    break;
                }
            }
        }
    }
}
