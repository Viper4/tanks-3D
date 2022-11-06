using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomSettings
{
    public bool isPublic;
    public string map;
    public string mode;
    public int teamLimit;
    public int teamSize;
    public int difficulty;
    public int playerLimit;
    public List<string> bots;
    public int botLimit;
    public bool fillLobby;
    public List<string> boosts;
    public int boostLimit;
    public int totalLives;
}
