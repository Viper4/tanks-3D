using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly string SAVE_FOLDER = Application.dataPath + "/SaveData/";

    private class Settings
    {
        public float sensitivity;
        public Dictionary<string, KeyCode> keyBinds;
        public bool silhouettes;
    }

    private class PlayerData
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

    public static void SavePlayerData(string fileName, PlayerControl script, int highestLevel = -1)
    {
        PlayerData playerData = new PlayerData
        {
            lives = script.lives,
            kills = script.kills,
            deaths = script.deaths,
            highestLevel = highestLevel < 0 ? script.highestLevel : highestLevel
        };

        if (File.Exists(SAVE_FOLDER + fileName))
        {
            string oldJson = File.ReadAllText(SAVE_FOLDER + fileName);
            if (oldJson != null)
            {
                PlayerData oldPlayerData = JsonConvert.DeserializeObject<PlayerData>(oldJson);

                playerData.kills = script.kills + oldPlayerData.kills;
                playerData.deaths = script.deaths + oldPlayerData.deaths;

                if (script.highestLevel > oldPlayerData.highestLevel)
                {
                    playerData.highestLevel = script.highestLevel;
                }
            }
        }

        string json = JsonConvert.SerializeObject(playerData, Formatting.Indented);

        File.WriteAllText(SAVE_FOLDER + fileName, json);
    }

    public static void LoadPlayerData(string fileName, PlayerControl script)
    {
        if (File.Exists(SAVE_FOLDER + fileName))
        {
            string json = File.ReadAllText(SAVE_FOLDER + fileName);
            if (json != null)
            {
                PlayerData playerData = JsonConvert.DeserializeObject<PlayerData>(json);

                script.lives = playerData.lives;
                script.kills = playerData.kills;
                script.deaths = playerData.deaths;
                script.highestLevel = playerData.highestLevel;
            }
            else
            {
                Debug.LogWarning("Could not retrieve json data from file " + SAVE_FOLDER + fileName);
            }
        }
        else
        {
            Debug.LogWarning("Could not find file '" + SAVE_FOLDER + fileName + "', creating a new file");

            SavePlayerData(fileName, script);
        }
    }

    public static void SaveSettings(string fileName)
    {
        GameObject player = GameObject.Find("Player");
        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        UIHandler UIHandler = player.transform.Find("UI").GetComponent<UIHandler>();

        Settings settings = new Settings
        {
            sensitivity = Camera.main.GetComponent<CameraControl>().sensitivity,
            keyBinds = playerControl.keyBinds,
            silhouettes = UIHandler.silhouettes,
        };

        string json = JsonConvert.SerializeObject(settings, Formatting.Indented);

        File.WriteAllText(SAVE_FOLDER + fileName, json);
    }

    public static void LoadSettings(string fileName)
    {
        GameObject player = GameObject.Find("Player");
        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        UIHandler UIHandler = player.transform.Find("UI").GetComponent<UIHandler>();

        if (File.Exists(SAVE_FOLDER + fileName))
        {
            string json = File.ReadAllText(SAVE_FOLDER + fileName);

            Camera.main.GetComponent<CameraControl>().sensitivity = 15;

            AddKeybind(playerControl, "Forward", KeyCode.W);
            AddKeybind(playerControl, "Left", KeyCode.A);
            AddKeybind(playerControl, "Backward", KeyCode.S);
            AddKeybind(playerControl, "Right", KeyCode.D);
            AddKeybind(playerControl, "Shoot", KeyCode.Mouse0);
            AddKeybind(playerControl, "Lay Mine", KeyCode.Space);
            AddKeybind(playerControl, "Lock Turret", KeyCode.LeftControl);
            AddKeybind(playerControl, "Lock Camera", KeyCode.LeftShift);
            AddKeybind(playerControl, "Switch Camera", KeyCode.Tab);

            UIHandler.silhouettes = true;

            if (json != null)
            {
                Settings settings = JsonConvert.DeserializeObject<Settings>(json);

                Camera.main.GetComponent<CameraControl>().sensitivity = settings.sensitivity;
                foreach(string key in settings.keyBinds.Keys)
                {
                    playerControl.keyBinds[key] = settings.keyBinds[key];
                }
                UIHandler.silhouettes = settings.silhouettes;
            }
        }
        else
        {
            Debug.LogWarning("Could not find file '" + SAVE_FOLDER + fileName + "', creating a new file");

            SaveSettings(fileName);
        }

        UIHandler.UpdateSettingsUI();
    }

    private static void AddKeybind(PlayerControl script, string key, KeyCode value)
    {
        if (!script.keyBinds.ContainsKey(key))
        {
            script.keyBinds.Add(key, value);
        }
    }
}
