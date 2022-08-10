using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using MyUnityAddons.Math;

public class MergeBoxColliders : EditorWindow
{
    List<List<BoxCollider>> touchingBoxColliders = new List<List<BoxCollider>>();
    List<BoxCollider> touchingColliders = new List<BoxCollider>();
    List<Transform> selection = new List<Transform>();

    [SerializeField] LayerMask ignoreLayerMask;
    readonly string[] options = { "Default", "TransparentFX", "Ignore Raycast", "Bullet", "Water", "UI", "Tank", "Player", "Mine", "Barrier", "Mine Radius", "Brown Tank", "Grey Tank", "Teal Tank", "Yellow Tank", "Red Tank", "Green Tank" };

    [MenuItem("Tools/Merge Box Colliders")]
    static void CreateMergeBoxColliders()
    {
        GetWindow<MergeBoxColliders>();
    }

    List<BoxCollider> GetTouchingColliders(BoxCollider boxCollider, Vector3[] directions, float[] distances)
    {
        List<BoxCollider> thisTouchingColliders = new List<BoxCollider>();
        for (int i = 0; i < directions.Length; i++)
        {
            BoxCollider collider = TestDirection(boxCollider.transform.position, directions[i], distances[i]);
            if (collider != null)
            {
                touchingColliders.Add(collider);
                thisTouchingColliders.Add(collider);
            }
        }
        return thisTouchingColliders;
    }

    BoxCollider TestDirection(Vector3 origin, Vector3 direction, float maxDistance)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance + 0.01f, ~ignoreLayerMask, QueryTriggerInteraction.Collide))
        {
            if (hit.transform.TryGetComponent<BoxCollider>(out var hitCollider))
            {
                if (selection.Contains(hitCollider.transform))
                {
                    Debug.DrawLine(origin, hit.point, Color.red, 10f);
                    selection.Remove(hit.transform);

                    return hitCollider;
                }
            }
        }
        return null;
    }

    void GetNextColliders(List<BoxCollider> next)
    {
        if (next.Count > 0)
        {
            foreach (BoxCollider nextCollider in next.ToList())
            {
                List<BoxCollider> newNext = GetTouchingColliders(nextCollider, new Vector3[] { nextCollider.transform.right, -nextCollider.transform.right, nextCollider.transform.up, -nextCollider.transform.up, nextCollider.transform.forward, -nextCollider.transform.forward }, new float[] { nextCollider.bounds.extents.x, nextCollider.bounds.extents.x, nextCollider.bounds.extents.y, nextCollider.bounds.extents.y, nextCollider.bounds.extents.z, nextCollider.bounds.extents.z });
                GetNextColliders(newNext);
            }
        }
    }

    void SelectionLoop()
    {
        for (int i = 0; i < selection.Count; i++)
        {
            if (selection[i].TryGetComponent<BoxCollider>(out var newBoxCollider))
            {
                Debug.DrawLine(selection[i].position, selection[i].position + selection[i].up * 3, Color.cyan, 10f);
                touchingColliders.Clear();

                GetTouchingColliders(newBoxCollider, new Vector3[] { newBoxCollider.transform.right, -newBoxCollider.transform.right, newBoxCollider.transform.up, -newBoxCollider.transform.up, newBoxCollider.transform.forward, -newBoxCollider.transform.forward }, new float[] { newBoxCollider.bounds.extents.x, newBoxCollider.bounds.extents.x, newBoxCollider.bounds.extents.y, newBoxCollider.bounds.extents.y, newBoxCollider.bounds.extents.z, newBoxCollider.bounds.extents.z });
                selection.RemoveAt(0);
                foreach (BoxCollider touchingCollider in touchingColliders.ToList())
                {
                    List<BoxCollider> next = GetTouchingColliders(touchingCollider, new Vector3[] { touchingCollider.transform.right, -touchingCollider.transform.right, touchingCollider.transform.up, -touchingCollider.transform.up, touchingCollider.transform.forward, -touchingCollider.transform.forward }, new float[] { touchingCollider.bounds.extents.x, touchingCollider.bounds.extents.x, touchingCollider.bounds.extents.y, touchingCollider.bounds.extents.y, touchingCollider.bounds.extents.z, touchingCollider.bounds.extents.z });
                    GetNextColliders(next);
                }

                touchingColliders.Add(newBoxCollider);
                touchingBoxColliders.Add(touchingColliders.ToList());
                SelectionLoop();
                return;
            }
        }
    }

    private void OnGUI()
    {
        ignoreLayerMask = EditorGUILayout.MaskField(ignoreLayerMask, options);

        if (GUILayout.Button("Merge"))
        {
            touchingBoxColliders.Clear();

            selection = Selection.transforms.ToList();

            if (selection.Count > 1)
            {
                SelectionLoop();

                foreach (List<BoxCollider> touchingColliders in touchingBoxColliders)
                {
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
        }

        GUI.enabled = false;
        EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);
    }
}