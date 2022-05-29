using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly string SAVE_FOLDER = Application.dataPath + "/SaveData/";
    private static readonly string CROSSHAIR_FOLDER = Application.dataPath + "/Crosshairs/";

    public static Sprite crosshair;

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
        crosshairFileName = "Default",
        crosshairColorIndex = 0,
        crosshairScale = 1,
    };

    public static PlayerData currentPlayerData = new PlayerData
    {
        lives = 3,
        kills = 0,
        shots = 0,
        deaths = 0,
        time = -1,
        bestTime = -1,
    };

    public class Settings
    {
        public float sensitivity;
        public Dictionary<string, KeyCode> keyBinds;
        public bool silhouettes;
        public bool showHUD;
        public float masterVolume;
        public string crosshairFileName;
        public int crosshairColorIndex;
        public float crosshairScale;
    }

    public class PlayerData
    {
        public int lives;
        public int kills;
        public int shots;
        public int deaths;
        public float time;
        public float bestTime;
    }

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

    public static void SavePlayerData(string fileName)
    {
        if (currentPlayerData.lives > 0 && (currentPlayerData.time < currentPlayerData.bestTime || currentPlayerData.bestTime == -1))
        {
            currentPlayerData.bestTime = currentPlayerData.time;
        }

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

                currentPlayerData.bestTime = playerData.bestTime;
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

            string filePath = CROSSHAIR_FOLDER + currentSettings.crosshairFileName + ".png";
            crosshair = ImageToSprite(filePath);

            try
            {
                Transform reticle = BaseUIHandler.UIElements["InGame"].Find("Reticle");

                reticle.GetComponent<CrosshairManager>().UpdateReticleSprite(crosshair, currentSettings.crosshairColorIndex, currentSettings.crosshairScale);
            }
            catch
            {
                Debug.Log("Unable to set reticle sprite, skipping.");
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
