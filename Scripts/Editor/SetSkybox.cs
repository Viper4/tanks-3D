using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SetSkybox : EditorWindow
{
    [SerializeField] Material skyboxMaterial;

    [MenuItem("Tools/Set Skybox")]
    static void CreateSetPrefabIndex()
    {
        CreateWindow<SetSkybox>();
    }

    private void OnGUI()
    {
        skyboxMaterial = (Material)EditorGUILayout.ObjectField("Skybox Material", skyboxMaterial, typeof(Material), false);

        if (GUILayout.Button("Set For All Build Scenes"))
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                RenderSettings.skybox = skyboxMaterial;
                EditorUtility.SetDirty(RenderSettings.skybox);
                EditorSceneManager.SaveScene(SceneManager.GetSceneByPath(scenePath));
            }
        }
    }
}
