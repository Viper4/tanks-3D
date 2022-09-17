using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using MyUnityAddons.Calculations;

public static class SaveSystem
{
    private static readonly string SAVE_FOLDER = Application.dataPath + "/SaveData/";
    private static readonly string CROSSHAIR_FOLDER = Application.dataPath + "/Crosshairs/";

    private static readonly string playerSettingsExtension = ".playersettings";
    private static readonly string roomSettingsExtension = ".roomsettings";
    private static readonly string playerDataExtension = ".playerdata";

    public static Sprite crosshair;

    public static readonly PlayerSettings defaultPlayerSettings = new PlayerSettings
    {
        sensitivity = 15,
        cameraSmoothing = 0.1f,
        fieldOfView = 60,
        keyBinds = new Dictionary<string, KeyCode>()
        {
            { "Forward", KeyCode.W },
            { "Left", KeyCode.A },
            { "Backward", KeyCode.S },
            { "Right", KeyCode.D },
            { "Up", KeyCode.Space },
            { "Down", KeyCode.LeftControl },
            { "Shoot", KeyCode.Mouse0 },
            { "Lay Mine", KeyCode.Space },
            { "Lock Turret", KeyCode.LeftControl },
            { "Lock Camera", KeyCode.LeftShift },
            { "Switch Camera", KeyCode.LeftAlt },
            { "Leaderboard", KeyCode.Tab },
            { "Toggle HUD", KeyCode.F1 },
            { "Screenshot", KeyCode.F11 }
        },
        silhouettes = true,
        masterVolume = 100,
        crosshairFileName = "Default",
        crosshairColorIndex = 0,
        crosshairScale = 1,
    };

    public static readonly RoomSettings defaultRoomSettings = new RoomSettings
    {
        isPublic = true,
        map = "Classic",
        primaryMode = "FFA",
        secondaryMode = "Endless",
        teamLimit = 2,
        teamSize = 3,
        waveSize = 1,
        roundAmount = 10,
        difficulty = 1,
        playerLimit = 8,
        bots = new List<string>(),
        botLimit = 4,
        fillLobby = true,
        totalLives = 6,
    };

