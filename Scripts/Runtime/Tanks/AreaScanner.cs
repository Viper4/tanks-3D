using MyUnityAddons.Calculations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AreaScanner : MonoBehaviour
{
    public Transform selectedObject { get; private set; } = null;

    [SerializeField] float heightLimit = 5;
    [SerializeField] float viewDistance = 50;
    [SerializeField] bool showRays = false;
    [SerializeField] float drawDuration = 2.5f;
    public LayerMask obstructLayerMask;

    [SerializeField] enum SelectionMode
    {
        Random,
        [Tooltip("Position with closest angle to origin forward")] Closest,
        [Tooltip("Position with farthest angle to origin forward")] Farthest,
        [Tooltip("Shortest straight line distance")] ShortestLine,
        [Tooltip("Longest straight line distance")] LongestLine,
        [Tooltip("Get closest position then closest position from that angle and repeat")] AlternateClose,
        [Tooltip("Get farthest position then farthest position from that angle and repeat")] AlternateFar,
        [Tooltip("Next index in positions")] Next,
        [Tooltip("Next index in closest positions")] NextClosest,
        [Tooltip("Next index in farthest positions")] NextFarthest,
        [Tooltip("Next index in shortest straight line positions")] NextShortestLine,
        [Tooltip("Next index in longest straight line positions")] NextLongestLine,
    }
    [SerializeField] SelectionMode selectionMode = SelectionMode.Closest;
    int objectIndex = -1;

    public List<Transform> GetVisibleObjects(Transform origin, LayerMask objectLayerMask)
    {
        List<Transform> visibleObjects = new List<Transform>();
        Collider[] overlappingColliders = Physics.OverlapSphere(origin.position, viewDistance, objectLayerMask);
        foreach(Collider collider in overlappingColliders)
        {
            if(Mathf.Abs(collider.bounds.min.y - origin.position.y) < heightLimit)
            {
                Vector3[] vertices = collider.GetComponent<MeshFilter>().sharedMesh.vertices;
                foreach(Vector3 vertex in vertices)
                {
                    if(Physics.Raycast(origin.position, vertex - origin.position, out RaycastHit hit, viewDistance, objectLayerMask))
                    {
                        if(hit.transform == collider.transform)
                        {
                            if(showRays)
                            {
                                Debug.DrawLine(origin.position, hit.point, Color.magenta, drawDuration);
                            }
                            visibleObjects.Add(hit.transform);
                            break;
                        }
                        else if(showRays)
                        {
                            Debug.DrawLine(origin.position, hit.point, Color.red, drawDuration);
                        }
                    }
                }
            }
        }
        return visibleObjects;
    }

    public List<Transform> GetVisibleObjectsNotNear(Transform origin, LayerMask objectLayerMask, LayerMask nearLayerMask, float nearRadius)
    {
        List<Transform> visibleObjects = new List<Transform>();
        Collider[] overlappingColliders = Physics.OverlapSphere(origin.position, viewDistance, objectLayerMask);
        foreach(Collider collider in overlappingColliders)
        {
            if(!Physics.CheckSphere(collider.bounds.center, nearRadius, nearLayerMask) && Mathf.Abs(collider.bounds.min.y - origin.position.y) < heightLimit)
            {
                foreach(Vector3 vertex in collider.Vertices())
                {
                    if(Physics.Raycast(origin.position, vertex - origin.position, out RaycastHit hit, viewDistance, objectLayerMask | obstructLayerMask))
                    {
                        if(hit.transform == collider.transform)
                        {
                            if(showRays)
                            {
                                Debug.DrawLine(origin.position, hit.point, Color.magenta, drawDuration);
                            }
                            visibleObjects.Add(hit.transform);
                            break;
                        }
                        else if(showRays)
                        {
                            Debug.DrawLine(origin.position, hit.point, Color.red, drawDuration);
                        }
                    }
                }
            }
        }
        return visibleObjects;
    }

    public List<Transform> GetVisibleObjectsNotNear(Transform origin, LayerMask objectLayerMask, string objectTag, LayerMask nearLayerMask, float nearRadius)
    {
        List<Transform> visibleObjects = new List<Transform>();
        Collider[] overlappingColliders = Physics.OverlapSphere(origin.position, viewDistance, objectLayerMask);
        foreach(Collider collider in overlappingColliders)
        {
            if(collider.transform.CompareTag(objectTag) && !Physics.CheckSphere(collider.bounds.center, nearRadius, nearLayerMask) && Mathf.Abs(collider.bounds.min.y - origin.position.y) < heightLimit)
            {
                foreach(Vector3 vertex in collider.Vertices())
                {
                    if(Physics.Raycast(origin.position, vertex - origin.position, out RaycastHit hit, viewDistance, objectLayerMask | obstructLayerMask))
                    {
                        if(hit.transform == collider.transform)
                        {
                            if(showRays)
                            {
                                Debug.DrawLine(origin.position, hit.point, Color.magenta, drawDuration);
                            }
                            visibleObjects.Add(hit.transform);
                            break;
                        }
                        else if(showRays)
                        {
                            Debug.DrawLine(origin.position, hit.point, Color.red, drawDuration);
                        }
                    }
                }
            }
        }
        return visibleObjects;
    }

    public List<Transform> GetVisibleObjects(Transform origin, LayerMask objectLayerMask, string objectTag)
    {
        List<Transform> visibleObjects = new List<Transform>();
        Collider[] overlappingColliders = Physics.OverlapSphere(origin.position, viewDistance, objectLayerMask);

        foreach(Collider collider in overlappingColliders)
        {
            if(collider.transform.CompareTag(objectTag) && Mathf.Abs(collider.bounds.min.y - origin.position.y) < heightLimit)
            {
                foreach(Vector3 vertex in collider.Vertices())
                {
                    if(Physics.Raycast(origin.position, vertex - origin.position, out RaycastHit hit, viewDistance, objectLayerMask | obstructLayerMask))
                    {
                        if(hit.transform == collider.transform)
                        {
                            if(showRays)
                            {
                                Debug.DrawLine(origin.position, hit.point, Color.magenta, drawDuration);
                            }
                            visibleObjects.Add(hit.transform);
                            break;
                        }
                        else if(showRays)
                        {
                            Debug.DrawLine(origin.position, hit.point, Color.red, drawDuration);
                        }
                    }
                }
            }
        }
        return visibleObjects;
    }

    public void SelectObject(Transform origin, List<Transform> objectList)
    {
        if(objectList.Count > 0)
        {
            objectIndex++;
            if(objectIndex > objectList.Count - 1)
            {
                objectIndex = 0;
            }

            switch(selectionMode)
            {
                case SelectionMode.Random:
                    selectedObject = objectList[Random.Range(0, objectList.Count)];
                    break;
                case SelectionMode.Closest:
                    selectedObject = origin.ClosestAngleTransform(objectList);
                    break;
                case SelectionMode.Farthest:
                    selectedObject = origin.FarthestAngleTransform(objectList);
                    break;
                case SelectionMode.ShortestLine:
                    selectedObject = objectList.OrderBy((x) => CustomMath.SqrDistance(x.position, origin.position)).ToList().FirstOrDefault();
                    break;
                case SelectionMode.LongestLine:
                    selectedObject = objectList.OrderByDescending((x) => CustomMath.SqrDistance(x.position, origin.position)).ToList().FirstOrDefault();
                    break;
                case SelectionMode.AlternateClose:
                    objectList.Remove(selectedObject);
                    selectedObject = origin.ClosestAngleTransform(objectList);
                    break;
                case SelectionMode.AlternateFar:
                    objectList.Remove(selectedObject);
                    selectedObject = origin.FarthestAngleTransform(objectList);
                    break;
                case SelectionMode.Next:
                    selectedObject = objectList[objectIndex];
                    break;
                case SelectionMode.NextClosest:
                    List<Transform> closestVisibleObjects = objectList.OrderBy((x) => Vector3.Angle(x.position - origin.position, origin.forward)).ToList();
                    selectedObject = closestVisibleObjects[objectIndex];
                    break;
                case SelectionMode.NextFarthest:
                    List<Transform> farthestVisibleObjects = objectList.OrderByDescending((x) => Vector3.Angle(x.position - origin.position, origin.forward)).ToList();
                    selectedObject = farthestVisibleObjects[objectIndex];
                    break;
                case SelectionMode.NextShortestLine:
                    List<Transform> shortestVisibleObjects = objectList.OrderBy((x) => CustomMath.SqrDistance(x.position, origin.position)).ToList();
                    selectedObject = shortestVisibleObjects[objectIndex];
                    break;
                case SelectionMode.NextLongestLine:
                    List<Transform> longestVisibleObjects = objectList.OrderByDescending((x) => CustomMath.SqrDistance(x.position, origin.position)).ToList();
                    selectedObject = longestVisibleObjects[objectIndex];
                    break;
            }
        }
        else
        {
            selectedObject = null;
        }
    }
}
