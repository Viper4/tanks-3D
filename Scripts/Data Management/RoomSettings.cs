using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomSettings
{
    public string version;
    public string map;
    public string roomName;
    public string primaryMode;
    public string secondaryMode;
    public int teamLimit;
    public int teamSize;
    public int waveSize;
    public int roundAmount;
    public int difficulty;
    public int playerLimit;
    public List<string> bots;
    public int botLimit;
    public bool fillLobby;
}
