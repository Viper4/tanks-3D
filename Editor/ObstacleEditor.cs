using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(ObstacleGeneration))]
public class ObstacleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        GUILayout.BeginHorizontal();

        if (targets.Length == 1)
        {
            ObstacleGeneration obstacle = (ObstacleGeneration)target;

            if (GUILayout.Button("Extend"))
            {
                obstacle.Extend();
            }

            if (GUILayout.Button("Delete"))
            {
                obstacle.Delete();
            }

            if (GUILayout.Button("Undo"))
            {
                obstacle.Undo();
            }

            if (GUILayout.Button("Clear"))
            {
                obstacle.Clear();
            }

            if (GUILayout.Button("Generate Random"))
            {
                GameObject.Find("Level").GetComponent<LevelGenerator>().GenerateObstacles(obstacle);
            }
        }
        else if (targets.Length > 1)
        {
            ObstacleGeneration[] obstacles = new ObstacleGeneration[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                obstacles[i] = (ObstacleGeneration)targets[i];
            }

            if (GUILayout.Button("Extend"))
            {
                foreach (ObstacleGeneration obstacle in obstacles)
                {
                    obstacle.Extend();
                }
            }

            if (GUILayout.Button("Delete"))
            {
                foreach (ObstacleGeneration obstacle in obstacles)
                {
                    obstacle.Delete();
                }
            }

            if (GUILayout.Button("Undo"))
            {
                foreach (ObstacleGeneration obstacle in obstacles)
                {
                    obstacle.Undo();
                }
            }
        }
        
        GUILayout.EndHorizontal();
    }
}
