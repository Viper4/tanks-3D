using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class ReplaceWithPrefab : EditorWindow
{
    [SerializeField] private GameObject prefab;
    [SerializeField] bool keepChildren = true;
    [SerializeField] bool removeDuplicateChildren = true;

    [MenuItem("Tools/Replace With Prefab")]
    static void CreateReplaceWithPrefab()
    {
        CreateWindow<ReplaceWithPrefab>();
    }

    private void OnGUI()
    {
        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
        keepChildren = EditorGUILayout.Toggle("Keep Children", keepChildren);
        removeDuplicateChildren = EditorGUILayout.Toggle("Remove Duplicate Children", removeDuplicateChildren);

        if (GUILayout.Button("Replace"))
        {
            var selection = Selection.gameObjects;

            for (var i = selection.Length - 1; i >= 0; i--)
            {
                var selected = selection[i];
                var prefabType = PrefabUtility.GetPrefabAssetType(prefab);
                GameObject newObject;

                if (prefabType == PrefabAssetType.Regular)
                {
                    newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                }
                else
                {
                    newObject = Instantiate(prefab);
                    newObject.name = prefab.name;
                }

                if (newObject == null)
                {
                    Debug.LogError("Error instantiating prefab");
                    break;
                }

                Undo.RegisterCreatedObjectUndo(newObject, "Replace With Prefabs");
                newObject.transform.parent = selected.transform.parent;
                newObject.transform.localPosition = selected.transform.localPosition;
                newObject.transform.localRotation = selected.transform.localRotation;
                newObject.transform.localScale = selected.transform.localScale;
                newObject.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());
                if (keepChildren)
                {
                    List<Transform> newObjectChildren = newObject.transform.Cast<Transform>().ToList();
                    foreach (Transform child in selected.transform.Cast<Transform>().ToList())
                    {
                        Transform duplicate = newObjectChildren.FirstOrDefault(x => x.name == child.name);
                        if (duplicate == null)
                        {
                            Undo.SetTransformParent(child, newObject.transform, "Replace With Prefabs");
                        }
                    }
                }

                Undo.DestroyObjectImmediate(selected);
            }
        }

        GUI.enabled = false;
        EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);
    }
}