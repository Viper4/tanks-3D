using System.Collections;
using UnityEngine;
using MyUnityAddons.Calculations;

public class BlackBot : MonoBehaviour
{
    TargetSystem targetSystem;

    BaseTankLogic baseTankLogic;

    Transform body;
    Transform turret;
    Transform barrel;

    public float[] fireDelay = { 0.3f, 0.5f };

    public float[] layDelay = { 0.15f, 0.4f };

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
        if(!GameManager.Instance.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null && !baseTankLogic.disabled)
        {
            targetDir = targetSystem.currentTarget.position - turret.position;
            angleToTarget = Mathf.Abs(Vector3.SignedAngle(transform.forward, targetDir, transform.up));

            predictedTargetPosition = targetSystem.PredictedTargetPosition(CustomMath.TravelTime(turret.position, targetSystem.currentTarget.position, fireControl.bulletSettings.speed));
            bool targetVisible = targetSystem.TargetVisible();
            bool predictedPosVisible = !Physics.Linecast(turret.position, predictedTargetPosition, ~targetSystem.ignoreLayerMask);
            if(predictedPosVisible)
            {
                baseTankLogic.targetTurretDir = predictedTargetPosition - turret.position;
            }
            else
            {
                baseTankLogic.targetTurretDir = targetDir;
            }

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
                    if(!resettingMode)
                    {
                        StartCoroutine(ResetMode());
                    }
                    break;
            }

            if((targetVisible || predictedPosVisible) && !shooting && Vector3.Angle(barrel.forward, targetDir) < maxShootAngle && fireControl.canFire && fireControl.firedBullets.Count < fireControl.bulletLimit)
            {
                StartCoroutine(Shoot());
            }
            else if(mode == Mode.Defense)
            {
                if(mineControl.canLay && !layingMine && Mathf.Abs(Vector3.SignedAngle(turret.forward, targetDir, turret.up)) > 15)
                {
                    StartCoroutine(LayMine());
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
