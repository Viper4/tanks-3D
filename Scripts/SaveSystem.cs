using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly string SAVE_FOLDER = Application.dataPath + "/SaveData/";
    private static readonly string CROSSHAIR_FOLDER = Application.dataPath + "/Crosshairs/";

    public static Sprite crosshair;

    public static readonly Settings defaultSettings = new Settings
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
        crosshairFileName = "Default",
        crosshairColorIndex = 0,
        crosshairScale = 1,
    };

    public static readonly PlayerData defaultPlayerData = new PlayerData
    {
        lives = 3,
        kills = 0,
        shots = 0,
        deaths = 0,
        time = -1,
        bestTime = -1,
    };

    public static void Init()
    {
        if (!Directory.Exists(SAVE_FOLDER))
        {
            Directory.CreateDirectory(SAVE_FOLDER);
        }
        if (!Directory.Exists(CROSSHAIR_FOLDER))
        {
            Directory.CreateDirectory(CROSSHAIR_FOLDER);
        }
    }

    public static void SavePlayerData(string fileName, PlayerData fromPlayerData)
    {
        Debug.Log("Saved PlayerData");
        if (fromPlayerData.lives > 0 && (fromPlayerData.time < fromPlayerData.bestTime || fromPlayerData.bestTime == -1))
        {
            fromPlayerData.bestTime = fromPlayerData.time;
        }

        string json = JsonConvert.SerializeObject(fromPlayerData, Formatting.Indented);

        File.WriteAllText(SAVE_FOLDER + fileName, json);
    }

    public static void LoadPlayerData(string fileName, PlayerData toPlayerData)
    {
        if (File.Exists(SAVE_FOLDER + fileName))
        {
            string json = File.ReadAllText(SAVE_FOLDER + fileName);
            if (json != null)
            {
                PlayerData loadedPlayerData = JsonConvert.DeserializeObject<PlayerData>(json);

                toPlayerData.lives = loadedPlayerData.lives;
                toPlayerData.kills = loadedPlayerData.kills;
                toPlayerData.shots = loadedPlayerData.shots;
                toPlayerData.deaths = loadedPlayerData.deaths;
                toPlayerData.time = loadedPlayerData.time;
                toPlayerData.bestTime = loadedPlayerData.bestTime;
                Debug.Log("Here " + toPlayerData.lives + " / " + loadedPlayerData.lives);
            }
            else
            {
                Debug.LogWarning("Could not retrieve json data from file " + SAVE_FOLDER + fileName);
            }
        }
        else
        {
            Debug.LogWarning("Could not find file '" + SAVE_FOLDER + fileName + "', creating a new file");

            SavePlayerData(fileName, defaultPlayerData);
        }
    }

    public static void SaveSettings(string fileName, Settings fromSettings)
    {
        // currentSettings variables are changed from SettingsUIHandler
        string json = JsonConvert.SerializeObject(fromSettings, Formatting.Indented);

        File.WriteAllText(SAVE_FOLDER + fileName, json);
    }

    public static void LoadSettings(string fileName, Settings toSettings)
    {
        Debug.Log("Load Settings called");
        GameObject player = GameObject.Find("Player");

        if (File.Exists(SAVE_FOLDER + fileName))
        {
            string json = File.ReadAllText(SAVE_FOLDER + fileName);

            if (json != null)
            {
                Settings loadedSettings = JsonConvert.DeserializeObject<Settings>(json);

                toSettings.keyBinds = loadedSettings.keyBinds;
                toSettings.sensitivity = loadedSettings.sensitivity;
                toSettings.masterVolume = loadedSettings.masterVolume;
                toSettings.showHUD = loadedSettings.showHUD;
                toSettings.silhouettes = loadedSettings.silhouettes;
                toSettings.crosshairColorIndex = loadedSettings.crosshairColorIndex;
                toSettings.crosshairScale = loadedSettings.crosshairScale;
                toSettings.crosshairFileName = loadedSettings.crosshairFileName;
            }

            string filePath = CROSSHAIR_FOLDER + toSettings.crosshairFileName + ".png";
            crosshair = ImageToSprite(filePath);

            BaseUIHandler baseUIHandler = Object.FindObjectOfType<BaseUIHandler>();
            if (baseUIHandler != null && baseUIHandler.UIElements.ContainsKey("InGame"))
            {
                Transform reticle = baseUIHandler.UIElements["InGame"].Find("Reticle");

                reticle.GetComponent<CrosshairManager>().UpdateReticleSprite(crosshair, toSettings.crosshairColorIndex, toSettings.crosshairScale);
            }
        }
        else
        {
            Debug.LogWarning("Could not find file '" + SAVE_FOLDER + fileName + "', creating a new file");

            SaveSettings(fileName, defaultSettings);
        }

        if (player != null)
        {
            Camera.main.GetComponent<CameraControl>().sensitivity = toSettings.sensitivity;
        }

        SoundManager[] allAudioSources = Object.FindObjectsOfType<SoundManager>();
        foreach (SoundManager audioSource in allAudioSources)
        {
            audioSource.UpdateVolume();
        }
    }

    public static Sprite ImageToSprite(string filePath, float pixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
    {
        // Converting png or other image format to sprite
        Texture2D spriteTexture = new Texture2D(2, 2);
        if (File.Exists(filePath))
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            if (spriteTexture.LoadImage(fileData))
            {
                return Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), pixelsPerUnit, 0, spriteType);
            }
        }
        return null;
    }
}
