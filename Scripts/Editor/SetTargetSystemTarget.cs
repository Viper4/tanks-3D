using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SetTargetSystemTarget : EditorWindow
{
    [SerializeField] Transform target;
    [SerializeField] private string targetName;
    [SerializeField] bool allTanks = true;
    [SerializeField] bool setPrimaryTarget = true;
    [SerializeField] bool setCurrentTarget = false;

    [MenuItem("Tools/Set Target System Target")]
    static void CreateSetTargetSystemTarget()
    {
        CreateWindow<SetTargetSystemTarget>();
    }

    private void OnGUI()
    {
        target = (Transform)EditorGUILayout.ObjectField("Target", target, typeof(Transform), true);
        targetName = EditorGUILayout.TextField("Target Name", targetName);
        allTanks = EditorGUILayout.Toggle("All Tanks", allTanks);
        setPrimaryTarget = EditorGUILayout.Toggle("Primary Target", setPrimaryTarget);
        setCurrentTarget = EditorGUILayout.Toggle("Current Target", setCurrentTarget);

        if (GUILayout.Button("Set Targets (Current Scene)"))
        {
            if (target != null)
            {
                SetTankTargets(target);
            }
            else
            {
                GameObject targetGO = GameObject.Find(targetName);
                if (targetGO != null)
                {
                    SetTankTargets(targetGO.transform);
                }
            }
        }
        else if (GUILayout.Button("Set Targets (All Build Scenes)"))
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                GameObject targetGO = GameObject.Find(targetName);
                if (targetGO != null)
                {
                    SetTankTargets(targetGO.transform);
                }
                EditorSceneManager.SaveScene(SceneManager.GetSceneByPath(scenePath));
            }
        }
        else if (GUILayout.Button("Close All Scenes"))
        {
            while (SceneManager.sceneCount > 1)
            {
                EditorSceneManager.CloseScene(SceneManager.GetSceneAt(1), true);
            }
        }
    }

    void SetTankTargets(Transform toTarget)
    {
        if (allTanks)
        {
            TargetSystem[] allTargetSystems = FindObjectsOfType<TargetSystem>();
            foreach (TargetSystem targetSystem in allTargetSystems)
            {
                Transform targetArea = targetSystem.GetTargetArea(toTarget.transform);
                if (setPrimaryTarget)
                {
                    targetSystem.primaryTarget = targetArea;
                }
                if (setCurrentTarget)
                {
                    targetSystem.currentTarget = targetArea;
                }
                EditorUtility.SetDirty(targetSystem);
            }
        }
        else
        {
            var selection = Selection.gameObjects;
            foreach (GameObject selected in selection)
            {
                if (selected.TryGetComponent<TargetSystem>(out var targetSystem))
                {
                    Transform targetArea = targetSystem.GetTargetArea(toTarget.transform);
                    if (setPrimaryTarget)
                    {
                        targetSystem.primaryTarget = targetArea;
                    }
                    if (setCurrentTarget)
                    {
                        targetSystem.currentTarget = targetArea;
                    }
                    EditorUtility.SetDirty(targetSystem);
                }
            }
        }
    }
}
