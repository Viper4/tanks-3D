using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(ObjectCreation))]
public class LevelEditor : Editor
{
    [SerializeField] Vector3[] bounds;
    
    [SerializeField] int tankLimit;
    [SerializeField] List<Transform> tanks = new List<Transform>();
    
    [SerializeField] int obstacleLimit;
    [SerializeField] List<Transform> obstacles = new List<Transform>();
    [SerializeField] float branchChance = 0.5f;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        GUILayout.BeginHorizontal();

        if (targets.Length == 1)
        {
            ObjectCreation objectCreation = (ObjectCreation)target;

            if (GUILayout.Button("Extend"))
            {
                objectCreation.Extend();
            }

            if (GUILayout.Button("Delete"))
            {
                objectCreation.Delete();
            }

            if (GUILayout.Button("Undo"))
            {
                objectCreation.Undo();
            }
            
            if (GUILayout.Button("Generate Random"))
            {
                objectCreation.GenerateRandom(tankLimit, tanks, obstacleLimit, obstacles, branchChance);
            }
        }
        else if (targets.Length > 1)
        {
            ObjectCreation[] objectCreations = new ObjectCreation[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                objectCreations[i] = (ObjectCreation)targets[i];
            }

            if (GUILayout.Button("Extend"))
            {
                foreach (ObjectCreation objectCreation in objectCreations)
                {
                    objectCreation.Extend();
                }
            }

            if (GUILayout.Button("Delete"))
            {
                foreach (ObjectCreation objectCreation in objectCreations)
                {
                    objectCreation.Delete();
                }
            }

            if (GUILayout.Button("Undo"))
            {
                foreach (ObjectCreation objectCreation in objectCreations)
                {
                    objectCreation.Undo();
                }
            }
        }
        else
        {
            
        }
        
        GUILayout.EndHorizontal();
    }
}
