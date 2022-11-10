using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChatSettings
{
    public string username;
    public bool whitelistActive;
    public List<string> whitelist;
    public List<string> blacklist;
    public List<string> muteList;
}
