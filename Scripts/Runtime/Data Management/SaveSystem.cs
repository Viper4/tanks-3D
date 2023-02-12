using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
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
            { "Debug Menu", KeyCode.F3 },
        },
        targetFramerate = 60,
        silhouettes = true,
        masterVolume = 100,
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

    public static PlayerData ResetPlayerData(PlayerData playerData)
    {
        PlayerData resetData = playerData;
        float bestTime = playerData.bestTime;

        resetData.lives = defaultPlayerData.lives;
        resetData.kills = defaultPlayerData.kills;
        resetData.shots = defaultPlayerData.shots;
        resetData.deaths = defaultPlayerData.deaths;
        resetData.time = defaultPlayerData.time;
        resetData.bestTime = bestTime;
        resetData.sceneIndex = defaultPlayerData.sceneIndex;
        resetData.previousSceneIndex = defaultPlayerData.previousSceneIndex;

        return playerData;
    }
}
