using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerSettings
{
    public float sensitivity;
    public bool cameraSmoothing;
    public float fieldOfView;
    public Dictionary<string, KeyCode> keyBinds;
    public int targetFramerate;
    public bool silhouettes;
    public float masterVolume;
    public int crosshairColorIndex;
    public float crosshairScale;
    public float slowZoomSpeed;
    public float fastZoomSpeed;
}
