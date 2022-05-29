using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly string SAVE_FOLDER = Application.dataPath + "/SaveData/";

    public static Settings currentSettings = new Settings
    {
        sensitivity = 15,
        keyBinds = new Dictionary<string, KeyCode>()
        {
            { "Forward", KeyCode.W },
            { "Left", KeyCode.A },
            { "Backward", KeyCode.S },
            { "Right", KeyCode.D },
            { "Shoot", KeyCode.Mouse0 },
            { "Lay Mine", KeyCode.Space },
            { "Lock Turret", KeyCode.LeftControl },
            { "Lock Camera", KeyCode.LeftShift },
            { "Switch Camera", KeyCode.Tab }
        },
        silhouettes = true,
        showHUD = true,
        masterVolume = 100,
    };

    public static PlayerData currentPlayerData = new PlayerData
    {
        lives = 3,
        kills = 0,
        deaths = 0,
        highestLevel = 0,
    };

    public class Settings
    {
        public float sensitivity;
        public Dictionary<string, KeyCode> keyBinds;
        public bool silhouettes;
        public bool showHUD;
        public float masterVolume;
    }

    public class PlayerData
    {
        public int lives;
        public int kills;
        public int deaths;
        public int highestLevel;
    }

    public static void Init()
    {
        if (!Directory.Exists(SAVE_FOLDER))
        {
            Directory.CreateDirectory(SAVE_FOLDER);
        }
    }

    public static void SavePlayerData(string fileName, int highestLevel = -1)
    {
        currentPlayerData.highestLevel = highestLevel > currentPlayerData.highestLevel ? highestLevel : currentPlayerData.highestLevel;

        string json = JsonConvert.SerializeObject(currentPlayerData, Formatting.Indented);

        File.WriteAllText(SAVE_FOLDER + fileName, json);
    }

    public static void LoadPlayerData(string fileName)
    {
        if (File.Exists(SAVE_FOLDER + fileName))
        {
            string json = File.ReadAllText(SAVE_FOLDER + fileName);
            if (json != null)
            {
                PlayerData playerData = JsonConvert.DeserializeObject<PlayerData>(json);

                currentPlayerData.lives = playerData.lives;
                currentPlayerData.kills = playerData.kills;
                currentPlayerData.deaths = playerData.deaths;
                currentPlayerData.highestLevel = playerData.highestLevel;
            }
            else
            {
                Debug.LogWarning("Could not retrieve json data from file " + SAVE_FOLDER + fileName);
            }
        }
        else
        {
            Debug.LogWarning("Could not find file '" + SAVE_FOLDER + fileName + "', creating a new file");

            SavePlayerData(fileName);
        }
    }

    public static void SaveSettings(string fileName)
    {
        // currentSettings variables are changed from SettingsUIHandler
        string json = JsonConvert.SerializeObject(currentSettings, Formatting.Indented);

        File.WriteAllText(SAVE_FOLDER + fileName, json);
    }

    public static void LoadSettings(string fileName)
    {
        GameObject player = GameObject.Find("Player");
        SettingsUIHandler settingsUIHandler = Object.FindObjectOfType<SettingsUIHandler>();

        if (File.Exists(SAVE_FOLDER + fileName))
        {
            string json = File.ReadAllText(SAVE_FOLDER + fileName);

            if (json != null)
            {
                currentSettings = JsonConvert.DeserializeObject<Settings>(json);
            }
        }
        else
        {
            Debug.LogWarning("Could not find file '" + SAVE_FOLDER + fileName + "', creating a new file");

            SaveSettings(fileName);
        }

        if (player != null)
        {
            Camera.main.GetComponent<CameraControl>().sensitivity = currentSettings.sensitivity;

            AudioSource[] allAudioSource = Object.FindObjectsOfType<AudioSource>();
            foreach (AudioSource audioSource in allAudioSource)
            {
                audioSource.volume *= currentSettings.masterVolume / 100;
            }
        }

        settingsUIHandler.UpdateSettingsUI();
    }
}
