using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreyBot : MonoBehaviour
{
    public Transform target;
    float dstToTarget;
    Quaternion rotToTarget;

    [SerializeField] LayerMask ignoreLayerMask;

    BaseTankLogic baseTankLogic;

    Transform body;
    Transform turret;
    Transform barrel;

    [SerializeField] float maxShootAngle = 30;

    public float[] reactionTime = { 0.7f, 1.25f };

    public float[] fireDelay = { 0.3f, 0.6f };
    float cooldown = 0;

    Rigidbody rb;

    [SerializeField] Vector2 inaccuracy = new Vector2(10, 25);

    [SerializeField] bool randomizeSeed = true;

    [SerializeField] float turretRotSpeed = 25f;
    [SerializeField] float turretNoiseSpeed = 0.15f;
    [SerializeField] float turretRotSeed = 0;
    Quaternion turretAnchor;

    [SerializeField] float[] turretRangeX = { -20, 20 };

    Vector3 lastEulerAngles;
    
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float avoidSpeed = 2f;

    float speed = 4;
    [SerializeField] float gravity = 8;
    float velocityY = 0;

    float triggerRadius = 3f;

    Coroutine shootRoutine;

    enum Mode
    {
        Move,
        Shoot,
        Avoid
    }
    Mode mode = Mode.Move;

    // Start is called before the first frame Update
    void Awake()
    {
        if (target == null)
        {
            Debug.Log("The variable target of GreyBot has been defaulted to the player");
            target = GameObject.Find("Player").transform;
        }

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

        triggerRadius = GetComponent<SphereCollider>().radius;
    }

    // Update is called once per frame
    void Update()
    {
        if (!SceneLoader.frozen && Time.timeScale != 0)
        {
            cooldown = cooldown > 0 ? cooldown - Time.deltaTime : 0;
            dstToTarget = Vector3.Distance(body.position, target.position);

            if (mode != Mode.Shoot && cooldown == 0 && !Physics.Raycast(turret.position, target.position - turret.position, dstToTarget, ~ignoreLayerMask, QueryTriggerInteraction.Ignore))
            {
                StartCoroutine(Shoot());
            }
            else if (shootRoutine != null)
            {
                StopCoroutine(shootRoutine);
                shootRoutine = null;
                mode = Mode.Move;
            }

            if (rb != null)
            {
                Vector3 targetDirection = transform.forward;
                Vector3 velocity;
                velocityY = baseTankLogic.IsGrounded() ? 0 : velocityY - Time.deltaTime * gravity;

                // Checking Forward on the center, left, and right side
                RaycastHit forwardHit;
                if (Physics.Raycast(body.position, transform.forward, out forwardHit, 2) || Physics.Raycast(body.position + transform.right, transform.forward, out forwardHit, 2) || Physics.Raycast(body.position - transform.right, transform.forward, out forwardHit, 2))
                {
                    mode = Mode.Avoid;
                    baseTankLogic.ObstacleAvoidance(forwardHit, triggerRadius);
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
            Vector3 targetDir = target.position - turret.position;
            rotToTarget = Quaternion.LookRotation(targetDir);
            turret.rotation = barrel.rotation = turretAnchor = Quaternion.RotateTowards(turretAnchor, rotToTarget, Time.deltaTime * turretRotSpeed);

            // Zeroing x and z eulers of turret and clamping barrel x euler
            turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y + noiseY, 0);
            barrel.localEulerAngles = new Vector3(Clamping.ClampAngle(barrel.localEulerAngles.x + noiseX, turretRangeX[0], turretRangeX[1]), barrel.localEulerAngles.y + noiseY, 0);

            lastEulerAngles = transform.eulerAngles;
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
            cooldown = GetComponent<FireControl>().fireCooldown;
            StartCoroutine(GetComponent<FireControl>().Shoot());

            shootRoutine = null;
            mode = Mode.Move;
        }
        else
        {
            shootRoutine = null;
            yield return null;
        }
    }
}
