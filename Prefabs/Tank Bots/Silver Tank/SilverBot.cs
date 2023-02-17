using MyUnityAddons.Calculations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SilverBot : MonoBehaviour
{
    TargetSystem targetSystem;
    BaseTankLogic baseTankLogic;
    AreaScanner areaScanner;

    Transform body;
    Transform turret;
    Transform barrel;

    [SerializeField] float[] fireDelay = { 0.3f, 0.45f };
    
    FireControl fireControl;
    bool shooting = false;

    Transform nearbyMine = null;
    Transform nearbyBullet = null;

    [SerializeField] float maxShootAngle = 5;

    Vector3 predictedTargetPosition;
    float angleToTarget;
    Vector3 targetDir;
    [SerializeField] float minTargetAngle = 35;

    [SerializeField] float explosionRadius = 6;
    float squareExplosionRadius;
    bool safeFromExplosion = false;
    [SerializeField] float loopTime = 0.1f;
    [SerializeField] LayerMask playerMissileLayers;
    [SerializeField] LayerMask passiveMissileLayers;
    [SerializeField] LayerMask friendlyLayers;
    Vector3 missilePosition;
    Coroutine missileRoutine;

    bool targetVisible;
    bool predictedPosVisible;

    List<Transform> visibleObjects = new List<Transform>();

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();
        baseTankLogic = GetComponent<BaseTankLogic>();
        areaScanner = GetComponent<AreaScanner>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        fireControl = GetComponent<FireControl>();

        squareExplosionRadius = explosionRadius * explosionRadius;
        InvokeRepeating(nameof(Loop), 0, loopTime);
    }

    // Update is called once per frame
    void Update()
    {
        if(!GameManager.Instance.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null && !baseTankLogic.disabled)
        {
            targetDir = targetSystem.currentTarget.position - turret.position;
            angleToTarget = Mathf.Abs(Vector3.SignedAngle(transform.forward, targetDir, transform.up));
            if (CustomMath.SqrDistance(transform.position, targetSystem.currentTarget.position) < squareExplosionRadius * 2) // Get away from target
            {
                if(missileRoutine != null)
                {
                    StopCoroutine(missileRoutine);
                    missileRoutine = null;
                    baseTankLogic.stationary = false;
                    shooting = false;
                }

                safeFromExplosion = false;

                if (angleToTarget < minTargetAngle)
                {
                    baseTankLogic.targetTankDir = -targetDir;
                }
                else
                {
                    baseTankLogic.targetTankDir = transform.forward;
                }

                baseTankLogic.targetTurretDir = targetSystem.currentTarget.position - turret.position;
            }
            else
            {
                safeFromExplosion = true;
                baseTankLogic.targetTankDir = transform.forward;
                predictedTargetPosition = targetSystem.PredictedTargetPosition(CustomMath.TravelTime(turret.position, targetSystem.currentTarget.position, fireControl.bulletSettings.speed));
                predictedTargetPosition.y += 0.1f; // To account for missile bounds
                if (missileRoutine == null)
                {
                    predictedPosVisible = !Physics.Linecast(turret.position, predictedTargetPosition, ~targetSystem.ignoreLayerMask);
                    targetVisible = targetSystem.TargetVisible();
                    if (predictedPosVisible)
                    {
                        baseTankLogic.targetTurretDir = predictedTargetPosition - turret.position;
                    }
                    else
                    {
                        Vector3 targetPos = targetSystem.currentTarget.position;
                        targetPos.y += 0.1f; // To account for missile bounds
                        baseTankLogic.targetTurretDir = targetPos - turret.position;
                    }

                    if ((targetVisible || predictedPosVisible) && fireControl.canFire && !shooting && Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle)
                    {
                        StartCoroutine(Shoot());
                    }
                }
            }

            if(nearbyMine != null)
            {
                baseTankLogic.AvoidMine(nearbyMine, 100);
            }

            if(nearbyBullet != null)
            {
                baseTankLogic.AvoidBullet(nearbyBullet);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        switch(other.tag)
        {
            case "Mine":
                nearbyMine = other.transform;
                break;
            case "Bullet":
                if (other.TryGetComponent<BulletBehaviour>(out var bulletBehaviour))
                {
                    if (bulletBehaviour.owner != null && bulletBehaviour.owner != transform)
                    {
                        nearbyBullet = other.transform;
                    }
                }
                break;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        switch(other.tag)
        {
            case "Mine":
                if(nearbyMine == other.transform)
                {
                    nearbyMine = null;
                }
                break;
            case "Bullet":
                if (nearbyBullet == other.transform)
                {
                    nearbyBullet = null;
                }
                break;
        }
    }

    void Loop()
    {
        if(missileRoutine == null)
        {
            if(safeFromExplosion && !GameManager.Instance.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null && !baseTankLogic.disabled)
            {
                SearchForMissilePositions();
            }
        }
        else
        {
            if (CustomMath.SqrDistance(missilePosition, targetSystem.currentTarget.position) > squareExplosionRadius)
            {
                StopCoroutine(missileRoutine);
                missileRoutine = null;
                baseTankLogic.stationary = false;
                shooting = false;
            }
        }
    }

    void SearchForMissilePositions()
    {
        visibleObjects = areaScanner.GetVisibleObjectsNotNear(turret, playerMissileLayers, friendlyLayers, explosionRadius);
        List<Vector3> validPositions = new List<Vector3>();
        List<Vector3> passivePositions = new List<Vector3>();

        foreach (Transform visibleObject in visibleObjects)
        {
            Collider visibleCollider = visibleObject.GetComponent<Collider>();
            Vector3 position = visibleCollider.ClosestPoint(targetSystem.currentTarget.position);

            if(Physics.SphereCast(barrel.position, 0.15f, position - barrel.position, out RaycastHit hit, Mathf.Infinity, areaScanner.obstructLayerMask))
            {
                if(CustomMath.SqrDistance(hit.point, transform.position) > squareExplosionRadius * 2)
                {
                    if (passiveMissileLayers == (passiveMissileLayers | (1 << hit.transform.gameObject.layer)))
                    {
                        passivePositions.Add(position);
                    }
                    if (CustomMath.SqrDistance(hit.point, targetSystem.currentTarget.position) < squareExplosionRadius || CustomMath.SqrDistance(hit.point, predictedTargetPosition) < squareExplosionRadius)
                    {
                        validPositions.Add(position);
                    }
                }
            }
        }

        if(validPositions.Count > 0)
        {
            missilePosition = validPositions[Random.Range(0, validPositions.Count)];
            missileRoutine = StartCoroutine(ShootAtMissilePos());
        }
        else if(passivePositions.Count > 0)
        {
            missilePosition = passivePositions[Random.Range(0, passivePositions.Count)];
            missileRoutine = StartCoroutine(ShootAtMissilePos());
        }
    }

    IEnumerator ShootAtMissilePos()
    {
        shooting = true;
        baseTankLogic.stationary = true;
        baseTankLogic.targetTurretDir = missilePosition - barrel.position;
        yield return new WaitUntil(() => fireControl.canFire && Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle);
        StartCoroutine(fireControl.Shoot());
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        baseTankLogic.stationary = false;
        shooting = false;
        missileRoutine = null;
    }

    IEnumerator Shoot()
    {
        shooting = true;
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        baseTankLogic.stationary = true;
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        if(targetVisible || predictedPosVisible)
            StartCoroutine(fireControl.Shoot());
        baseTankLogic.stationary = false;
        shooting = false;
    }
}
