using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class FindMissingScripts : EditorWindow
{
    int gameObjectCount = 0, missingCount = 0;

    [MenuItem("Tools/Find Missing Scripts")]
    static void CreateReplaceWithPrefab()
    {
        GetWindow<FindMissingScripts>();
    }

    public void OnGUI()
    {
        if (GUILayout.Button("Find and Remove in Selected GameObjects"))
        {
            GameObject[] gameObjects = Selection.gameObjects;
            gameObjectCount = 0;
            missingCount = 0;
            foreach (GameObject GO in gameObjects)
            {
                FindInGO(GO, false);
            }
            Debug.Log($"Searched {gameObjectCount} GameObjects, found and removed {missingCount} missing");
        }
        else if (GUILayout.Button("Find and Remove in Current Scene"))
        {
            gameObjectCount = 0;
            missingCount = 0;

            FindInScene(false);

            Debug.Log($"Searched {gameObjectCount} GameObjects, found and removed {missingCount} missing");
        }
        else if (GUILayout.Button("Find and Remove in All Scenes"))
        {
            gameObjectCount = 0;
            missingCount = 0;
            string scenePath = SceneManager.GetActiveScene().path;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                FindInScene(true);
                EditorSceneManager.SaveScene(SceneManager.GetSceneByPath(scenePath));
            }
            Debug.Log($"Searched {gameObjectCount} GameObjects in {scenePath}, found and removed {missingCount} missing");
        }
    }

    private void FindInGO(GameObject GO, bool markDirty)
    {
        gameObjectCount++;

        int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(GO);
        if (missing > 0)
        {
            if (markDirty)
                EditorUtility.SetDirty(GO);
            missingCount += missing;
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(GO);
        }
    }

    private void FindInScene(bool markDirty)
    {
        foreach (GameObject GO in FindObjectsOfType<GameObject>())
        {
            FindInGO(GO, markDirty);
        }
    }
}