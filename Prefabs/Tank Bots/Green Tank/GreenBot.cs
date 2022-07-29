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

    Dictionary<Transform, Vector3> mirrorPositionPair = new Dictionary<Transform, Vector3>();
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

        CalculateBulletRicochet(turret.position, targetSystem.currentTarget.position, ricochetPredictions);

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
        mirrorPositionPair.Clear();
        List<RaycastHit> mirrorHits = new List<RaycastHit>();
        lastPosition = transform.position;

        float angleOffset = 360 / testRays;
        // Finding valid mirrors by casting testRays number of rays in 360 degrees
        for (int j = 0; j < testRays; j++)
        {
            Vector3 testDirection = Quaternion.AngleAxis(angleOffset * j, Vector3.up) * Vector3.forward;
            if (Physics.Raycast(origin, testDirection, out RaycastHit mirrorHit, Mathf.Infinity, mirrorLayerMask))
            {
                if (!mirrorPositionPair.ContainsKey(mirrorHit.transform))
                {
                    mirrorHits.Add(mirrorHit);
                    Vector3 mirroredPosition = Mirror(testDirection, mirrorHit);
                    mirrorPositionPair.Add(mirrorHit.transform, mirroredPosition);
                    
                    if (showRays)
                    {
                        Debug.DrawLine(turret.position, mirrorHit.point, Color.magenta, Mathf.Infinity);
                        Debug.DrawLine(mirrorHit.point, mirroredPosition, Color.yellow, Mathf.Infinity);
                    }
                }
                else if (showRays)
                {
                    Debug.DrawLine(turret.position, mirrorHit.point, Color.red, Mathf.Infinity);
                }
            }
            else if (showRays)
            {
                Debug.DrawRay(turret.position, testDirection, Color.red, Mathf.Infinity);
            }
        }
    }
    
    void UpdateShootLookPositions(Vector3 origin, Vector3 destination)
    {
        tempShootPositions.Clear();
        tempLookPositions.Clear();
        foreach (Transform mirror in mirrorPositionPair.Keys)
        {
            Vector3 LOSDir = mirrorPositionPair[mirror] - destination;

            // Checking if destination is in line of sight of mirroredPosition
            if (Physics.Raycast(destination, LOSDir, out RaycastHit LOSHit, Vector3.Distance(destination, mirrorPositionPair[mirror]), mirrorLayerMask))
            {
                // Checking bullet path
                if (!Physics.Raycast(destination, LOSDir, out RaycastHit bulletObstruct1, Vector3.Distance(destination, LOSHit.point) - 0.1f, obstructLayerMask))
                {
                    // Checking if the visible LOS point corresponds to this mirror
                    if (LOSHit.transform == mirror)
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
                            Debug.Log(bulletObstruct.transform.name);
                            tempLookPositions.Add(LOSHit.point);

                            if (showRays)
                            {
                                Debug.DrawLine(origin, LOSHit.point, Color.cyan, updateDelay);
                                Debug.DrawLine(origin, bulletObstruct.point, Color.red, updateDelay);
                            }
                        }
                    }

                }
                else
                {
                    Debug.Log(bulletObstruct1.transform.name);

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
                Debug.DrawLine(destination, mirrorPositionPair[mirror], Color.white, updateDelay);
            }
        }
    }

    void CalculateBulletRicochet(Vector3 origin, Vector3 destination, int ricochets = 1)
    {
        UpdateShootLookPositions(origin, destination);

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
