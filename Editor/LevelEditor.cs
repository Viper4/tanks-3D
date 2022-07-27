using UnityEngine;
using UnityEditor;

public class LevelEditor : EditorWindow
{
    [SerializeField] int times = 1;
    [SerializeField] Vector3 eulerAngles;
    [SerializeField] Vector3 scale = new Vector3(2, 2, 2);
    [SerializeField] private Vector3 direction;
    [SerializeField] float distanceAway;

    [MenuItem("Tools/Level Editor")]
    static void CreateReplaceWithPrefab()
    {
        GetWindow<LevelEditor>();
    }

    private void OnGUI()
    {
        times = EditorGUILayout.IntField("Times", times);
        eulerAngles = EditorGUILayout.Vector3Field("Euler Angles", eulerAngles);
        scale = EditorGUILayout.Vector3Field("Scale", scale);
        direction = EditorGUILayout.Vector3Field("Direction", direction);
        distanceAway = EditorGUILayout.FloatField("Distance Away", distanceAway);

        if (GUILayout.Button("Clone"))
        {
            var selection = Selection.gameObjects;

            foreach (GameObject selected in selection)
            {
                // Iterate through all the times to clone this gameobject
                for (int i = 0; i < times; i++)
                {
                    // Instantiate this object at the given direction and distance away, reset its name and scale, and add the clone to the clonedObjects list
                    GameObject clone = Instantiate(selected, selected.transform.position + direction * (distanceAway * (i + 1)), Quaternion.Euler(eulerAngles), selected.transform.parent);
                    clone.name = selected.name;
                    clone.transform.localScale = scale;
                    Undo.RegisterCreatedObjectUndo(clone, "Cloned");
                }
            }
        }

        if (GUILayout.Button("Delete"))
        {
            var selection = Selection.gameObjects;

            foreach (GameObject selected in selection)
            {
                Undo.DestroyObjectImmediate(selected);
            }
        }

        if (GUILayout.Button("Generate Random"))
        {
            var selection = Selection.gameObjects;

            LevelGenerator levelGenerator = FindObjectOfType<LevelGenerator>();
            levelGenerator.GenerateObstacles(selection[0].GetComponent<ObstacleGeneration>());
        }

        GUI.enabled = false;
        EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);
    }
}

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

