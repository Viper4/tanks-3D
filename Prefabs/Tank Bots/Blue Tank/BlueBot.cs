using System.Collections;
using UnityEngine;
using MyUnityAddons.Calculations;

public class BlueBot : MonoBehaviour
{
    TargetSystem targetSystem;

    BaseTankLogic baseTankLogic;
    RicochetCalculation bulletRicochet;

    Transform body;
    Transform turret;
    Transform barrel;

    public float[] fireDelay = { 0.28f, 0.425f };
    public float[] modeResetDelay = { 3.5f, 5f };
    public float[] fireResetDelay = { 2.5f, 4 };

    [SerializeField] float updateDelay = 0.5f;

    [SerializeField] float maxShootAngle = 5;
    [SerializeField] float maxRicochetAngle = 45;

    float angleToTarget;
    [Tooltip("Threshold to start rotating to target")][SerializeField] float maxTargetAngle = 100;
    [Tooltip("Threshold to start rotating away from target")][SerializeField] float minTargetAngle = 20;

    FireControl fireControl;

    Vector3 targetDir;

    Transform nearbyMine = null;
    Transform nearbyBullet = null;

    Coroutine firePatternRoutine = null;
    Coroutine shootRoutine = null;

    enum FirePattern
    {
        None,
        Reset,
        ThreeShot
    }
    FirePattern firePattern = FirePattern.None;

    public enum Mode
    {
        Offense,
        Defense,
    }
    public Mode mode = Mode.Offense;
    Mode previousMode = Mode.Offense;

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();

        baseTankLogic = GetComponent<BaseTankLogic>();
        bulletRicochet = GetComponent<RicochetCalculation>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        fireControl = GetComponent<FireControl>();

