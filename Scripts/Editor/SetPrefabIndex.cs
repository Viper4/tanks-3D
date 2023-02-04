using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SetPrefabIndex : EditorWindow
{
    [SerializeField] private GameManager gameManager;

    [MenuItem("Tools/Set Prefab Index")]
    static void CreateSetPrefabIndex()
    {
        CreateWindow<SetPrefabIndex>();
    }

    private void OnGUI()
    {
        gameManager = (GameManager)EditorGUILayout.ObjectField("GameManager", gameManager, typeof(GameManager), false);

        if (GUILayout.Button("Set Indices"))
        {
            var selection = Selection.gameObjects;

            foreach(GameObject gameObject in selection)
            {
                if(gameObject.TryGetComponent<SaveableLevelObject>(out var levelObject))
                {
                    levelObject.prefabIndex = gameManager.editorPrefabs.IndexOf(gameObject);
                    EditorUtility.SetDirty(gameObject);
                }
            }
        }
    }
}
