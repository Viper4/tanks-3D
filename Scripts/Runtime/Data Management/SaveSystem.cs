using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    private static readonly string SAVE_FOLDER = Application.dataPath + "/SaveData/";
    private static readonly string SETTINGS_FOLDER = SAVE_FOLDER + "Settings/";
    private static readonly string LEVELS_FOLDER = SAVE_FOLDER + "Levels/";
    private static readonly string DATA_FOLDER = SAVE_FOLDER + "Data/";

    public static readonly string CROSSHAIR_FOLDER = Application.dataPath + "/Crosshairs/";

    private static readonly string chatSettingsExtension = ".chatsettings";
    private static readonly string playerSettingsExtension = ".playersettings";
    private static readonly string roomSettingsExtension = ".roomsettings";
    private static readonly string playerDataExtension = ".playerdata";

    public static readonly ChatSettings defaultChatSettings = new ChatSettings()
    {
        username = "",
        whitelistActive = false,
        whitelist = new List<string>(),
        blacklist = new List<string>(),
        muteList = new List<string>(),
    };

    public static readonly PlayerSettings defaultPlayerSettings = new PlayerSettings
    {
        sensitivity = 15,
        cameraSmoothing = true,
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
            { "Chat", KeyCode.Return },
            { "Leaderboard", KeyCode.Tab },
            { "Zoom Control", KeyCode.Z },
            { "Toggle HUD", KeyCode.F1 },
            { "Screenshot", KeyCode.F2 },
            { "Debug Menu", KeyCode.F3 },
        },
        targetFramerate = 60,
        silhouettes = true,
        masterVolume = 100,
        crosshairFileName = "Default",
        crosshairColorIndex = 0,
        crosshairScale = 1,
        slowZoomSpeed = 1f,
        fastZoomSpeed = 5f,
    };

    public static readonly RoomSettings defaultRoomSettings = new RoomSettings
    {
        isPublic = true,
        map = "Classic",
        mode = "FFA",
        teamLimit = 2,
        teamSize = 3,
        difficulty = 1,
        playerLimit = 8,
        bots = new List<string>(),
        botLimit = 4,
        fillLobby = true,
        boosts = new List<string>(),
        boostLimit = 10,
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
        sceneIndex = -1,
        previousSceneIndex = -1
    };

    public static void Init()
    {
        if (!Directory.Exists(SAVE_FOLDER))
        {
            Directory.CreateDirectory(SAVE_FOLDER);
        }
        if (!Directory.Exists(SETTINGS_FOLDER))
        {
            Directory.CreateDirectory(SETTINGS_FOLDER);
        }
        if (!Directory.Exists(LEVELS_FOLDER))
        {
            Directory.CreateDirectory(LEVELS_FOLDER);
        }
        if (!Directory.Exists(DATA_FOLDER))
        {
            Directory.CreateDirectory(DATA_FOLDER);
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
        playerData.previousSceneIndex = defaultPlayerData.previousSceneIndex;

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
        FileStream stream = new FileStream(DATA_FOLDER + fileName + playerDataExtension, FileMode.Create);

        formatter.Serialize(stream, fromPlayerData);
        stream.Close();
    }

    public static PlayerData LoadPlayerData(string fileName)
    {
        string filePath = DATA_FOLDER + fileName + playerDataExtension;
        if (File.Exists(filePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(filePath, FileMode.Open);

            PlayerData loadedPlayerData = (PlayerData)formatter.Deserialize(stream);
            stream.Close();
            return loadedPlayerData;
        }
        else
        {
            Debug.LogWarning("Could not find file '" + filePath + "', saving and loading defaults.");

            defaultPlayerData.SavePlayerData(fileName, false);

            return defaultPlayerData;
        }
    }

    public static void SaveChatSettings(this ChatSettings fromSettings, string fileName)
    {
        string json = JsonConvert.SerializeObject(fromSettings, Formatting.Indented);

        File.WriteAllText(SETTINGS_FOLDER + fileName + chatSettingsExtension, json);
    }

    public static ChatSettings LoadChatSettings(string fileName)
    {
        if (File.Exists(SETTINGS_FOLDER + fileName + chatSettingsExtension))
        {
            string json = File.ReadAllText(SETTINGS_FOLDER + fileName + chatSettingsExtension);

            if (json != null)
            {
                return JsonConvert.DeserializeObject<ChatSettings>(json);
            }
        }
        else
        {
            Debug.LogWarning("Could not find file '" + SETTINGS_FOLDER + fileName + chatSettingsExtension + "', saving and loading defaults.");

            defaultChatSettings.SaveChatSettings(fileName);
        }
        return defaultChatSettings;
    }

    public static void SavePlayerSettings(this PlayerSettings fromSettings, string fileName)
    {
        string json = JsonConvert.SerializeObject(fromSettings, Formatting.Indented);

        File.WriteAllText(SETTINGS_FOLDER + fileName + playerSettingsExtension, json);
    }

    public static PlayerSettings LoadPlayerSettings(string fileName)
    {
        if (File.Exists(SETTINGS_FOLDER + fileName + playerSettingsExtension))
        {
            string json = File.ReadAllText(SETTINGS_FOLDER + fileName + playerSettingsExtension);

            if (json != null)
            {
                return JsonConvert.DeserializeObject<PlayerSettings>(json);
            }
        }
        else
        {
            Debug.LogWarning("Could not find file '" + SETTINGS_FOLDER + fileName + playerSettingsExtension + "', saving and loading defaults.");

            defaultPlayerSettings.SavePlayerSettings(fileName);
        }
        return defaultPlayerSettings;
    }

    public static void SaveRoomSettings(this RoomSettings fromSettings, string fileName)
    {
        string json = JsonConvert.SerializeObject(fromSettings, Formatting.Indented);

        File.WriteAllText(SETTINGS_FOLDER + fileName + roomSettingsExtension, json);
    }

    public static RoomSettings LoadRoomSettings(string fileName)
    {
        RoomSettings newSettings = new RoomSettings();
        if (File.Exists(SETTINGS_FOLDER + fileName + roomSettingsExtension))
        {
            string json = File.ReadAllText(SETTINGS_FOLDER + fileName + roomSettingsExtension);

            if (json != null)
            {
                RoomSettings loadedSettings = JsonConvert.DeserializeObject<RoomSettings>(json);

                newSettings = loadedSettings;
            }
        }
        else
        {
            Debug.LogWarning("Could not find file '" + SETTINGS_FOLDER + fileName + roomSettingsExtension + "', saving and loading defaults.");

            defaultRoomSettings.SaveRoomSettings(fileName);

            newSettings = defaultRoomSettings;
        }
        return newSettings;
    }

    public static void SaveTempLevel(LevelInfo levelInfo)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(Application.temporaryCachePath + "/temp.level", FileMode.Create);
        formatter.Serialize(stream, levelInfo);
        stream.Close();
    }

    public static void SaveLevel(string levelName, string levelDescription, string levelCreators, SaveableLevelObject[] levelObjects)
    {
        LevelInfo levelInfo = new LevelInfo()
        {
            name = levelName,
            description = levelDescription,
            creators = levelCreators,
            levelObjects = new List<LevelObjectInfo>(),
        };

        foreach (SaveableLevelObject levelObject in levelObjects)
        {
            if (levelObject.CompareTag("Spawnpoint"))
            {
                Color color = levelObject.GetComponent<MeshRenderer>().material.color;
                int spawnpointType = levelObject.name switch
                {
                    "Players" => 0,
                    "Bots" => 1,
                    _ => 2,
                };
                levelInfo.levelObjects.Add(new LevelObjectInfo()
                {
                    ID = levelObject.GetInstanceID(),
                    prefabIndex = levelObject.prefabIndex,
                    name = levelObject.name,
                    tag = levelObject.tag,
                    layer = levelObject.gameObject.layer,
                    posX = levelObject.transform.position.x,
                    posY = levelObject.transform.position.y,
                    posZ = levelObject.transform.position.z,
                    eulerX = levelObject.transform.eulerAngles.x,
                    eulerY = levelObject.transform.eulerAngles.y,
                    eulerZ = levelObject.transform.eulerAngles.z,
                    scaleX = levelObject.transform.localScale.x,
                    scaleY = levelObject.transform.localScale.y,
                    scaleZ = levelObject.transform.localScale.z,
                    colorR = color.r,
                    colorG = color.g,
                    colorB = color.b,
                    colorA = color.a,
                    spawnType = spawnpointType,
                });
            }
            else
            {
                levelInfo.levelObjects.Add(new LevelObjectInfo()
                {
                    ID = levelObject.GetInstanceID(),
                    prefabIndex = levelObject.prefabIndex,
                    name = levelObject.name,
                    tag = levelObject.tag,
                    layer = levelObject.gameObject.layer,
                    posX = levelObject.transform.position.x,
                    posY = levelObject.transform.position.y,
                    posZ = levelObject.transform.position.z,
                    eulerX = levelObject.transform.eulerAngles.x,
                    eulerY = levelObject.transform.eulerAngles.y,
                    eulerZ = levelObject.transform.eulerAngles.z,
                    scaleX = levelObject.transform.localScale.x,
                    scaleY = levelObject.transform.localScale.y,
                    scaleZ = levelObject.transform.localScale.z,
                });
            }
        }

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(LEVELS_FOLDER + levelName + ".level", FileMode.Create);

        formatter.Serialize(stream, levelInfo);
        stream.Close();
        GameManager.Instance.ShowPopup("Saved to " + LEVELS_FOLDER + levelName + ".level", Color.yellow, new Color(1, 0.92f, 0.016f, 0.5f), 2.5f);
    }

    public static LevelInfo LoadTempLevel()
    {
        LevelInfo levelInfo = null;
        string filePath = Application.temporaryCachePath + "/temp.level";
        if(File.Exists(filePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(filePath, FileMode.Open);

            levelInfo = (LevelInfo)formatter.Deserialize(stream);
            stream.Close();
        }
        else
        {
            Debug.LogWarning("Could not find file '" + filePath + "'.");
        }

        return levelInfo;
    }

    public static LevelInfo LoadLevel(string fileName)
    {
        LevelInfo levelInfo = null;
        string filePath = LEVELS_FOLDER + fileName + ".level";
        if (File.Exists(filePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(filePath, FileMode.Open);

            levelInfo = (LevelInfo)formatter.Deserialize(stream);
            stream.Close();
            GameManager.Instance.ShowPopup("Loaded from " + LEVELS_FOLDER + fileName + ".level", Color.yellow, new Color(1, 0.92f, 0.016f, 0.5f), 2.5f);
        }
        else
        {
            Debug.LogWarning("Could not find file '" + filePath + "'.");
            GameManager.Instance.ShowPopup("Could not find file " + LEVELS_FOLDER + fileName + ".level", Color.red, new Color(1, 0.675f, 0.675f, 0.5f), 2.5f);
        }

        return levelInfo;
    }

    public static void DeleteFile(string fullFileName)
    {
        if(File.Exists(SAVE_FOLDER + fullFileName))
        {
            File.Delete(SAVE_FOLDER + fullFileName);
        }
    }

    public static string LatestFileInSaveFolder(bool returnExtension, string fileExtension = "")
    {
        FileInfo latestFile = new DirectoryInfo(SAVE_FOLDER).GetFiles("*" + fileExtension, SearchOption.AllDirectories).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();

        if(latestFile != null)
        {
            if(returnExtension)
            {
                return latestFile.Name;
            }
            else
            {
                return Path.GetFileNameWithoutExtension(latestFile.Name);
            }
        }
        else
        {
            return null;
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
