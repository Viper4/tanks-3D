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

    [System.Serializable]
    public struct LevelObjectInfo
    {
        public string prefabName;
        public string name;
        public string tag;
        public int layer;
        public float posX;
        public float posY;
        public float posZ;
        public float eulerX;
        public float eulerY;
        public float eulerZ;
        public float scaleX;
        public float scaleY;
        public float scaleZ;
    }
}
