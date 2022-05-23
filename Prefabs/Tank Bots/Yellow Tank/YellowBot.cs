using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YellowBot : MonoBehaviour
{
    TargetSelector targetSelector;
    float dstToTarget;
    Quaternion rotToTarget;

    [SerializeField] LayerMask transparentLayerMask;
    [SerializeField] LayerMask barrierLayerMask;

    BaseTankLogic baseTankLogic;

    Transform body;
    Transform turret;
    Transform barrel;
    Quaternion turretAnchor;
    Vector3 lastEulerAngles;

    [SerializeField] float maxShootAngle = 30;
    public float[] reactionTime = { 0.3f, 0.45f };
    public float[] fireDelay = { 0.3f, 0.6f };

    public float[] layDelay = { 0.3f, 0.6f };

    Rigidbody rb;

    [SerializeField] bool randomizeSeed = true;

    [SerializeField] Vector2 inaccuracy = new Vector2(8, 35);
    [SerializeField] float turretRotSpeed = 35f;
    [SerializeField] float turretNoiseSpeed = 0.15f;
    [SerializeField] float turretRotSeed = 0;

    [SerializeField] float[] turretRangeX = { -20, 20 };

    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float avoidSpeed = 4f;
    float speed = 4;

    [SerializeField] float gravity = 8;
    float velocityY = 0;

    float triggerRadius = 3f;

    FireControl fireControl;
    bool layingMine;
    MineControl mineControl;

    enum Mode
    {
        Move,
        Shoot,
        Avoid,
        Lay
    }
    Mode mode = Mode.Move;

    // Start is called before the first frame Update
    void Awake()
    {
        targetSelector = GetComponent<TargetSelector>();

        baseTankLogic = GetComponent<BaseTankLogic>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");
        turretAnchor = turret.rotation;
        lastEulerAngles = body.eulerAngles;

        rb = GetComponent<Rigidbody>();

        if (randomizeSeed)
        {
            turretRotSeed = Random.Range(-99.0f, 99.0f);
        }

        triggerRadius = GetComponent<SphereCollider>().radius;

        fireControl = GetComponent<FireControl>();
        mineControl = GetComponent<MineControl>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!SceneLoader.frozen && Time.timeScale != 0)
        {
            dstToTarget = Vector3.Distance(body.position, targetSelector.target.position);

            if (fireControl.canFire && mode != Mode.Shoot && Physics.Raycast(barrel.position, targetSelector.target.position - barrel.position, out RaycastHit barrelHit, dstToTarget, ~transparentLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (barrelHit.transform.root.name == targetSelector.target.root.name)
                {
                    StartCoroutine(Shoot());
                }
            }

            if (mineControl.canLay && !layingMine && mode != Mode.Lay && mode != Mode.Avoid)
            {
                StartCoroutine(LayMine());
            }

            if (rb != null)
            {
                Vector3 targetDirection = transform.forward;
                Vector3 velocity;
                velocityY = baseTankLogic.IsGrounded() ? 0 : velocityY - Time.deltaTime * gravity;

                // Checking Forward on the center, left, and right side
                RaycastHit forwardHit;
                if (Physics.Raycast(body.position, transform.forward, out forwardHit, 2, barrierLayerMask) || Physics.Raycast(body.position + transform.right, transform.forward, out forwardHit, 2, barrierLayerMask) || Physics.Raycast(body.position - transform.right, transform.forward, out forwardHit, 2, barrierLayerMask))
                {
                    mode = Mode.Avoid;
                    baseTankLogic.ObstacleAvoidance(forwardHit, triggerRadius, barrierLayerMask);
                }
                else if (mode == Mode.Avoid)
                {
                    mode = Mode.Move;
                }

                switch (mode)
                {
                    case Mode.Move:
                        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit middleHit, 1) && Physics.Raycast(transform.position + transform.forward, -transform.up, out RaycastHit frontHit, 1))
                        {
                            targetDirection = frontHit.point - middleHit.point;
                        }

                        speed = moveSpeed;

                        baseTankLogic.noisyRotation = true;
                        break;
                    case Mode.Avoid:
                        speed = avoidSpeed;

                        baseTankLogic.noisyRotation = false;
                        break;
                    default:
                        speed = 0;

                        baseTankLogic.noisyRotation = false;
                        break;
                }

                velocity = targetDirection * speed + Vector3.up * velocityY;

                rb.velocity = velocity;
            }

            // Inaccuracy to rotation with noise
            float noiseX = inaccuracy.x * (Mathf.PerlinNoise(turretRotSeed + Time.time * turretNoiseSpeed, turretRotSeed + 1f + Time.time * turretNoiseSpeed) - 0.5f);
            float noiseY = inaccuracy.y * (Mathf.PerlinNoise(turretRotSeed + 4f + Time.time * turretNoiseSpeed, turretRotSeed + 5f + Time.time * turretNoiseSpeed) - 0.5f);

            // Correcting turret and barrel y rotation to not depend on the parent
            turret.eulerAngles = new Vector3(turret.eulerAngles.x, turret.eulerAngles.y + lastEulerAngles.y - transform.eulerAngles.y, turret.eulerAngles.z);
            barrel.eulerAngles = new Vector3(barrel.eulerAngles.x, barrel.eulerAngles.y + lastEulerAngles.y - transform.eulerAngles.y, barrel.eulerAngles.z);

            // Rotating turret and barrel towards player
            Vector3 targetDir = targetSelector.target.position - turret.position;
            rotToTarget = Quaternion.LookRotation(targetDir);
            turret.rotation = barrel.rotation = turretAnchor = Quaternion.RotateTowards(turretAnchor, rotToTarget, Time.deltaTime * turretRotSpeed);

            // Zeroing x and z eulers of turret and clamping barrel x euler
            turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y + noiseY, 0);
            barrel.localEulerAngles = new Vector3(Clamping.ClampAngle(barrel.localEulerAngles.x + noiseX, turretRangeX[0], turretRangeX[1]), barrel.localEulerAngles.y + noiseY, 0);

            lastEulerAngles = transform.eulerAngles;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        Vector3 desiredDir;
        switch (other.tag)
        {
            case "Mine":
                // Move in opposite direction of mine
                desiredDir = transform.position - other.transform.position;

                // Applying rotation
                baseTankLogic.RotateToVector(desiredDir);
                break;
        }
    }

    IEnumerator Shoot()
    {
        // When angle between barrel and target is less than maxShootAngle, then stop and fire
        float angle = Quaternion.Angle(barrel.rotation, rotToTarget);
        if (angle < maxShootAngle)
        {
            // Keeps moving until reaction time from seeing player is reached
            yield return new WaitForSeconds(Random.Range(reactionTime[0], reactionTime[1]));
            // Stops moving and delay in firing
            mode = Mode.Shoot;
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
            StartCoroutine(GetComponent<FireControl>().Shoot());

            mode = Mode.Move;
        }
        else
        {
            yield return null;
        }
    }

    IEnumerator LayMine()
    {
        layingMine = true;
        yield return new WaitForSeconds(Random.Range(layDelay[0], layDelay[1]));
        mode = Mode.Lay;
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(GetComponent<MineControl>().LayMine());
        Vector3 desiredDir = Quaternion.AngleAxis(Random.Range(-180.0f, 180.0f), Vector3.up) * transform.forward;
        rb.rotation = Quaternion.LookRotation(desiredDir);
        
        transform.position += transform.forward * 0.1f;
        mode = Mode.Move;
        layingMine = false;
    }
}
