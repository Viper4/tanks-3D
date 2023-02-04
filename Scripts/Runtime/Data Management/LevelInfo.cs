using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelInfo
{
    public string name;
    public string description;
    public string creators;
    public List<LevelObjectInfo> levelObjects;
}
