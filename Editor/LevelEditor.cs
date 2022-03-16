using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(ObjectCreation))]
public class LevelEditor : Editor
{
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
        }
        else
        {
            ObjectCreation[] objectCreations = new ObjectCreation[targets.Length - 1];

            for (int i = 0; i < targets.Length - 1; i++)
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

        GUILayout.EndHorizontal();
    }
}
