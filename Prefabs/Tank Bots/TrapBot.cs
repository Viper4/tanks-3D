using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Calculations;
using System.Linq;

public class TrapBot : MonoBehaviour
{
    TargetSystem targetSystem;

    BaseTankLogic baseTankLogic;
    RicochetCalculation bulletRicochet;

    Transform body;
    Transform turret;
    Transform barrel;

    public float[] fireDelay = { 0.28f, 0.425f };
    [SerializeField] float trapWidth = 2;

    public float[] layDelay = { 0.3f, 0.6f };
    [SerializeField] float layDistance = 10;

    public float[] modeResetDelay = { 3.5f, 5f };

    float angleToTarget;
    [SerializeField] float maxShootAngle = 5;
    [SerializeField] float maxRicochetAngle = 90;

    [Tooltip("Threshold to start rotating to target")] [SerializeField] float maxTargetAngle = 100;
    [Tooltip("Threshold to start rotating away from target")] [SerializeField] float minTargetAngle = 20;

    FireControl fireControl;
    MineControl mineControl;
    bool layingMine = false;

    Vector3 targetDir;
    public Transform partner = null;

    Transform nearbyMine = null;
    Transform nearbyBullet = null;

    Coroutine fireRoutine = null;
    TrapBot[] trapBots;

    enum FirePattern
    {
        None,
        Trap
    }
    FirePattern firePattern = FirePattern.None;

    public enum Mode
    {
        Offense,
        Defense,
        LayingMine,
        Pincer
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
        mineControl = GetComponent<MineControl>();
        trapBots = FindObjectsOfType<TrapBot>();

        if(trapBots.Length > 1)
        {
            partner = transform.ClosestTransform(trapBots.Select((x) => x.transform).ToList());
            if(partner != null)
            {
                TrapBot partnerScript = partner.GetComponent<TrapBot>();
                if(partnerScript.partner == null)
                {
                    partnerScript.partner = transform;
                    partnerScript.mode = Mode.Pincer;
                    mode = Mode.Pincer;
                }
            }
        }
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
                case Mode.Pincer:
                    // Rotating towards target when target is getting behind this tank
                    if(angleToTarget > maxTargetAngle)
                    {
                        baseTankLogic.targetTankDir = targetDir;
                    }
                    else
                    {
                        baseTankLogic.targetTankDir = transform.forward;
                    }

                    if(partner == null)
                    {
                        trapBots = trapBots.Where(x => x != null).ToArray();
                        partner = transform.ClosestTransform(trapBots.Select(x => x.transform).ToList());
                        if(partner != null)
                        {
                            TrapBot partnerScript = partner.GetComponent<TrapBot>();
                            if(partnerScript.partner == null)
                            {
                                partnerScript.partner = transform;
                                partnerScript.mode = Mode.Pincer;
                                mode = Mode.Pincer;
                            }
                            else
                            {
                                partner = null;
                                mode = Mode.Offense;
                                break;
                            }
                        }
                        else
                        {
                            mode = Mode.Offense;
                            break;
                        }
                    }
                    else if(partner.GetComponent<TrapBot>().partner != transform)
                    {
                        partner = null;
                        mode = Mode.Offense;
                        break;
                    }

                    // If going towards partner then turn around
                    Vector3 partnerDir = partner.position - transform.position;
                    if(Vector3.Angle(transform.forward, partnerDir) < 30)
                    {
                        partnerDir.y = 0;
                        baseTankLogic.targetTankDir = -partnerDir;
                    }
                    previousMode = mode;
                    break;
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

            float turretAngleToTarget = Mathf.Abs(Vector3.SignedAngle(turret.forward, targetDir, transform.up));
            if(targetSystem.TargetVisible())
            {
                if(turretAngleToTarget < 15 && fireControl.canFire && fireControl.firedBullets.Count < fireControl.bulletLimit && firePattern == FirePattern.None)
                {
                    fireRoutine = StartCoroutine(TrapFirePattern());
                }
            }
            else
            {
                if(firePattern != FirePattern.None)
                {
                    StopCoroutine(fireRoutine);
                    fireRoutine = null;
                    firePattern = FirePattern.None;
                    mode = previousMode;
                    baseTankLogic.stationary = false;
                }
            }

