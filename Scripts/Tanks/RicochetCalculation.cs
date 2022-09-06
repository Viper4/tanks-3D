using MyUnityAddons.Calculations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RicochetCalculation : MonoBehaviour
{
    [SerializeField] bool showRays;

    [SerializeField] LayerMask mirrorLayerMask;
    [SerializeField] LayerMask nonObstructLayerMask;
    [SerializeField] float drawDuration = 1;
    [SerializeField] int ricochetPredictions = 1;
    [SerializeField] float viewDistance = 50;
    [SerializeField] float[] maxHorizontalAngle = new float[] {-180, 180};
    [SerializeField] float[] maxVerticalAngle = new float[] {-20, 20};

    Dictionary<Transform, List<List<Vector3>>> mirrorPositionPairs = new Dictionary<Transform, List<List<Vector3>>>();
    List<RaycastHit> mirrorHits = new List<RaycastHit>();

    public Dictionary<Vector3, float> shootPositions { get; private set; } = new Dictionary<Vector3, float>();
    private Vector3 previousShootPosition;
    public List<Vector3> lookPositions { get; private set; } = new List<Vector3>();

    public enum SelectionMode
    {
        Random,
        [Tooltip("Position with closest angle to origin forward")] Closest,
        [Tooltip("Position with farthest angle to origin forward")] Farthest,
        [Tooltip("Shortest bullet path distance")] Shortest,
        [Tooltip("Longest bullet path distance")] Longest,
        [Tooltip("Get closest position then closest position from that angle and repeat")] AlternateClose,
        [Tooltip("Get farthest position then farthest position from that angle and repeat")] AlternateFar,
        [Tooltip("Next index in positions")] Next,
        [Tooltip("Next index in closest positions")] NextClosest,
        [Tooltip("Next index in farthest positions")] NextFarthest,
        [Tooltip("Next index in shortest positions")] NextShortest,
        [Tooltip("Next index in longest positions")] NextLongest,
    }
    public SelectionMode selectionMode = SelectionMode.NextClosest;
    int shootIndex = -1;

    Vector3 Mirror(Vector3 inDirection, RaycastHit mirrorHit, float mirrorDistance = 0)
    {
        if (mirrorDistance == 0)
        {
            mirrorDistance = mirrorHit.distance;
        }

        Vector3 reflectedVector = Vector3.Reflect(-inDirection, mirrorHit.normal);
        return mirrorHit.point + (reflectedVector * mirrorDistance);
    }

    public void ScanArea(Vector3 origin)
    {
        mirrorPositionPairs.Clear();
        mirrorHits.Clear();

        // Finding valid mirrors by casting rays to all colliders within sphere
        Collider[] overlappingColliders = Physics.OverlapSphere(origin, viewDistance, mirrorLayerMask, QueryTriggerInteraction.Ignore);
        foreach (Collider collider in overlappingColliders)
        {
            if (Physics.Raycast(origin, collider.bounds.center - origin, out RaycastHit mirrorHit, viewDistance, mirrorLayerMask, QueryTriggerInteraction.Ignore) || // Center
                Physics.Raycast(origin, collider.bounds.min - origin, out mirrorHit, viewDistance, mirrorLayerMask, QueryTriggerInteraction.Ignore) || // Min
                Physics.Raycast(origin, collider.bounds.max - origin, out mirrorHit, viewDistance, mirrorLayerMask, QueryTriggerInteraction.Ignore)) // Max)
            {
                // Populating first ricochet mirror positions
                if (!mirrorPositionPairs.ContainsKey(mirrorHit.transform))
                {
                    Vector3 mirroredPosition = Mirror((mirrorHit.point - origin).normalized, mirrorHit);
                    mirrorPositionPairs.Add(mirrorHit.transform, new List<List<Vector3>>());
                    for (int k = 0; k < ricochetPredictions; k++)
                    {
                        mirrorPositionPairs[mirrorHit.transform].Add(new List<Vector3>() { mirroredPosition });
                    }
                    mirrorHits.Add(mirrorHit);

                    if (showRays)
                    {
                        Debug.DrawLine(origin, mirrorHit.point, Color.magenta, drawDuration * 2);
                        Debug.DrawLine(mirrorHit.point, mirroredPosition, Color.yellow, drawDuration * 2);
                    }
                }
                else if (showRays)
                {
                    Debug.DrawLine(origin, mirrorHit.point, Color.red, drawDuration * 2);
                }
            }
        }

        // Skip first ricochet mirror positions
        for (int i = 1; i < ricochetPredictions; i++)
        {
            foreach (Transform mirror in mirrorPositionPairs.Keys)
            {
                foreach (RaycastHit mirrorHit in mirrorHits)
                {
                    foreach (Vector3 lastMirrorPosition in mirrorPositionPairs[mirror][i - 1])
                    {
                        Vector3 mirroredPosition = Mirror(mirrorHit.point - lastMirrorPosition, mirrorHit, 1);
                        if (!mirrorPositionPairs[mirrorHit.transform][i].Contains(mirroredPosition))
                        {
                            mirrorPositionPairs[mirrorHit.transform][i].Add(mirroredPosition);
                            if (showRays)
                            {
                                Debug.DrawLine(mirroredPosition, mirrorHit.point, Color.blue, drawDuration * 2);
                            }
                        }
                    }
                }
            }
        }
    }

    void TestAngles(Transform origin, RaycastHit reflectHit, Vector3 reflectedDestination, float totalDistance)
    {
        // Check if origin can rotate to shoot position given max horizontal and vertical angles
        Vector3 incitingDir = reflectHit.point - origin.position;
        if (origin.CanRotateTo(incitingDir, maxVerticalAngle, maxHorizontalAngle))
        {
            // Comparing inciting and exiting angle on vertical axis and horizontal axis relative to hit normal
            float[] incitingAngles = new float[]
            {
                Vector3.SignedAngle(incitingDir, reflectHit.normal, Vector3.up),
                Vector3.SignedAngle(incitingDir, reflectHit.normal, Quaternion.AngleAxis(90, Vector3.up) * reflectHit.normal)
            };

            Vector3 exitingDir = reflectHit.point - reflectedDestination;
            float[] exitingAngles = new float[]
            {
                Vector3.SignedAngle(exitingDir, reflectHit.normal, -Vector3.up),
                Vector3.SignedAngle(exitingDir, reflectHit.normal, Quaternion.AngleAxis(-90, Vector3.up) * reflectHit.normal)
            };

            // Checking if inciting and exiting angles are the same sign and are within 0.1 degrees
            if (incitingAngles[0] * exitingAngles[0] >= 0 && incitingAngles[1] * exitingAngles[1] >= 0 &&
                Mathf.Abs(incitingAngles[0] - exitingAngles[0]) < 0.1f && Mathf.Abs(incitingAngles[1] - exitingAngles[1]) < 0.1f)
            {
                shootPositions.AddOrReplace(reflectHit.point, totalDistance);

                if (showRays)
                {
                    Debug.DrawLine(origin.position, reflectHit.point, Color.green, drawDuration);
                }
            }
            else
            {
                lookPositions.Add(reflectHit.point);

                if (showRays)
                {
                    Debug.DrawLine(origin.position, reflectHit.point, Color.cyan, drawDuration * 0.5f);
                }
            }
        }
    }

    public void CalculateBulletRicochets(Transform origin, Vector3 destination)
    {
        shootPositions.Clear();
        lookPositions.Clear();
        float halfDrawDuration = drawDuration * 0.5f;
        foreach (Transform mirror in mirrorPositionPairs.Keys)
        {
            foreach (Vector3 mirroredPosition in mirrorPositionPairs[mirror][ricochetPredictions - 1])
            {
                Vector3 LOSDir = mirroredPosition - destination;

                // Checking if destination is in line of sight of mirroredPosition
                if (Physics.Raycast(destination, LOSDir, out RaycastHit LOSHit, Vector3.Distance(destination, mirroredPosition), mirrorLayerMask) && LOSHit.transform == mirror)
                {
                    float totalDistance = LOSHit.distance;

                    if (ricochetPredictions > 1)
                    {
                        RaycastHit lastReflectHit = LOSHit;
                        Vector3 lastDestination = destination;
                        for (int i = 0; i < ricochetPredictions - 1; i++)
                        {
                            // Reflecting
                            Vector3 reflectedDir = Vector3.Reflect(lastReflectHit.point - lastDestination, lastReflectHit.normal);
                            if (Physics.Raycast(lastReflectHit.point, reflectedDir, out RaycastHit reflectHit, Mathf.Infinity, ~nonObstructLayerMask))
                            {
                                totalDistance += reflectHit.distance;

                                if (showRays)
                                {
                                    Debug.DrawLine(lastReflectHit.point, lastDestination, Color.green, halfDrawDuration);
                                    Debug.DrawLine(lastReflectHit.point, reflectHit.point, Color.green, halfDrawDuration);
                                }
                                if (i < ricochetPredictions - 2)
                                {
                                    lastDestination = lastReflectHit.point;
                                    lastReflectHit = reflectHit;
                                }
                                else
                                {
                                    float finalReflectDst = Vector3.Distance(origin.position, reflectHit.point) - 0.1f;
                                    if (!Physics.Raycast(origin.position, reflectHit.point - origin.position, out RaycastHit finalReflectHit, finalReflectDst, ~nonObstructLayerMask))
                                    {
                                        totalDistance += finalReflectDst;
                                        TestAngles(origin, reflectHit, lastReflectHit.point, totalDistance);
                                    }
                                    else
                                    {
                                        lookPositions.Add(reflectHit.point);

                                        if (showRays)
                                        {
                                            Debug.DrawLine(origin.position, finalReflectHit.point, Color.red, halfDrawDuration);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        float reflectDst = Vector3.Distance(origin.position, LOSHit.point) - 0.1f;
                        // Checking bullet path
                        if (Physics.Raycast(origin.position, LOSHit.point - origin.position, out RaycastHit bulletObstruct, reflectDst, ~nonObstructLayerMask))
                        {
                            if (showRays)
                            {
                                Debug.DrawLine(origin.position, LOSHit.point, Color.cyan, halfDrawDuration);
                                Debug.DrawLine(origin.position, bulletObstruct.point, Color.red, halfDrawDuration);
                            }
                        }
                        else
                        {
                            totalDistance += reflectDst;
                            TestAngles(origin, LOSHit, destination, totalDistance);
                        }
                    }
                }
            }
        }
    }

    public Vector3 SelectShootPosition(Transform origin, SelectionMode mode)
    {
        Vector3 shootPosition = origin.position;
        if (shootPositions.Count > 0)
        {
            List<Vector3> shootPositionsKeys = shootPositions.Keys.ToList();
            shootIndex++;
            if (shootIndex > shootPositionsKeys.Count - 1)
            {
                shootIndex = 0;
            }

            switch (mode)
            {
                case SelectionMode.Random:
                    shootPosition = shootPositionsKeys[Random.Range(0, shootPositions.Keys.Count)];
                    break;
                case SelectionMode.Closest:
                    shootPosition = origin.ClosestAnglePosition(shootPositionsKeys);
                    break;
                case SelectionMode.Farthest:
                    shootPosition = origin.FarthestAnglePosition(shootPositionsKeys);
                    break;
                case SelectionMode.Shortest:
                    shootPosition = shootPositions.OrderBy((x) => x.Value).ToList().FirstOrDefault().Key;
                    break;
                case SelectionMode.Longest:
                    shootPosition = shootPositions.OrderByDescending((x) => x.Value).ToList().FirstOrDefault().Key;
                    break;
                case SelectionMode.AlternateClose:
                    shootPositionsKeys.Remove(previousShootPosition);
                    shootPosition = origin.ClosestAnglePosition(shootPositionsKeys);
                    previousShootPosition = shootPosition;
                    break;
                case SelectionMode.AlternateFar:
                    shootPositionsKeys.Remove(previousShootPosition);
                    shootPosition = origin.FarthestAnglePosition(shootPositionsKeys);
                    previousShootPosition = shootPosition;
                    break;
                case SelectionMode.Next:
                    shootPosition = shootPositionsKeys[shootIndex];
                    break;
                case SelectionMode.NextClosest:
                    List<Vector3> closestShootPositions = shootPositionsKeys.OrderBy((x) => Vector3.Angle(x - origin.position, origin.forward)).ToList();
                    shootPosition = closestShootPositions[shootIndex];
                    break;
                case SelectionMode.NextFarthest:
                    List<Vector3> farthestShootPositions = shootPositionsKeys.OrderByDescending((x) => Vector3.Angle(x - origin.position, origin.forward)).ToList();
                    shootPosition = farthestShootPositions[shootIndex];
                    break;
                case SelectionMode.NextShortest:
                    List<Vector3> shortestShootPositions = shootPositions.OrderBy((x) => x.Value).ToList().Keys();
                    shootPosition = shortestShootPositions[shootIndex];
                    break;
                case SelectionMode.NextLongest:
                    List<Vector3> longestShootPositions = shootPositions.OrderByDescending((x) => x.Value).ToList().Keys();
                    shootPosition = longestShootPositions[shootIndex];
                    break;
            }
        }
        return shootPosition;
    }
}
