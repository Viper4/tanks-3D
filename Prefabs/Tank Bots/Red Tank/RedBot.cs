using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedBot : MonoBehaviour
{
    TargetSelector targetSelector;
    Quaternion rotToTarget;

    BaseTankLogic baseTankLogic;

    Transform body;
    Transform turret;
    Transform barrel;
    Quaternion turretAnchor;
    Vector3 lastEulerAngles;

    public float[] reactionTime = { 0.28f, 0.425f };
    public float[] fireDelay = { 0.2f, 0.45f };

    Rigidbody rb;

    [SerializeField] bool randomizeSeed = true;

    [SerializeField] Vector2 inaccuracy = new Vector2(3, 30);
    [SerializeField] float turretRotSpeed = 40f;
    [SerializeField] float turretNoiseSpeed = 0.2f;
    [SerializeField] float turretRotSeed = 0;
    [SerializeField] float maxShootAngle = 5;

    [SerializeField] float[] turretRangeX = { -20, 20 };

    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float avoidSpeed = 3f;
    float speed = 5;

    [SerializeField] float gravity = 8;
    float velocityY = 0;

    FireControl fireControl;
    bool shooting = false;

    enum Mode
    {
        Normal,
        Shoot,
        Avoid,
        Defend
    }
    Mode mode = Mode.Normal;

    // Start is called before the first frame Update
    void Awake()
    {
        targetSelector = GetComponent<TargetSelector>();

        baseTankLogic = GetComponent<BaseTankLogic>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        turretAnchor = turret.rotation;

        rb = GetComponent<Rigidbody>();

        lastEulerAngles = body.eulerAngles;

        if (randomizeSeed)
        {
            turretRotSeed = Random.Range(-99.0f, 99.0f);
        }

        fireControl = GetComponent<FireControl>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!SceneLoader.frozen && Time.timeScale != 0)
        {
            if (rb != null)
            {
                Vector3 velocity;
                velocityY = baseTankLogic.IsGrounded() ? 0 : velocityY - Time.deltaTime * gravity;

                Vector3 targetDirection = transform.forward;
                if (Physics.Raycast(transform.position, -transform.up, out RaycastHit middleHit, 1) && Physics.Raycast(transform.position + transform.forward, -transform.up, out RaycastHit frontHit, 1))
                {
                    targetDirection = frontHit.point - middleHit.point;
                }

                // Checking Forward on the center, left, and right side
                RaycastHit forwardHit;
                if (Physics.Raycast(body.position, transform.forward, out forwardHit, 2, baseTankLogic.barrierLayers) || Physics.Raycast(body.position + transform.right, transform.forward, out forwardHit, 2, baseTankLogic.barrierLayers) || Physics.Raycast(body.position - transform.right, transform.forward, out forwardHit, 2, baseTankLogic.barrierLayers))
                {
                    mode = Mode.Avoid;
                    baseTankLogic.ObstacleAvoidance(forwardHit, 2, baseTankLogic.barrierLayers);
                }
                else if (mode == Mode.Avoid)
                {
                    mode = Mode.Normal;
                }

                switch (mode)
                {
                    case Mode.Normal:
                        // Resetting currentTarget to primary and trying to move to target
                        targetSelector.currentTarget = targetSelector.primaryTarget;
                        baseTankLogic.RotateToVector(targetSelector.currentTarget.position - turret.position);

                        speed = moveSpeed;

                        baseTankLogic.noisyRotation = true;
                        break;
                    case Mode.Defend:
                        // OnTriggerStay handles rotation
                        speed = moveSpeed;

                        baseTankLogic.noisyRotation = true;
                        break;
                    case Mode.Avoid:
                        speed = avoidSpeed;

                        baseTankLogic.noisyRotation = false;
                        break;
                    case Mode.Shoot:
                        speed = 0;

                        baseTankLogic.noisyRotation = false;
                        break;
                }

                velocity = targetDirection * speed + Vector3.up * velocityY;

                rb.velocity = velocity;

                // Inaccuracy to rotation with noise
                float noiseX = inaccuracy.x * (Mathf.PerlinNoise(turretRotSeed + Time.time * turretNoiseSpeed, turretRotSeed + 1f + Time.time * turretNoiseSpeed) - 0.5f);
                float noiseY = inaccuracy.y * (Mathf.PerlinNoise(turretRotSeed + 4f + Time.time * turretNoiseSpeed, turretRotSeed + 5f + Time.time * turretNoiseSpeed) - 0.5f);

                // Correcting turret and barrel y rotation to not depend on the parent
                turret.eulerAngles = new Vector3(turret.eulerAngles.x, turret.eulerAngles.y + lastEulerAngles.y - transform.eulerAngles.y, turret.eulerAngles.z);
                barrel.eulerAngles = new Vector3(barrel.eulerAngles.x, barrel.eulerAngles.y + lastEulerAngles.y - transform.eulerAngles.y, barrel.eulerAngles.z);

                // Rotating turret and barrel towards target
                Vector3 targetDir = targetSelector.predictedTargetPos - turret.position;
                rotToTarget = Quaternion.LookRotation(targetDir);
                turret.rotation = barrel.rotation = turretAnchor = Quaternion.RotateTowards(turretAnchor, rotToTarget, Time.deltaTime * turretRotSpeed);

                // Zeroing x and z eulers of turret and clamping barrel x euler
                turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y + noiseY, 0);
                barrel.localEulerAngles = new Vector3(Clamping.ClampAngle(barrel.localEulerAngles.x + noiseX, turretRangeX[0], turretRangeX[1]), barrel.localEulerAngles.y + noiseY, 0);
            }

            if (fireControl.canFire && mode != Mode.Shoot && !shooting && Physics.Raycast(barrel.position, targetSelector.predictedTargetPos - barrel.position, out RaycastHit barrelHit, Mathf.Infinity, ~baseTankLogic.transparentLayers, QueryTriggerInteraction.Ignore))
            {
                // Ray hits the capsule collider which is on Tank Origin for player and the 2nd topmost transform for tank bots
                if (barrelHit.transform.root.name == "Player" && targetSelector.currentTarget.root.name == "Player")
                {
                    StartCoroutine(Shoot());
                }
                else if (barrelHit.transform == targetSelector.currentTarget.parent) // target for tank bots is the turret
                {
                    StartCoroutine(Shoot());
                }
            }

            lastEulerAngles = transform.eulerAngles;
        }
        else
        {
            rb.velocity = Vector3.zero;
        }
    }

    void OnTriggerStay(Collider other)
    {
        mode = Mode.Normal;
        Vector3 desiredDir;
        // Avoiding bullets and mines
        switch (other.tag)
        {
            case "Bullet":
                // Move perpendicular to bullets and shoot at them
                if (other.GetComponent<BulletBehaviour>().owner != transform)
                {
                    mode = Mode.Defend;
                    desiredDir = Random.Range(0, 2) == 0 ? Quaternion.AngleAxis(90, Vector3.up) * other.transform.forward : Quaternion.AngleAxis(-90, Vector3.up) * other.transform.forward;

                    // Applying rotation
                    baseTankLogic.RotateToVector(desiredDir);

                    // Firing at bullets
                    targetSelector.currentTarget = other.transform;
                }
                break;
            case "Mine":
                if (SceneLoader.autoPlay)
                {
                    // Move in opposite direction of mine
                    desiredDir = transform.position - other.transform.position;

                    // Applying rotation
                    baseTankLogic.RotateToVector(desiredDir);
                }
                else if (other.GetComponent<MineBehaviour>().owner != targetSelector.primaryTarget.root)
                {
                    // Move in opposite direction of mine
                    desiredDir = transform.position - other.transform.position;

                    // Applying rotation
                    baseTankLogic.RotateToVector(desiredDir);
                }
                break;
        }
    }

    IEnumerator Shoot()
    {
        // When angle between barrel and target is less than shootAngle, then stop and fire
        float angle = Quaternion.Angle(barrel.rotation, rotToTarget);
        if(angle < maxShootAngle)
        {
            shooting = true;

            // Keeps moving until reaction time from seeing player is reached
            yield return new WaitForSeconds(Random.Range(reactionTime[0], reactionTime[1]));

            // Stops moving and delay in firing
            mode = Mode.Shoot;
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
            StartCoroutine(GetComponent<FireControl>().Shoot());

            mode = Mode.Normal;
            shooting = false;
        }
    }
}
