using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Math;
using System.Linq;

public class GreenBot : MonoBehaviour
{
    BaseTankLogic baseTankLogic;
    FireControl fireControl;
    RicochetCalculation bulletRicochet;

    Transform turret;
    Transform barrel;

    [SerializeField] float[] fireDelay = { 0.2f, 0.45f };
    [SerializeField] float[] indexChangeDelay = { 0.5f, 1.5f };

    [SerializeField] float maxShootAngle = 2;
    [SerializeField] float updateDelay = 1;
    [SerializeField] float predictionScale = 0.8f;

    bool shooting = false;

    TargetSystem targetSystem;

    Vector3 lookDirection;
    int lookIndex = 0;
    Vector3 lastPosition;

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();

        baseTankLogic = GetComponent<BaseTankLogic>();
        fireControl = GetComponent<FireControl>();
        bulletRicochet = GetComponent<RicochetCalculation>();

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
            if (lastPosition == null || (lastPosition - transform.position).sqrMagnitude > 0.099f)
            {
                lastPosition = transform.position;
                bulletRicochet.ScanArea(turret.position);
            }

            if (bulletRicochet.shootPositions.Count > 0)
            {
                baseTankLogic.targetTurretDir = bulletRicochet.shootPosition - turret.position;

                if (!shooting && fireControl.canFire && Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle)
                {
                    StartCoroutine(Shoot());
                }
            }
            else if (bulletRicochet.lookPositions.Count > 0)
            {
                if (lookIndex >= bulletRicochet.lookPositions.Count)
                {
                    lookIndex = 0;
                }
                lookDirection = bulletRicochet.lookPositions[lookIndex] - turret.position;
                baseTankLogic.targetTurretDir = lookDirection;
            }
            else
            {
                baseTankLogic.targetTurretDir = targetSystem.currentTarget.position - turret.position;
            }
        }
    }

    IEnumerator TimedLoop()
    {
        yield return new WaitUntil(() => !GameManager.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null);

        Vector3 predictedPos = targetSystem.PredictedTargetPosition(CustomMath.TravelTime(turret.position, targetSystem.currentTarget.position, fireControl.speed * predictionScale));
        bulletRicochet.CalculateBulletRicochets(barrel, predictedPos);

        if (bulletRicochet.shootPositions.ContainsKey(bulletRicochet.shootPosition))
        {
            predictedPos = targetSystem.PredictedTargetPosition(bulletRicochet.shootPositions[bulletRicochet.shootPosition] / fireControl.speed);
            bulletRicochet.CalculateBulletRicochets(barrel, predictedPos);
        }

        yield return new WaitForSeconds(updateDelay);

        StartCoroutine(TimedLoop());
    }

    IEnumerator Shoot()
    {
        shooting = true;

        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(fireControl.Shoot());
        List<Vector3> shootPositionsCut = bulletRicochet.shootPositions.Keys.ToList();

        shootPositionsCut.Remove(bulletRicochet.shootPosition);
        bulletRicochet.shootPosition = turret.ClosestAnglePosition(shootPositionsCut);

        shooting = false;
    }

    IEnumerator SwitchLookIndex()
    {
        yield return new WaitUntil(() => lookDirection != null && Vector3.Angle(barrel.forward, lookDirection) < maxShootAngle);
        lookIndex = Random.Range(0, bulletRicochet.lookPositions.Count);
        yield return new WaitForSeconds(Random.Range(indexChangeDelay[0], indexChangeDelay[1]));

        StartCoroutine(SwitchLookIndex());
    }
}
