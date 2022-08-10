using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Math;
using System.Linq;

public class BlackBot : MonoBehaviour
{
    TargetSystem targetSystem;

    BaseTankLogic baseTankLogic;

    Transform body;
    Transform turret;
    Transform barrel;

    public float[] fireDelay = { 0.3f, 0.5f };

    public float[] layDelay = { 0.15f, 0.4f };
    [SerializeField] float layDistance = 8;

    public float[] modeResetDelay = { 3.5f, 5f };

    float angleToTarget;
    [SerializeField] float maxShootAngle = 2.5f;

    [Tooltip("Threshold to start rotating to target")][SerializeField] float maxTargetAngle = 110;
    [Tooltip("Threshold to start rotating away from target")][SerializeField] float minTargetAngle = 35;

    FireControl fireControl;
    MineControl mineControl;
    bool shooting = false;
    bool layingMine = false;

    Vector3 targetDir;
    Vector3 predictedTargetPosition;

    Transform nearbyMine = null;
    Transform nearbyBullet = null;

    bool resettingMode = false;
    enum Mode
    {
        Offense,
        Defense
    }
    Mode mode = Mode.Offense;

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();

        baseTankLogic = GetComponent<BaseTankLogic>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        fireControl = GetComponent<FireControl>();
        mineControl = GetComponent<MineControl>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null)
        {
            predictedTargetPosition = targetSystem.PredictedTargetPosition(CustomMath.TravelTime(turret.position, targetSystem.currentTarget.position, fireControl.speed));
            targetDir = predictedTargetPosition - turret.position;
            angleToTarget = Mathf.Abs(Vector3.SignedAngle(transform.forward, targetDir, transform.up));
            baseTankLogic.targetTurretDir = targetDir;

            switch (mode)
            {
                case Mode.Offense:
                    // Rotating towards target when target is getting behind this tank
                    if (angleToTarget > maxTargetAngle)
                    {
                        baseTankLogic.targetTankDir = targetDir;
                    }
                    else
                    {
                        baseTankLogic.targetTankDir = transform.forward;
                    }
                    break;
                case Mode.Defense:
                    // Rotating away from target when target is in front of tank
                    if (angleToTarget < minTargetAngle)
                    {
                        baseTankLogic.targetTankDir = -targetDir;
                    }
                    else
                    {
                        baseTankLogic.targetTankDir = transform.forward;
                    }
                    if (!resettingMode)
                    {
                        StartCoroutine(ResetMode());
                    }
                    break;
            }

            if (!shooting && Vector3.Angle(barrel.forward, targetDir) < maxShootAngle && fireControl.canFire && fireControl.bulletsFired < fireControl.bulletLimit && targetSystem.TargetVisible())
            {
                StartCoroutine(Shoot());
            }
            else if (mode == Mode.Defense)
            {
                if (mineControl.canLay && !layingMine && Mathf.Abs(Vector3.SignedAngle(turret.forward, targetDir, turret.up)) > 15)
                {
                    Transform closestTank = transform.ClosestTransform(transform.parent);
                    float closestTankSqrDst = closestTank == null ? Mathf.Infinity : CustomMath.SqrDistance(closestTank.position, transform.position);
                    if (closestTankSqrDst > layDistance * layDistance)
                    {
                        StartCoroutine(LayMine());
                    }
                }
            }

            if (nearbyMine != null)
            {
                Vector3 oppositeDir = transform.position - nearbyMine.position;
                if (!Physics.Raycast(body.position, oppositeDir, 2.5f, ~targetSystem.ignoreLayerMask))
                {
                    baseTankLogic.targetTankDir = oppositeDir;
                }
            }

            if (nearbyBullet != null)
            {
                Vector3 otherForward = nearbyBullet.forward;
                otherForward.y = 0;
                Vector3 clockwise = Quaternion.AngleAxis(90, turret.up) * otherForward;
                Vector3 counterClockwise = Quaternion.AngleAxis(-90, turret.up) * otherForward;
                Vector3 newTargetDir;

                if (Mathf.Abs(Vector3.SignedAngle(transform.position - nearbyBullet.position, clockwise, transform.up)) <= Mathf.Abs(Vector3.SignedAngle(transform.position - nearbyBullet.position, counterClockwise, transform.up)))
                {
                    newTargetDir = clockwise;
                }
                else
                {
                    newTargetDir = counterClockwise;
                }
                if (!Physics.Raycast(body.position, newTargetDir, 2.5f, ~targetSystem.ignoreLayerMask))
                {
                    Debug.DrawLine(transform.position, transform.position + newTargetDir, Color.cyan, 0.1f);
                    baseTankLogic.targetTankDir = newTargetDir;
                }
            }
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
                    // Move perpendicular to bullets
                    if (other.TryGetComponent<BulletBehaviour>(out var bulletBehaviour))
                    {
                        if (bulletBehaviour.owner != null && bulletBehaviour.owner != transform)
                        {
                            nearbyBullet = other.transform;
                            mode = Mode.Defense;
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

    IEnumerator ResetMode()
    {
        resettingMode = true;
        yield return new WaitForSeconds(Random.Range(modeResetDelay[0], modeResetDelay[1]));
        mode = Mode.Offense;
        resettingMode = false;
    }

    IEnumerator Shoot()
    {
        shooting = true;
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        baseTankLogic.stationary = true;
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(fireControl.Shoot());
        baseTankLogic.stationary = false;
        shooting = false;
    }

    IEnumerator LayMine()
    {
        layingMine = true;
        baseTankLogic.stationary = true;
        yield return new WaitForSeconds(Random.Range(layDelay[0], layDelay[1]));
        StartCoroutine(mineControl.LayMine());
        transform.position += transform.forward * 0.1f;
        layingMine = false;
        baseTankLogic.stationary = false;
    }
}
