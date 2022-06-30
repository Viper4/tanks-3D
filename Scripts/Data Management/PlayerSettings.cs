using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerSettings
{
    public float sensitivity;
    public Dictionary<string, KeyCode> keyBinds;
    public bool silhouettes;
    public float masterVolume;
    public string crosshairFileName;
    public int crosshairColorIndex;
    public float crosshairScale;
}
