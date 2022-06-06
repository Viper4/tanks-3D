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
            { "Switch Camera", KeyCode.LeftAlt },
            { "Leaderboard", KeyCode.Tab }
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

    public static void SavePlayerData(string fileName, PlayerData fromPlayerData, bool compareTime)
    {
        if (compareTime && fromPlayerData.lives > 0 && (fromPlayerData.time < fromPlayerData.bestTime || fromPlayerData.bestTime == -1))
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
            } 
            else
            {
                Debug.LogWarning("Could not retrieve json data from file " + SAVE_FOLDER + fileName);
            }
        }
        else
        {
            Debug.LogWarning("Could not find file '" + SAVE_FOLDER + fileName + "', creating a new file");

            SavePlayerData(fileName, defaultPlayerData, false);
        }
    }

    public static void SaveSettings(string fileName, Settings fromSettings)
    {
        // currentSettings variables are changed from SettingsUIHandler
        string json = JsonConvert.SerializeObject(fromSettings, Formatting.Indented);

        File.WriteAllText(SAVE_FOLDER + fileName, json);
    }

    public static void LoadSettings(string fileName, DataSystem toData)
    {
        if (File.Exists(SAVE_FOLDER + fileName))
        {
            string json = File.ReadAllText(SAVE_FOLDER + fileName);

            if (json != null)
            {
                Settings loadedSettings = JsonConvert.DeserializeObject<Settings>(json);

                toData.currentSettings.keyBinds = loadedSettings.keyBinds;
                toData.currentSettings.sensitivity = loadedSettings.sensitivity;
                toData.currentSettings.masterVolume = loadedSettings.masterVolume;
                toData.currentSettings.showHUD = loadedSettings.showHUD;
                toData.currentSettings.silhouettes = loadedSettings.silhouettes;
                toData.currentSettings.crosshairColorIndex = loadedSettings.crosshairColorIndex;
                toData.currentSettings.crosshairScale = loadedSettings.crosshairScale;
                toData.currentSettings.crosshairFileName = loadedSettings.crosshairFileName;
            }

            string filePath = CROSSHAIR_FOLDER + toData.currentSettings.crosshairFileName + ".png";
            crosshair = ImageToSprite(filePath);

            Transform playerUI = toData.transform.Find("Player UI");
            if (playerUI != null)
            {
                BaseUIHandler baseUIHandler = playerUI.GetComponent<BaseUIHandler>();
                if (baseUIHandler != null && baseUIHandler.UIElements.ContainsKey("InGame"))
                {
                    Transform reticle = baseUIHandler.UIElements["InGame"].Find("Reticle");

                    reticle.GetComponent<CrosshairManager>().UpdateReticleSprite(crosshair, toData.currentSettings.crosshairColorIndex, toData.currentSettings.crosshairScale);
                }
            }
        }
        else
        {
            Debug.LogWarning("Could not find file '" + SAVE_FOLDER + fileName + "', creating a new file");

            SaveSettings(fileName, defaultSettings);
        }

        Transform camera = toData.transform.Find("Main Camera");
        if(camera != null)
        {
            CameraControl cameraS = toData.transform.Find("Main Camera").GetComponent<CameraControl>();
            MultiplayerCameraControl cameraM = toData.transform.Find("Main Camera").GetComponent<MultiplayerCameraControl>();

            if (cameraS != null)
            {
                cameraS.sensitivity = toData.currentSettings.sensitivity;
            }
            else if (cameraM != null)
            {
                cameraM.sensitivity = toData.currentSettings.sensitivity;
            }
        }

        SoundManager[] allSounds = Object.FindObjectsOfType<SoundManager>();
        foreach (SoundManager sound in allSounds)
        {
            sound.UpdateVolume(toData.currentSettings.masterVolume);
        }

        EngineSoundManager[] allEngineSounds = Object.FindObjectsOfType<EngineSoundManager>();
        foreach (EngineSoundManager engineSound in allEngineSounds)
        {
            engineSound.UpdateMasterVolume(toData.currentSettings.masterVolume);
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