    public static readonly PlayerData defaultPlayerData = new PlayerData
    {
        lives = 3,
        kills = 0,
        shots = 0,
        deaths = 0,
        time = -1,
        bestTime = -1,
        sceneIndex = -1
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

    public static PlayerData ResetPlayerData(string fileName)
    {
        PlayerData playerData = LoadPlayerData(fileName);
        float bestTime = playerData.bestTime;

        playerData.lives = defaultPlayerData.lives;
        playerData.kills = defaultPlayerData.kills;
        playerData.shots = defaultPlayerData.shots;
        playerData.deaths = defaultPlayerData.deaths;
        playerData.time = defaultPlayerData.time;
        playerData.bestTime = bestTime;
        playerData.sceneIndex = defaultPlayerData.sceneIndex;
        Debug.Log(playerData.kills + " / " + defaultPlayerData.kills);

        playerData.SavePlayerData(fileName, false);

        return playerData;
    }

    public static void SavePlayerData(this PlayerData fromPlayerData, string fileName, bool compareTime)
    {
        if (compareTime && fromPlayerData.lives > 0 && (fromPlayerData.time < fromPlayerData.bestTime || fromPlayerData.bestTime == -1))
        {
            Debug.Log("Updated best Time");
            fromPlayerData.bestTime = fromPlayerData.time;
        }

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(SAVE_FOLDER + fileName + playerDataExtension, FileMode.Create);

        formatter.Serialize(stream, fromPlayerData);
        stream.Close();
    }

    public static PlayerData LoadPlayerData(string fileName)
    {
        PlayerData newPlayerData;
        string filePath = SAVE_FOLDER + fileName + playerDataExtension;
        if (File.Exists(filePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(filePath, FileMode.Open);

            PlayerData loadedPlayerData = (PlayerData)formatter.Deserialize(stream);
            stream.Close();

            newPlayerData = loadedPlayerData;
        }
        else
        {
            Debug.LogWarning("Could not find file '" + filePath + "', saving and loading defaults.");

            defaultPlayerData.SavePlayerData(fileName, false);

            newPlayerData = defaultPlayerData;
        }

        return newPlayerData;
    }

    public static void SavePlayerSettings(this PlayerSettings fromSettings, string fileName)
    {
        string json = JsonConvert.SerializeObject(fromSettings, Formatting.Indented);

        File.WriteAllText(SAVE_FOLDER + fileName + playerSettingsExtension, json);
    }

    public static PlayerSettings LoadPlayerSettings(string fileName, Transform player)
    {
        if (File.Exists(SAVE_FOLDER + fileName + playerSettingsExtension))
        {
            string json = File.ReadAllText(SAVE_FOLDER + fileName + playerSettingsExtension);

            if (json != null)
            {
                PlayerSettings loadedSettings = JsonConvert.DeserializeObject<PlayerSettings>(json);

                if (player != null)
                {
                    if (player.TryGetComponent<SpectatorControl>(out var spectatorControl))
                    {
                        spectatorControl.sensitivity = loadedSettings.sensitivity;
                        spectatorControl.rotationSmoothTime = loadedSettings.cameraSmoothing;
                    }
                    else
                    {
                        string crosshairFilePath = CROSSHAIR_FOLDER + loadedSettings.crosshairFileName + ".png";
                        crosshair = CustomMath.ImageToSprite(crosshairFilePath);
                        Transform playerUI = player.Find("Player UI");
                        if (playerUI != null)
                        {
                            BaseUIHandler baseUIHandler = playerUI.GetComponent<BaseUIHandler>();
                            if (baseUIHandler != null && baseUIHandler.UIElements.ContainsKey("InGame"))
                            {
                                Transform reticle = baseUIHandler.UIElements["InGame"].Find("Reticle");
                                reticle.GetComponent<CrosshairManager>().UpdateReticleSprite(crosshair, loadedSettings.crosshairColorIndex, loadedSettings.crosshairScale);
                            }
                        }

                        Transform camera = player.Find("Camera");
                        if (camera != null && camera.TryGetComponent<CameraControl>(out var cameraS))
                        {
                            cameraS.sensitivity = loadedSettings.sensitivity;
                            cameraS.rotationSmoothTime = loadedSettings.cameraSmoothing;
                        }
                    }
                }

                AudioListener.volume = loadedSettings.masterVolume / 100;

                return loadedSettings;
            }
        }
        else
        {
            Debug.LogWarning("Could not find file '" + SAVE_FOLDER + fileName + playerSettingsExtension + "', saving and loading defaults.");

            defaultPlayerSettings.SavePlayerSettings(fileName);
        }
        return defaultPlayerSettings;
    }

    public static void SaveRoomSettings(this RoomSettings fromSettings, string fileName)
    {
        string json = JsonConvert.SerializeObject(fromSettings, Formatting.Indented);

        File.WriteAllText(SAVE_FOLDER + fileName + roomSettingsExtension, json);
    }

    public static RoomSettings LoadRoomSettings(string fileName)
    {
        RoomSettings newSettings = new RoomSettings();
        if (File.Exists(SAVE_FOLDER + fileName + roomSettingsExtension))
        {
            string json = File.ReadAllText(SAVE_FOLDER + fileName + roomSettingsExtension);

            if (json != null)
            {
                RoomSettings loadedSettings = JsonConvert.DeserializeObject<RoomSettings>(json);

                newSettings = loadedSettings;
            }
        }
        else
        {
            Debug.LogWarning("Could not find file '" + SAVE_FOLDER + fileName + roomSettingsExtension + "', saving and loading defaults.");

            defaultRoomSettings.SaveRoomSettings(fileName);

            newSettings = defaultRoomSettings;
        }
        return newSettings;
    }

    public static void DeleteFile(string fullFileName)
    {
        if (File.Exists(SAVE_FOLDER + fullFileName))
        {
            File.Delete(SAVE_FOLDER + fullFileName);
        }
    }

    public static string LatestFileInSaveFolder(bool returnExtension, string fileExtension = "")
    {
        DirectoryInfo saveFolder = new DirectoryInfo(SAVE_FOLDER);
        FileInfo latestFile = saveFolder.GetFiles("*" + fileExtension, SearchOption.AllDirectories).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();

        if (returnExtension)
        {
            return latestFile.Name;
        }
        else
        {
            return Path.GetFileNameWithoutExtension(latestFile.Name);
        }
    }

    public static IEnumerable<string> FilesInSaveFolder(bool returnExtension, string fileExtension = "")
    {
        if (returnExtension)
        {
            return Directory.EnumerateFiles(SAVE_FOLDER, "*" + fileExtension, SearchOption.AllDirectories).Select(Path.GetFileName);
        }
        else
        {
            return Directory.EnumerateFiles(SAVE_FOLDER, "*" + fileExtension, SearchOption.AllDirectories).Select(Path.GetFileNameWithoutExtension);
        }
    }
}