            if(firePattern == FirePattern.None)
            {
                baseTankLogic.targetTurretDir = targetDir;
                if(mode == Mode.Defense && mineControl.canLay && !layingMine &&(turretAngleToTarget > maxRicochetAngle || !targetSystem.TargetVisible() || fireControl.bulletLimit >= fireControl.firedBullets.Count))
                {
                    Transform closestTank = transform.ClosestTransform(transform.parent);
                    float closestTankDst = closestTank == null ? Mathf.Infinity : Vector3.Distance(closestTank.position, transform.position);
                    if(closestTankDst > layDistance)
                    {
                        StartCoroutine(LayMine());
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

    void OnTriggerStay(Collider other)
    {
        if(!GameManager.Instance.frozen && Time.timeScale != 0 && baseTankLogic != null && !baseTankLogic.disabled)
        {
            // Avoiding bullets and mines
            switch(other.tag)
            {
                case "Bullet":
                    // Move perpendicular to bullets
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

    IEnumerator TrapFirePattern()
    {
        firePattern = FirePattern.Trap;
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));

        float angle = Random.Range(0, 2) == 0 ? 90 : -90;
        Vector3 leftTarget = targetSystem.currentTarget.position +(Quaternion.AngleAxis(angle, targetSystem.currentTarget.up) * targetDir).normalized * trapWidth;
        Vector3 rightTarget = targetSystem.currentTarget.position +(Quaternion.AngleAxis(-angle, targetSystem.currentTarget.up) * targetDir).normalized * trapWidth;

        Debug.DrawLine(targetSystem.currentTarget.position, leftTarget, Color.blue, 5f);
        Debug.DrawLine(targetSystem.currentTarget.position, rightTarget, Color.blue, 5f);

        // Firing to left of target
        if(!Physics.Linecast(turret.position, leftTarget, ~targetSystem.ignoreLayerMask))
        {
            baseTankLogic.targetTurretDir = leftTarget - turret.position;
            yield return new WaitUntil(() => Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle);
            baseTankLogic.stationary = true;
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
            StartCoroutine(fireControl.Shoot());
            baseTankLogic.stationary = false;
        }

        // Firing to right of target
        if(!Physics.Linecast(turret.position, rightTarget, ~targetSystem.ignoreLayerMask))
        {
            baseTankLogic.targetTurretDir = rightTarget - turret.position;
            yield return new WaitUntil(() => fireControl.canFire && Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle);
            baseTankLogic.stationary = true;
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
            StartCoroutine(fireControl.Shoot());
            baseTankLogic.stationary = false;
        }

        bulletRicochet.ScanArea(turret.position);
        bulletRicochet.CalculateBulletRicochets(barrel, targetSystem.currentTarget.position);
        Vector3 shootPosition = bulletRicochet.SelectShootPosition(barrel, bulletRicochet.selectionMode);
        baseTankLogic.targetTurretDir = shootPosition - turret.position;
        if(bulletRicochet.shootPositions.Count > 0 &&(Mathf.Abs(Vector3.SignedAngle(turret.forward, shootPosition - turret.position, turret.up)) < maxRicochetAngle || !turret.CanRotateTo(targetDir, baseTankLogic.turretRangeX, baseTankLogic.turretRangeY)))
        {
            // Firing ricochet bullet to target
            baseTankLogic.stationary = true;
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
            yield return new WaitUntil(() => fireControl.canFire && Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle);
            StartCoroutine(fireControl.Shoot());
            baseTankLogic.stationary = false;
        }
        else
        {
            // Firing bullet straight to target
            baseTankLogic.targetTurretDir = targetDir;
            baseTankLogic.stationary = true;
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
            yield return new WaitUntil(() => fireControl.canFire && Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle);
            StartCoroutine(fireControl.Shoot());
            baseTankLogic.stationary = false;
        }
        if(mode != Mode.Defense)
        {
            StartCoroutine(ResetMode());
            mode = Mode.Defense;
        }

        fireRoutine = null;
        firePattern = FirePattern.None;
    }

    IEnumerator LayMine()
    {
        mode = Mode.LayingMine;

        layingMine = true;
        baseTankLogic.stationary = true;
        yield return new WaitForSeconds(Random.Range(layDelay[0], layDelay[1]));
        StartCoroutine(mineControl.LayMine());
        transform.position += transform.forward * 0.1f;
        layingMine = false;
        baseTankLogic.stationary = false;

        List<Vector3> movePositions = new List<Vector3>();
        if(!Physics.Raycast(transform.position, -transform.right, mineControl.explosionRadius, ~targetSystem.ignoreLayerMask))
        {
            Vector3 newPosition = transform.position - transform.right * mineControl.explosionRadius;
            if(Physics.Linecast(targetSystem.currentTarget.position, newPosition, ~targetSystem.ignoreLayerMask))
            {
                movePositions.Add(newPosition);
            }
        }
        if(!Physics.Raycast(transform.position, transform.forward, mineControl.explosionRadius, ~targetSystem.ignoreLayerMask))
        {
            Vector3 newPosition = transform.position + transform.forward * mineControl.explosionRadius;
            if(Physics.Linecast(targetSystem.currentTarget.position, newPosition, ~targetSystem.ignoreLayerMask))
            {
                movePositions.Add(newPosition);
            }
        }
        if(!Physics.Raycast(transform.position, transform.right, mineControl.explosionRadius, ~targetSystem.ignoreLayerMask))
        {
            Vector3 newPosition = transform.position + transform.right * mineControl.explosionRadius;
            if(Physics.Linecast(targetSystem.currentTarget.position, newPosition, ~targetSystem.ignoreLayerMask))
            {
                movePositions.Add(newPosition);
            }
        }
        if(!Physics.Raycast(transform.position, -transform.forward, mineControl.explosionRadius, ~targetSystem.ignoreLayerMask))
        {
            Vector3 newPosition = transform.position - transform.forward * mineControl.explosionRadius;
            if(Physics.Linecast(targetSystem.currentTarget.position, newPosition, ~targetSystem.ignoreLayerMask))
            {
                movePositions.Add(newPosition);
            }
        }

        if(movePositions.Count > 0)
        {
            Vector3 newLayPosition = movePositions[Random.Range(0, movePositions.Count)];
            baseTankLogic.targetTankDir = newLayPosition - transform.position;
            yield return new WaitUntil(() => Vector3.Distance(transform.position, newLayPosition) < 1);
            if(mineControl.mineLimit - mineControl.laidMines.Count > 0 && mineControl.canLay)
            {
                layingMine = true;
                baseTankLogic.stationary = true;
                yield return new WaitForSeconds(Random.Range(layDelay[0], layDelay[1]));
                StartCoroutine(mineControl.LayMine());
                layingMine = false;
                baseTankLogic.stationary = false;
            }
        }

        mode = previousMode;
    }
}
