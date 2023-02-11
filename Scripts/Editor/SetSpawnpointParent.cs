using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class SetSpawnpointParent : EditorWindow
{
    [SerializeField] private GameObject parent;
    [SerializeField] private string parentName;
    [SerializeField] private string spawnType;

    enum SpawnpointType
    {
        Default,
        Teams
    }

    [MenuItem("Tools/Set Spawnpoint Parent")]
    static void CreateSetSpawnpointParent()
    {
        CreateWindow<SetSpawnpointParent>();
    }

    private void OnGUI()
    {
        parent = (GameObject)EditorGUILayout.ObjectField("Parent", parent, typeof(GameObject), false);
        parentName = EditorGUILayout.TextField("Parent Name", parentName);
        spawnType = EditorGUILayout.TextField("Spawn Type", spawnType);

        if (GUILayout.Button("Set Parent"))
        {
            if(parent == null)
            {
                parent = GameObject.Find(parentName);
            }

            GameObject[] selection = Selection.gameObjects;

            for (int i = 0; i < selection.Length; i++)
            {
                GameObject selected = selection[i];
                switch (spawnType)
                {
                    case "Teams":
                        selected.transform.SetParent(parent.transform.Find("Spawnpoints/Teams"));
                        break;
                    default:
                        selected.transform.SetParent(parent.transform.Find("Spawnpoints/Default"));
                        break;
                }
            }
            EditorUtility.SetDirty(parent);
        }

        GUI.enabled = false;
        EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);
    }
}