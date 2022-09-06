using System.Collections;
using UnityEngine;
using MyUnityAddons.Calculations;

public class AquamarineBot : MonoBehaviour
{
    BaseTankLogic baseTankLogic;
    FireControl fireControl;
    RicochetCalculation bulletRicochet;

    Transform turret;
    Transform barrel;

    [SerializeField] float[] fireDelay = { 0.2f, 0.45f };
    [SerializeField] float[] indexChangeDelay = { 0.5f, 1.5f };

    [SerializeField] float maxShootAngle = 0.5f;
    [SerializeField] float updateDelay = 0.1f;
    [SerializeField] float predictionScale = 0.68f;

    bool shooting = false;

    TargetSystem targetSystem;

    Vector3 shootPosition;
    Vector3 lookDirection;
    int lookIndex = 0;

    Transform nearbyMine = null;
    Transform nearbyBullet = null;

    Coroutine fireRoutine = null;

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();

        baseTankLogic = GetComponent<BaseTankLogic>();
        fireControl = GetComponent<FireControl>();
        bulletRicochet = GetComponent<RicochetCalculation>();

        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        InvokeRepeating(nameof(Loop), 0.1f, updateDelay);
        StartCoroutine(SwitchLookIndex());
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null)
        {
            baseTankLogic.targetTankDir = transform.forward;

            if (bulletRicochet.lookPositions.Count > 0)
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

            if (nearbyBullet == null && nearbyMine == null)
            {
                if (bulletRicochet.shootPositions.Count > 0)
                {
                    baseTankLogic.targetTurretDir = shootPosition - turret.position;

                    if (!shooting && fireControl.canFire && fireControl.BulletSpawnClear())
                    {
                        fireRoutine = StartCoroutine(Shoot());
                    }
                }
                else if (targetSystem.TargetVisible())
                {
                    baseTankLogic.targetTurretDir = targetSystem.currentTarget.position - turret.position;

                    if (!shooting && fireControl.canFire && Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle)
                    {
                        fireRoutine = StartCoroutine(Shoot());
                    }
                }
            }

            if (targetSystem.TargetVisible())
            {
                baseTankLogic.targetTurretDir = targetSystem.currentTarget.position - turret.position;

                if (!shooting && fireControl.canFire && Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle)
                {
                    fireRoutine = StartCoroutine(Shoot());
                }
            }
            else
            {
                if (bulletRicochet.shootPositions.Count > 0 && nearbyMine == null && nearbyBullet == null)
                {
                    baseTankLogic.targetTurretDir = shootPosition - turret.position;

                    if (!shooting && fireControl.canFire && fireControl.BulletSpawnClear())
                    {
                        fireRoutine = StartCoroutine(Shoot());
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


            if (nearbyMine != null)
            {
                baseTankLogic.AvoidMine(nearbyMine, 100);
                StopFiring();
            }

            if (nearbyBullet != null)
            {
                baseTankLogic.AvoidBullet(nearbyBullet);
                StopFiring();
            }
        }
    }

    void Loop()
    {
        if (!GameManager.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null)
        {
            bulletRicochet.ScanArea(turret.position);

            Vector3 predictedPos = targetSystem.PredictedTargetPosition(CustomMath.TravelTime(turret.position, targetSystem.currentTarget.position, fireControl.speed * predictionScale));
            bulletRicochet.CalculateBulletRicochets(barrel, predictedPos);

            if (bulletRicochet.shootPositions.Count > 0)
            {
                shootPosition = bulletRicochet.SelectShootPosition(barrel, RicochetCalculation.SelectionMode.Closest);
                predictedPos = targetSystem.PredictedTargetPosition(bulletRicochet.shootPositions[shootPosition] / fireControl.speed);
                bulletRicochet.CalculateBulletRicochets(barrel, predictedPos);
                if (bulletRicochet.shootPositions.Count > 0)
                {
                    shootPosition = bulletRicochet.SelectShootPosition(barrel, RicochetCalculation.SelectionMode.Closest);
                }
            }
        }
    }

    void StopFiring()
    {
        if(fireRoutine != null)
        {
            StopCoroutine(fireRoutine);
            shooting = false;
            baseTankLogic.stationary = false;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!GameManager.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null)
        {
            // Avoiding bullets and mines
            switch (other.tag)
            {
                case "Bullet":
                    nearbyBullet = other.transform;
                    break;
                case "Mine":
                    nearbyMine = other.transform;
                    break;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        switch (other.tag)
        {
            case "Bullet":
                if (nearbyBullet == other.transform)
                {
                    nearbyBullet = null;
                }
                break;
            case "Mine":
                if (nearbyMine == other.transform)
                {
                    nearbyMine = null;
                }
                break;
        }
    }

    IEnumerator Shoot()
    {
        shooting = true;
        baseTankLogic.stationary = true;
        yield return new WaitUntil(() => Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle);
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(fireControl.Shoot());
        baseTankLogic.stationary = false;
        bulletRicochet.SelectShootPosition(barrel, bulletRicochet.selectionMode);

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