        StartCoroutine(TimedLoop());
    }

    // Update is called once per frame
    void Update()
    {
        if(!GameManager.Instance.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null && !baseTankLogic.disabled)
        {
            targetDir = targetSystem.currentTarget.position - turret.position;
            angleToTarget = Mathf.Abs(Vector3.SignedAngle(transform.forward, targetDir, transform.up));

            switch(mode)
            {
                case Mode.Offense:
                    // Rotating towards target when target is getting behind this tank
                    if(angleToTarget > maxTargetAngle)
                    {
                        baseTankLogic.targetTankDir = targetDir;
                    }
                    else
                    {
                        baseTankLogic.targetTankDir = transform.forward;
                    }

                    previousMode = mode;
                    break;
                case Mode.Defense:
                    // Rotating away from target when target is in front of tank
                    if(angleToTarget < minTargetAngle)
                    {
                        baseTankLogic.targetTankDir = -targetDir;
                    }
                    else
                    {
                        baseTankLogic.targetTankDir = transform.forward;
                    }
                    break;
            }

            if(bulletRicochet.shootPositions.Count > 0 || targetSystem.TargetVisible())
            {
                if(fireControl.canFire && fireControl.firedBullets.Count < fireControl.bulletLimit && firePattern == FirePattern.None)
                {
                    firePatternRoutine = StartCoroutine(ThreeShotPattern());
                }
            }
            else
            {
                if(firePattern == FirePattern.ThreeShot)
                {
                    StopCoroutine(firePatternRoutine);
                    firePatternRoutine = null;
                    if(shootRoutine != null)
                    {
                        StopCoroutine(shootRoutine);
                        shootRoutine = null;
                    }
                    firePattern = FirePattern.None;
                    mode = previousMode;
                    baseTankLogic.stationary = false;
                }
            }

            if(firePattern != FirePattern.ThreeShot)
            {
                baseTankLogic.targetTurretDir = targetDir;
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

    IEnumerator TimedLoop()
    {
        yield return new WaitUntil(() => !GameManager.Instance.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null);

        if(firePattern != FirePattern.ThreeShot)
        {
            bulletRicochet.ScanArea(turret.position);
            bulletRicochet.CalculateBulletRicochets(barrel, targetSystem.currentTarget.position);
        }

        yield return new WaitForSeconds(updateDelay);

        StartCoroutine(TimedLoop());
    }

    void OnTriggerStay(Collider other)
    {
        if(!GameManager.Instance.frozen && Time.timeScale != 0 && baseTankLogic != null && !baseTankLogic.disabled)
        {
            // Avoiding bullets and mines
            switch(other.tag)
            {
                case "Bullet":
                    if(other.TryGetComponent<BulletBehaviour>(out var bulletBehaviour))
                    {
                        if(bulletBehaviour.owner != null && bulletBehaviour.owner != transform)
                        {
                            nearbyBullet = other.transform;
                        }
                    }
                    break;
                case "Mine":
                    nearbyMine = other.transform;
                    break;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        switch(other.tag)
        {
            case "Bullet":
                if(nearbyBullet == other.transform)
                {
                    nearbyBullet = null;
                }
                break;
            case "Mine":
                if(nearbyMine == other.transform)
                {
                    nearbyMine = null;
                }
                break;
        }
    }

    IEnumerator ResetMode()
    {
        yield return new WaitForSeconds(Random.Range(modeResetDelay[0], modeResetDelay[1]));
        mode = previousMode;
    }

    IEnumerator ResetFirePattern()
    {
        firePattern = FirePattern.Reset;
        yield return new WaitForSeconds(Random.Range(fireResetDelay[0], fireResetDelay[1]));
        firePattern = FirePattern.None;
    }

    IEnumerator FireRicochetOrStraight()
    {
        bulletRicochet.ScanArea(turret.position);
        bulletRicochet.CalculateBulletRicochets(barrel, targetSystem.currentTarget.position);
        Vector3 shootPosition = bulletRicochet.SelectShootPosition(barrel, bulletRicochet.selectionMode);
        if(bulletRicochet.shootPositions.Count > 0 && Mathf.Abs(Vector3.SignedAngle(turret.forward, shootPosition - turret.position, turret.up)) < maxRicochetAngle && fireControl.BulletSpawnClear())
        {
            // Firing ricochet bullet to target
            baseTankLogic.targetTurretDir = shootPosition - turret.position;
            baseTankLogic.stationary = true;
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
            yield return new WaitUntil(() => fireControl.canFire && Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle);
            StartCoroutine(fireControl.Shoot());
            baseTankLogic.stationary = false;
        }
        else if(barrel.CanRotateTo(targetDir, baseTankLogic.turretRangeX, baseTankLogic.turretRangeY))
        {
            // Firing bullet straight to target
            baseTankLogic.targetTurretDir = targetDir;
            baseTankLogic.stationary = true;
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
            yield return new WaitUntil(() => fireControl.canFire && Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle);
            StartCoroutine(fireControl.Shoot());
            baseTankLogic.stationary = false;
        }
        shootRoutine = null;
    }

    IEnumerator ThreeShotPattern()
    {
        firePattern = FirePattern.ThreeShot;

        if(fireControl.firedBullets.Count < fireControl.bulletLimit)
        {
            shootRoutine = StartCoroutine(FireRicochetOrStraight());
            yield return new WaitUntil(() => shootRoutine == null);
        }

        if(fireControl.firedBullets.Count < fireControl.bulletLimit)
        {
            shootRoutine = StartCoroutine(FireRicochetOrStraight());
            yield return new WaitUntil(() => shootRoutine == null);
        }

        if(fireControl.firedBullets.Count < fireControl.bulletLimit)
        {
            shootRoutine = StartCoroutine(FireRicochetOrStraight());
            yield return new WaitUntil(() => shootRoutine == null);
        }

        if(mode != Mode.Defense)
        {
            StartCoroutine(ResetMode());
            mode = Mode.Defense;
        }
        firePatternRoutine = null;

        StartCoroutine(ResetFirePattern());
    }
}
