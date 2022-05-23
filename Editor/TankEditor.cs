using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TankGeneration))]
public class TankEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.BeginHorizontal();

        TankGeneration tank = (TankGeneration)target;

        if (GUILayout.Button("Clear"))
        {
            tank.Clear();
        }

        if (GUILayout.Button("Generate Random"))
        {
            GameObject.Find("Level").GetComponent<LevelGenerator>().GenerateTanks(tank);
        }

        GUILayout.EndHorizontal();
    }
}
