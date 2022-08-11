using MyUnityAddons.Math;
using System.Collections;
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
    [SerializeField] int horizontalSteps = 60;
    [SerializeField] int verticalSteps = 10;
    [SerializeField] float maxHorizontalAngle = 180;
    [SerializeField] float maxVerticalAngle = 20;
    [SerializeField] bool doubleScanHorizontal = true;
    [SerializeField] bool doubleScanVertical = true;

    Dictionary<Transform, List<List<Vector3>>> mirrorPositionPairs = new Dictionary<Transform, List<List<Vector3>>>();
    List<RaycastHit> mirrorHits = new List<RaycastHit>();

    [HideInInspector] public Dictionary<Vector3, float> shootPositions = new Dictionary<Vector3, float>();
    [HideInInspector] public Vector3 shootPosition;
    [HideInInspector] public List<Vector3> lookPositions = new List<Vector3>();

    Vector3 Mirror(Vector3 inDirection, RaycastHit mirrorHit, float mirrorDistance = 0)
    {
        if (mirrorDistance == 0)
        {
            mirrorDistance = mirrorHit.distance;
        }

        Vector3 reflectedVector = Vector3.Reflect(-inDirection, mirrorHit.normal);
        return mirrorHit.point + (reflectedVector * mirrorDistance);
    }

    void HorizontalScan(Vector3 origin, Vector3 verticalVector, float angleOffset)
    {
        for (int j = 0; j < horizontalSteps; j++)
        {
            Vector3 testDirection = Quaternion.AngleAxis(angleOffset * j, transform.up) * verticalVector;
            if (Physics.Raycast(origin, testDirection, out RaycastHit mirrorHit, Mathf.Infinity, mirrorLayerMask))
            {
                // Populating first ricochet mirror positions
                if (!mirrorPositionPairs.ContainsKey(mirrorHit.transform))
                {
                    Vector3 mirroredPosition = Mirror(testDirection, mirrorHit);
                    mirrorPositionPairs.Add(mirrorHit.transform, new List<List<Vector3>>());
                    for (int k = 0; k < ricochetPredictions; k++)
                    {
                        mirrorPositionPairs[mirrorHit.transform].Add(new List<Vector3>() { mirroredPosition });
                    }
                    mirrorHits.Add(mirrorHit);

                    if (showRays)
                    {
                        Debug.DrawLine(origin, mirrorHit.point, Color.magenta, drawDuration);
                        Debug.DrawLine(mirrorHit.point, mirroredPosition, Color.yellow, drawDuration);
                    }
                }
                else if (showRays)
                {
                    Debug.DrawLine(origin, mirrorHit.point, Color.red, drawDuration);
                }
            }
            else if (showRays)
            {
                Debug.DrawRay(origin, testDirection, Color.red, drawDuration);
            }
        }
    }

    public void ScanArea(Vector3 origin)
    {
        mirrorPositionPairs.Clear();
        mirrorHits.Clear();

        float angleOffsetH = maxHorizontalAngle / horizontalSteps;
        float angleOffsetV = maxVerticalAngle / verticalSteps;

        // Finding valid mirrors by casting rays in every angleOffsetY horizontal, every angleOffsetX degree vertical
        if (doubleScanVertical)
        {
            for (int i = 0; i < verticalSteps; i++)
            {
                Vector3 verticalVectorPos = Quaternion.AngleAxis(angleOffsetV * i, transform.right) * transform.forward;
                Vector3 verticalVectorNeg = Quaternion.AngleAxis(-angleOffsetV * i, transform.right) * transform.forward;
                HorizontalScan(origin, verticalVectorPos, angleOffsetH);
                HorizontalScan(origin, verticalVectorNeg, angleOffsetH);
                if (doubleScanHorizontal)
                {
                    HorizontalScan(origin, verticalVectorPos, -angleOffsetH);
                    HorizontalScan(origin, verticalVectorNeg, -angleOffsetH);
                }
            }
        }
        else
        {
            for (int i = 0; i < verticalSteps; i++)
            {
                Vector3 verticalVector = Quaternion.AngleAxis(angleOffsetV * i, transform.right) * transform.forward;
                HorizontalScan(origin, verticalVector, angleOffsetH);
                if (doubleScanHorizontal)
                {
                    HorizontalScan(origin, verticalVector, -angleOffsetH);
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
                                Debug.DrawLine(mirroredPosition, mirrorHit.point, Color.blue, drawDuration);
                            }
                        }
                    }
                }
            }
        }
    }

    public void CalculateBulletRicochets(Transform origin, Vector3 destination)
    {
        shootPositions.Clear();
        lookPositions.Clear();
        float halfdrawDuration = drawDuration * 0.5f;
        foreach (Transform mirror in mirrorPositionPairs.Keys)
        {
            foreach (Vector3 mirroredPosition in mirrorPositionPairs[mirror][ricochetPredictions - 1])
            {
                Vector3 LOSDir = mirroredPosition - destination;

                // Checking if destination is in line of sight of mirroredPosition
                if (Physics.Raycast(destination, LOSDir, out RaycastHit LOSHit, Vector3.Distance(destination, mirroredPosition), mirrorLayerMask))
                {
                    float path1Dst = Vector3.Distance(destination, LOSHit.point) - 0.1f;
                    // Checking bullet path
                    if (!Physics.Raycast(destination, LOSDir, out RaycastHit bulletObstruct1, path1Dst, ~nonObstructLayerMask))
                    {
                        float totalDistance = path1Dst;
                        // Checking if the visible LOS point corresponds to this mirror
                        if (LOSHit.transform == mirror)
                        {
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
                                        float reflectDst = Vector3.Distance(lastReflectHit.point, reflectHit.point) - 0.1f;
                                        totalDistance += reflectDst;
                                        // Checking bullet path of reflection
                                        if (!Physics.Raycast(reflectHit.point, lastReflectHit.point - reflectHit.point, out RaycastHit bulletObstruct, reflectDst - 0.1f, ~nonObstructLayerMask))
                                        {
                                            if (showRays)
                                            {
                                                Debug.DrawLine(lastReflectHit.point, lastDestination, Color.green, halfdrawDuration);
                                                Debug.DrawLine(lastReflectHit.point, reflectHit.point, Color.green, halfdrawDuration);
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
                                                    // Comparing inciting angle with exiting angle
                                                    float incitingAngle = Vector3.Angle(reflectHit.point - origin.position, reflectHit.normal);
                                                    float exitingAngle = Vector3.Angle(reflectHit.point - lastReflectHit.point, reflectHit.normal);
                                                    if (Mathf.Abs(incitingAngle - exitingAngle) < 0.1f)
                                                    {
                                                        shootPositions.AddOrReplace(reflectHit.point, totalDistance);

                                                        if (showRays)
                                                        {
                                                            Debug.DrawLine(origin.position, reflectHit.point, Color.green, halfdrawDuration);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        lookPositions.Add(reflectHit.point);

                                                        if (showRays)
                                                        {
                                                            Debug.DrawLine(origin.position, reflectHit.point, Color.cyan, halfdrawDuration);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    lookPositions.Add(reflectHit.point);

                                                    if (showRays)
                                                    {
                                                        Debug.DrawLine(origin.position, finalReflectHit.point, Color.red, halfdrawDuration);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (showRays)
                                            {
                                                Debug.DrawLine(lastReflectHit.point, lastDestination, Color.cyan, halfdrawDuration);
                                                Debug.DrawLine(lastReflectHit.point, bulletObstruct.point, Color.red, halfdrawDuration);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                float reflectDst = Vector3.Distance(origin.position, LOSHit.point) - 0.25f;
                                // Checking bullet path
                                if (!Physics.Raycast(origin.position, LOSHit.point - origin.position, out RaycastHit bulletObstruct, reflectDst, ~nonObstructLayerMask))
                                {
                                    totalDistance += reflectDst;
                                    // Comparing inciting angle with exiting angle
                                    float incitingAngle = Vector3.Angle(LOSHit.point - origin.position, LOSHit.normal);
                                    float exitingAngle = Vector3.Angle(LOSHit.point - destination, LOSHit.normal);
                                    if (Mathf.Abs(incitingAngle - exitingAngle) < 0.1f)
                                    {
                                        shootPositions.AddOrReplace(LOSHit.point, totalDistance);

                                        if (showRays)
                                        {
                                            Debug.DrawLine(origin.position, LOSHit.point, Color.green, halfdrawDuration);
                                        }
                                    }
                                    else
                                    {
                                        lookPositions.Add(LOSHit.point);

                                        if (showRays)
                                        {
                                            Debug.DrawLine(origin.position, LOSHit.point, Color.cyan, halfdrawDuration);
                                        }
                                    }

                                    if (showRays)
                                    {
                                        Debug.DrawLine(origin.position, LOSHit.point, Color.green, halfdrawDuration);
                                        Debug.DrawLine(LOSHit.point, LOSHit.point, Color.green, halfdrawDuration);
                                        Debug.DrawLine(LOSHit.point, destination, Color.green, halfdrawDuration);
                                    }
                                }
                                else
                                {
                                    lookPositions.Add(LOSHit.point);
                                    if (showRays)
                                    {
                                        Debug.DrawLine(origin.position, LOSHit.point, Color.cyan, halfdrawDuration);
                                        Debug.DrawLine(origin.position, bulletObstruct.point, Color.red, halfdrawDuration);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        lookPositions.Add(LOSHit.point);

                        if (showRays)
                        {
                            Debug.DrawLine(destination, LOSHit.point, Color.cyan, halfdrawDuration);
                            Debug.DrawLine(destination, bulletObstruct1.point, Color.red, halfdrawDuration);
                        }
                    }
                }
                else if (showRays)
                {
                    Debug.DrawLine(destination, mirroredPosition, Color.white, halfdrawDuration);
                }
            }
        }

        if (!shootPositions.ContainsKey(shootPosition))
        {
            shootPosition = origin.ClosestAnglePosition(shootPositions.Keys.ToList());
        }
    }
}
