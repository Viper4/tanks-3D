using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomCustomization : MonoBehaviour
{
    [SerializeField] DataManager dataManager;

    [SerializeField] RectTransform FFASettings;
    [SerializeField] RectTransform teamSettings;
    [SerializeField] RectTransform PvESettings;

    [SerializeField] RectTransform roundSettings;

    public void ChangeMap(Dropdown dropdown)
    {
        dataManager.currentRoomSettings.map = dropdown.options[dropdown.value].text;
    }

    public void ChangePrimaryMode(Dropdown dropdown)
    {
        string option = dropdown.options[dropdown.value].text;
        dataManager.currentRoomSettings.primaryMode = option;

        switch (option)
        {
            case "FFA":
                FFASettings.gameObject.SetActive(true);
                PvESettings.gameObject.SetActive(false);
                teamSettings.gameObject.SetActive(false);
                break;
            case "Teams":
                teamSettings.gameObject.SetActive(true);
                FFASettings.gameObject.SetActive(false);
                PvESettings.gameObject.SetActive(false);
                break;
            case "PvE":
                PvESettings.gameObject.SetActive(true);
                teamSettings.gameObject.SetActive(false);
                FFASettings.gameObject.SetActive(false);
                break;
        }
    }

    public void ChangeSecondaryMode(Dropdown dropdown)
    {
        string option = dropdown.options[dropdown.value].text;
        dataManager.currentRoomSettings.secondaryMode = option;

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
        int.TryParse(dropdown.options[dropdown.value].text, out dataManager.currentRoomSettings.teamLimit);
    }

    public void ChangeTeamSize(InputField input)
    {
        int.TryParse(input.text, out dataManager.currentRoomSettings.teamSize);
    }

    public void ChangeWaveSize(Dropdown dropdown)
    {
        dataManager.currentRoomSettings.waveSize = dropdown.value;
    }

    public void ChangeDifficulty(Dropdown dropdown)
    {
        dataManager.currentRoomSettings.difficulty = dropdown.value;
    }

    public void ChangePlayerLimit(InputField input)
    {
        int.TryParse(input.text, out dataManager.currentRoomSettings.playerLimit);
    }

    public void ChangeBotSelection(MultiDropdown multiDropdown)
    {
        foreach (int value in multiDropdown.values)
        {
            dataManager.currentRoomSettings.bots.Add(multiDropdown.options[value].text);
        }
    }

    public void ChangeRoundAmount(InputField input)
    {
        int.TryParse(input.text, out dataManager.currentRoomSettings.roundAmount);
    }

    public void ChangeBotLimit(InputField input)
    {
        int.TryParse(input.text, out dataManager.currentRoomSettings.botLimit);
    }

    public void ChangeFillLobby(Toggle toggle)
    {
        dataManager.currentRoomSettings.fillLobby = toggle.isOn;
    }

    public void UpdateSettingsUI()
    {
        GameObject[] allUISettings = GameObject.FindGameObjectsWithTag("UI Setting");
        foreach(GameObject setting in allUISettings)
        {
            switch (setting.name)
            {
                case "Map Dropdown":
                    SetValueToOption(setting, dataManager.currentRoomSettings.map);
                    break;
                case "Map Preview":

                    break;
                case "Mode 1 Dropdown":
                    SetValueToOption(setting, dataManager.currentRoomSettings.primaryMode);
                    break;
                case "Mode 2 Dropdown":
                    SetValueToOption(setting, dataManager.currentRoomSettings.secondaryMode);
                    break;
                case "Team Limit":
                    SetValueToOption(setting, dataManager.currentRoomSettings.teamLimit.ToString());
                    break;
                case "Team Size":
                    setting.GetComponent<InputField>().text = dataManager.currentRoomSettings.teamSize.ToString();
                    break;
                case "Wave Size":
                    SetValueToOption(setting, dataManager.currentRoomSettings.waveSize.ToString());
                    break;
                case "Difficulty":
                    setting.GetComponent<Dropdown>().value = dataManager.currentRoomSettings.difficulty;
                    break;
                case "Round Amount":
                    setting.GetComponent<InputField>().text = dataManager.currentRoomSettings.roundAmount.ToString();
                    break;
                case "Player Limit":
                    setting.GetComponent<InputField>().text = dataManager.currentRoomSettings.playerLimit.ToString();
                    break;
                case "Bot Selection":
                    SetValuesToOptions(setting, dataManager.currentRoomSettings.bots);
                    break;
                case "Bot Limit":
                    setting.GetComponent<InputField>().text = dataManager.currentRoomSettings.botLimit.ToString();
                    break;
                case "Fill Lobby":
                    setting.GetComponent<Toggle>().isOn = dataManager.currentRoomSettings.fillLobby;
                    break;
            }
        }
    }

    private void SetValueToOption(GameObject dropdownGO, string optionText)
    {
        Dropdown dropdown = dropdownGO.GetComponent<Dropdown>();
        Dropdown.OptionData option = new Dropdown.OptionData(optionText, null);
        int optionIndex = dropdown.options.IndexOf(option);
        if(optionIndex != -1)
        {
            dropdown.value = optionIndex;
        }
    }

    private void SetValuesToOptions(GameObject multiDropdownGO, List<string> optionTexts)
    {
        MultiDropdown multiDropdown = multiDropdownGO.GetComponent<MultiDropdown>();
        for (int i = 0; i < optionTexts.Count; i++)
        {
            Dropdown.OptionData option = new Dropdown.OptionData(optionTexts[i], null);
            int optionIndex = multiDropdown.options.IndexOf(option);
            if (optionIndex != -1 && !multiDropdown.values.Contains(optionIndex))
            {
                multiDropdown.values.Add(optionIndex);
            }
        }
    }
}
