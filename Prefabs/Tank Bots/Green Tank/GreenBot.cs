using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Math;
using System.Linq;

public class GreenBot : MonoBehaviour
{
    BaseTankLogic baseTankLogic;

    Transform turret;
    Transform barrel;

    [SerializeField] float[] fireDelay = { 1, 3 };
    [SerializeField] float[] indexChangeDelay = { 0.5f, 1.5f };

    [SerializeField] float maxShootAngle = 2;

    bool shooting = false;

    TargetSystem targetSystem;

    [SerializeField] bool showRays;

    [SerializeField] LayerMask mirrorLayerMask;
    [SerializeField] LayerMask obstructLayerMask;
    [SerializeField] float updateDelay = 1;
    [SerializeField] int ricochetPredictions = 1;
    [SerializeField] int testRays = 60;
    [SerializeField] float rayRadius = 0.045f;

    Dictionary<Transform, List<List<Vector3>>> mirrorPositionPairs = new Dictionary<Transform, List<List<Vector3>>>();

    List<Vector3> shootPositions = new List<Vector3>();
    List<Vector3> tempShootPositions = new List<Vector3>();
    int shootIndex = 0;
    List<Vector3> lookPositions = new List<Vector3>();
    List<Vector3> tempLookPositions = new List<Vector3>();
    int lookIndex = 0;

    Vector3 lookDirection;
    Vector3 lastPosition;

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();

