using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Math;

public class GreenBot : MonoBehaviour
{
    BaseTankLogic baseTankLogic;

    Transform turret;
    Transform barrel;
    Transform bulletSpawn;

    public float[] fireDelay = { 1, 3 };

    [SerializeField] float maxShootAngle = 2;

    bool shooting = false;

    TargetSelector targetSelector;

    [SerializeField] LayerMask mirrorLayerMask;
    [SerializeField] float updateDelay = 5;
    [SerializeField] int testRays = 60;
    [SerializeField] float rayRadius = 0.05f;
    List<Vector3> shootPositions = new List<Vector3>();
    int shootIndex = 0;
    List<Vector3> lookPositions = new List<Vector3>();
    int lookIndex = 0;

    // Start is called before the first frame Update
    void Start()
    {
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");
        bulletSpawn = barrel.Find("BulletSpawn");

        if (GetComponent<TargetSelector>() != null)
        {
            targetSelector = GetComponent<TargetSelector>();
        }

        baseTankLogic = GetComponent<BaseTankLogic>();

        StartCoroutine(GetMirrorPositions());
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.frozen && Time.timeScale != 0 && targetSelector.currentTarget != null)
        {
            if (shootPositions.Count > 0)
            {
                Vector3 shootDirection = shootPositions[shootIndex] - turret.position;
                baseTankLogic.RotateTurretTo(shootDirection);

                if (!shooting && Vector3.Angle(turret.forward, shootDirection) < maxShootAngle)
                {
                    StartCoroutine(Shoot());
                }
            }
            else if (lookPositions.Count > 0)
            {
                Vector3 lookDirection = lookPositions[lookIndex] - turret.position;
                baseTankLogic.RotateTurretTo(lookDirection);
            }
        }
    }

    IEnumerator GetMirrorPositions()
    {
        yield return new WaitUntil(() => !GameManager.frozen && Time.timeScale != 0 && targetSelector.currentTarget != null);
        lookPositions.Clear();
        Vector3 dirToTarget = targetSelector.currentTarget.position - turret.position;

        Debug.DrawRay(turret.position, dirToTarget, Color.blue, updateDelay);

        // Finding valid mirrors by casting testRays number of rays in 360 degrees
        for (int i = 0; i < testRays - 1; i++)
        {
            Vector3 testDirection = Quaternion.AngleAxis(360 / testRays * i, Vector3.up) * turret.forward;
            if (Physics.Raycast(turret.position, testDirection, out RaycastHit mirrorHit, Mathf.Infinity, mirrorLayerMask))
            {
                if (Vector3.Angle(mirrorHit.normal, -testDirection) < maxShootAngle * 2)
                {
                    float mirrorDistance = Vector3.Distance(turret.position, mirrorHit.point);
                    Vector3 mirroredPosition = mirrorHit.point + (testDirection * mirrorDistance);

                    Debug.DrawLine(turret.position, mirrorHit.point, Color.magenta, updateDelay);
                    Debug.DrawLine(mirrorHit.point, mirroredPosition, Color.yellow, updateDelay);

                    // Checking if target is in line of sight of mirroredPosition
                    if (Physics.SphereCast(targetSelector.currentTarget.position, rayRadius, mirroredPosition - targetSelector.currentTarget.position, out RaycastHit LOSHit, Vector3.Distance(targetSelector.currentTarget.position, mirroredPosition), mirrorLayerMask))
                    {
                        // Checking if shoot point is unobstructed from this tank
                        if (!Physics.SphereCast(bulletSpawn.position, rayRadius, LOSHit.point - bulletSpawn.position, out RaycastHit hit, Vector3.Distance(bulletSpawn.position, LOSHit.point) - 0.1f, ~targetSelector.ignoreLayerMask))
                        {
                            shootPositions.Add(LOSHit.point);
                            Debug.DrawLine(targetSelector.currentTarget.position, LOSHit.point, Color.green, updateDelay);
                            Debug.DrawRay(LOSHit.point, Vector3.Reflect(mirroredPosition - targetSelector.currentTarget.position, LOSHit.normal), Color.green, updateDelay);
                        }
                        else
                        {
                            lookPositions.Add(LOSHit.point);
                            Debug.DrawLine(targetSelector.currentTarget.position, LOSHit.point, Color.cyan, updateDelay);
                        }
                    }
                }
                else
                {
                    //Debug.DrawLine(turret.position, mirrorHit.point, Color.red, updateDelay);
                }
            }
        }

        lookIndex = Random.Range(0, lookPositions.Count);
        yield return new WaitForSeconds(updateDelay * 0.5f);

        lookIndex = Random.Range(0, lookPositions.Count);
        yield return new WaitForSeconds(updateDelay * 0.5f);
        StartCoroutine(GetMirrorPositions());
    }

    IEnumerator Shoot()
    {
        shooting = true;

        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(GetComponent<FireControl>().Shoot());
        shootIndex++;
        if (shootIndex >= shootPositions.Count)
        {
            shootPositions.Clear();
            shootIndex = 0;
        }

        shooting = false;
    }
}
