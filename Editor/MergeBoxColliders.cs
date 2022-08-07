using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using MyUnityAddons.Math;

public class MergeBoxColliders : EditorWindow
{
    [SerializeField] LayerMask ignoreLayerMask;
    readonly string[] options = { "Default", "TransparentFX", "Ignore Raycast", "Bullet", "Water", "UI", "Tank", "Player", "Mine", "Barrier", "Mine Radius", "Brown Tank", "Grey Tank", "Teal Tank", "Yellow Tank", "Red Tank", "Green Tank" };

    [MenuItem("Tools/Merge Box Colliders")]
    static void CreateMergeBoxColliders()
    {
        GetWindow<MergeBoxColliders>();
    }

    int FindIndexOf(List<List<BoxCollider>> list, BoxCollider boxCollider)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Contains(boxCollider))
            {
                return i;
            }
        }
        return -1;
    }

    List<BoxCollider> GetTouchingColliders(Transform[] selection, BoxCollider boxCollider, Vector3[] directions, float[] distances)
    {
        List<BoxCollider> touchingColliders = new List<BoxCollider>();

        for (int i = 0; i < directions.Length; i++)
        {
            BoxCollider collider = TestDirection(selection, boxCollider.transform.position, directions[i], distances[i]);
            if (collider != null)
            {
                touchingColliders.Add(collider);
            }
        }
        return touchingColliders;
    }

    BoxCollider TestDirection(Transform[] selection, Vector3 origin, Vector3 direction, float maxDistance)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, ~ignoreLayerMask))
        {
            if (hit.transform.TryGetComponent<BoxCollider>(out var hitCollider))
            {
                if (selection.Contains(hitCollider.transform))
                {
                    Debug.DrawLine(origin, hit.point, Color.red, 5f);

                    return hitCollider;
                }
            }
        }
        return null;
    }

    private void OnGUI()
    {
        ignoreLayerMask = EditorGUILayout.MaskField(ignoreLayerMask, options);

        if (GUILayout.Button("Merge"))
        {
            Transform[] selection = Selection.transforms;

            if (selection.Length > 1)
            {
                List<BoxCollider> touchingColliders = new List<BoxCollider>();
                for (int i = 0; i < selection.Length; i++)
                {
                    if (selection[i].TryGetComponent<BoxCollider>(out var boxCollider))
                    {
                        touchingColliders.Union(GetTouchingColliders(selection, boxCollider, new Vector3[] { boxCollider.transform.right, -boxCollider.transform.right, boxCollider.transform.up, -boxCollider.transform.up, boxCollider.transform.forward, -boxCollider.transform.forward }, new float[] { boxCollider.bounds.extents.x, boxCollider.bounds.extents.x, boxCollider.bounds.extents.y, boxCollider.bounds.extents.y, boxCollider.bounds.extents.z, boxCollider.bounds.extents.z }));
                        touchingColliders.Add(boxCollider);
                    }
                }

                Debug.Log(touchingColliders.Count);

                Vector3 min = touchingColliders[0].bounds.min;
                Vector3 max = touchingColliders[0].bounds.max;

                for (int i = 1; i < touchingColliders.Count; i++)
                {
                    if (touchingColliders[i].bounds.min.x < min.x)
                    {
                        min.x = touchingColliders[i].bounds.min.x;
                    }
                    if (touchingColliders[i].bounds.min.y < min.y)
                    {
                        min.y = touchingColliders[i].bounds.min.y;
                    }
                    if (touchingColliders[i].bounds.min.z < min.z)
                    {
                        min.z = touchingColliders[i].bounds.min.z;
                    }

                    if (touchingColliders[i].bounds.max.x > max.x)
                    {
                        max.x = touchingColliders[i].bounds.max.x;
                    }
                    if (touchingColliders[i].bounds.max.y > max.y)
                    {
                        max.y = touchingColliders[i].bounds.max.y;
                    }
                    if (touchingColliders[i].bounds.max.z > max.z)
                    {
                        max.z = touchingColliders[i].bounds.max.z;
                    }
                    Undo.DestroyObjectImmediate(touchingColliders[i]);
                }

                BoxCollider firstCollider = touchingColliders[0];
                Undo.RegisterCompleteObjectUndo(firstCollider, "Merge Box Colliders");
                Vector3 size = (max - min).Divide(firstCollider.transform.localScale);
                Vector3 center = firstCollider.transform.InverseTransformPoint((max + min) * 0.5f); // Midpoint of max and min
                firstCollider.size = size;
                firstCollider.center = center;
            }
        }

        GUI.enabled = false;
        EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);
    }
}