        baseTankLogic = GetComponent<BaseTankLogic>();

        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        StartCoroutine(TimedLoop());
        StartCoroutine(SwitchLookIndex());
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null)
        {
            if (lastPosition == null || lastPosition.Round() != transform.position.Round())
            {
                ScanArea(turret.position);
            }

            if (shootPositions.Count > 0)
            {
                if (shootIndex >= shootPositions.Count)
                {
                    shootIndex = 0;
                }
                Vector3 shootDirection = shootPositions[shootIndex] - turret.position;
                baseTankLogic.RotateTurretTo(shootDirection);

                if (!shooting && Vector3.Angle(turret.forward, shootDirection) < maxShootAngle)
                {
                    StartCoroutine(Shoot());
                }
            }
            else if (lookPositions.Count > 0)
            {
                if (lookIndex >= lookPositions.Count)
                {
                    lookIndex = 0;
                }
                lookDirection = lookPositions[lookIndex] - turret.position;
                baseTankLogic.RotateTurretTo(lookDirection);
            }
            else
            {
                baseTankLogic.RotateTurretTo(targetSystem.currentTarget.position - turret.position);
            }
        }
    }

    IEnumerator TimedLoop()
    {
        yield return new WaitUntil(() => !GameManager.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null);

        CalculateBulletRicochets(turret.position, targetSystem.currentTarget.position);

        yield return new WaitForSeconds(updateDelay);

        StartCoroutine(TimedLoop());
    }

    Vector3 Mirror(Vector3 inDirection, RaycastHit mirrorHit, float mirrorDistance = 0)
    {
        if (mirrorDistance == 0)
        {
            mirrorDistance = mirrorHit.distance;
        }

        Vector3 reflectedVector = Vector3.Reflect(-inDirection, mirrorHit.normal);
        return mirrorHit.point + (reflectedVector * mirrorDistance);
    }

    void ScanArea(Vector3 origin)
    {
        mirrorPositionPairs.Clear();
        lastPosition = transform.position;
        List<RaycastHit> mirrorHits = new List<RaycastHit>();

        float angleOffset = 360 / testRays;
        // Finding valid mirrors by casting testRays number of rays in 360 degrees
        for (int i = 0; i < testRays; i++)
        {
            Vector3 testDirection = Quaternion.AngleAxis(angleOffset * i, Vector3.up) * Vector3.forward;
            if (Physics.Raycast(origin, testDirection, out RaycastHit mirrorHit, Mathf.Infinity, mirrorLayerMask))
            {
                // Populating first ricochet mirror positions
                if (!mirrorPositionPairs.ContainsKey(mirrorHit.transform))
                {
                    Vector3 mirroredPosition = Mirror(testDirection, mirrorHit);
                    mirrorPositionPairs.Add(mirrorHit.transform, new List<List<Vector3>>());
                    for (int j = 0; j < ricochetPredictions; j++)
                    {
                        mirrorPositionPairs[mirrorHit.transform].Add(new List<Vector3>() { mirroredPosition });
                    }
                    mirrorHits.Add(mirrorHit);

                    if (showRays)
                    {
                        Debug.DrawLine(turret.position, mirrorHit.point, Color.magenta, 60);
                        Debug.DrawLine(mirrorHit.point, mirroredPosition, Color.yellow, 60);
                    }
                }
                else if (showRays)
                {
                    Debug.DrawLine(turret.position, mirrorHit.point, Color.red, 60);
                }
            }
            else if (showRays)
            {
                Debug.DrawRay(turret.position, testDirection, Color.red, 60);
            }
        }

        // Skip first ricochet mirror positions
        for (int i = 1; i < ricochetPredictions; i++)
        {
            foreach (Transform mirror in mirrorPositionPairs.Keys)
            {
                foreach (RaycastHit mirrorHit in mirrorHits)
                {
                    foreach(Vector3 lastMirrorPosition in mirrorPositionPairs[mirror][i - 1])
                    {
                        Vector3 mirroredPosition = Mirror(mirrorHit.point - lastMirrorPosition, mirrorHit, 1);
                        if (!mirrorPositionPairs[mirrorHit.transform][i].Contains(mirroredPosition))
                        {
                            mirrorPositionPairs[mirrorHit.transform][i].Add(mirroredPosition);
                            if (showRays)
                            {
                                Debug.DrawLine(mirroredPosition, mirrorHit.point, Color.blue, 60);
                            }
                        }
                    }
                }
            }
        }
    }

    void CalculateBulletRicochets(Vector3 origin, Vector3 destination)
    {
        tempShootPositions.Clear();
        tempLookPositions.Clear();
        float halfUpdateDelay = updateDelay * 0.5f;
        foreach (Transform mirror in mirrorPositionPairs.Keys)
        {
            foreach (Vector3 mirroredPosition in mirrorPositionPairs[mirror][ricochetPredictions - 1])
            {
                Vector3 LOSDir = mirroredPosition - destination;

                // Checking if destination is in line of sight of mirroredPosition
                if (Physics.Raycast(destination, LOSDir, out RaycastHit LOSHit, Vector3.Distance(destination, mirroredPosition), mirrorLayerMask))
                {
                    // Checking bullet path
                    if (!Physics.Raycast(destination, LOSDir, out RaycastHit bulletObstruct1, Vector3.Distance(destination, LOSHit.point) - 0.1f, obstructLayerMask))
                    {
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
                                    if (Physics.Raycast(lastReflectHit.point, reflectedDir, out RaycastHit reflectHit, Mathf.Infinity, obstructLayerMask))
                                    {
                                        // Checking bullet path
                                        if (!Physics.SphereCast(lastReflectHit.point, rayRadius, reflectedDir, out RaycastHit bulletObstruct, Vector3.Distance(lastReflectHit.point, reflectHit.point) - 0.1f, obstructLayerMask))
                                        {
                                            if (showRays)
                                            {
                                                Debug.DrawLine(lastReflectHit.point, lastDestination, Color.green, halfUpdateDelay);
                                                Debug.DrawLine(lastReflectHit.point, reflectHit.point, Color.green, halfUpdateDelay);
                                            }
                                            if (i < ricochetPredictions - 2)
                                            {
                                                lastDestination = lastReflectHit.point;
                                                lastReflectHit = reflectHit;
                                            }
                                            else
                                            {
                                                if (!Physics.SphereCast(origin, rayRadius, reflectHit.point - origin, out RaycastHit obstructHit, Vector3.Distance(origin, reflectHit.point) - 0.1f, obstructLayerMask))
                                                {
                                                    // Comparing inciting angle with exiting angle
                                                    float incitingAngle = Vector3.Angle(reflectHit.point - origin, reflectHit.normal);
                                                    float exitingAngle = Vector3.Angle(reflectHit.point - lastReflectHit.point, reflectHit.normal);
                                                    if (Mathf.Abs(incitingAngle - exitingAngle) < maxShootAngle)
                                                    {
                                                        tempShootPositions.Add(reflectHit.point);

                                                        if (showRays)
                                                        {
                                                            Debug.DrawLine(origin, reflectHit.point, Color.green, updateDelay);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        tempLookPositions.Add(reflectHit.point);

                                                        if (showRays)
                                                        {
                                                            Debug.DrawLine(origin, reflectHit.point, Color.cyan, updateDelay);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    tempLookPositions.Add(reflectHit.point);

                                                    if (showRays)
                                                    {
                                                        Debug.DrawLine(origin, obstructHit.point, Color.red, halfUpdateDelay);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (showRays)
                                            {
                                                Debug.DrawLine(lastReflectHit.point, lastDestination, Color.cyan, halfUpdateDelay);
                                                Debug.DrawLine(lastReflectHit.point, reflectHit.point, Color.cyan, halfUpdateDelay);
                                                Debug.DrawLine(lastReflectHit.point, bulletObstruct.point, Color.red, halfUpdateDelay);
                                            }
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                // Checking bullet path
                                if (!Physics.SphereCast(origin, rayRadius, LOSHit.point - origin, out RaycastHit bulletObstruct, Vector3.Distance(origin, LOSHit.point) - 0.1f, obstructLayerMask))
                                {
                                    tempShootPositions.Add(LOSHit.point);

                                    if (showRays)
                                    {
                                        Debug.DrawLine(origin, LOSHit.point, Color.green, updateDelay);
                                        Debug.DrawLine(LOSHit.point, LOSHit.point, Color.green, updateDelay);
                                        Debug.DrawLine(LOSHit.point, destination, Color.green, updateDelay);
                                    }
                                }
                                else
                                {
                                    tempLookPositions.Add(LOSHit.point);

                                    if (showRays)
                                    {
                                        Debug.DrawLine(origin, LOSHit.point, Color.cyan, updateDelay);
                                        Debug.DrawLine(origin, bulletObstruct.point, Color.red, updateDelay);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        tempLookPositions.Add(LOSHit.point);

                        if (showRays)
                        {
                            Debug.DrawLine(destination, LOSHit.point, Color.cyan, updateDelay);
                            Debug.DrawLine(destination, bulletObstruct1.point, Color.red, updateDelay);
                        }
                    }
                }
                else if (showRays)
                {
                    Debug.DrawLine(destination, mirroredPosition, Color.white, updateDelay);
                }
            }
        }

        shootPositions = tempShootPositions.ToList();
        lookPositions = tempLookPositions.ToList();
    }

    IEnumerator Shoot()
    {
        shooting = true;

        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(GetComponent<FireControl>().Shoot());
        shootIndex = Random.Range(0, shootPositions.Count);

        shooting = false;
    }

    IEnumerator SwitchLookIndex()
    {
        yield return new WaitUntil(() => lookDirection != null && Vector3.Angle(barrel.forward, lookDirection) < maxShootAngle);
        lookIndex = Random.Range(0, lookPositions.Count);
        yield return new WaitForSeconds(Random.Range(indexChangeDelay[0], indexChangeDelay[1]));

        StartCoroutine(SwitchLookIndex());
    }
}
