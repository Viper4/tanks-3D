using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerSettings
{
    public string username;
    public float sensitivity;
    public float cameraSmoothing;
    public float fieldOfView;
    public Dictionary<string, KeyCode> keyBinds;
    public int targetFramerate;
    public bool silhouettes;
    public float masterVolume;
    public string crosshairFileName;
    public int crosshairColorIndex;
    public float crosshairScale;
    public float slowZoomSpeed;
    public float fastZoomSpeed;
}
