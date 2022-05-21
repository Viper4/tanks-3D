using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TealBot : MonoBehaviour
{
    public Transform target;
    float dstToTarget;
    Quaternion rotToTarget;

    [SerializeField] LayerMask targetLayerMasks;

    BaseTankLogic baseTankLogic;

    Transform body;
    Transform turret;
    Transform barrel;
    Quaternion turretAnchor;

    public float[] reactionTime = { 0.3f, 0.45f };

    public float[] fireDelay = { 0.3f, 0.6f };
    float cooldown = 0;

    Rigidbody rb;

    [SerializeField] float turretRotSpeed = 25f;
    [SerializeField] float[] turretRangeX = { -20, 20 };

    Vector3 lastEulerAngles;
    
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float avoidSpeed = 2f;

    float speed = 4;
    float gravity = 10;
    float velocityY = 0;

    float triggerRadius = 3.5f;

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
            Debug.Log("The variable target of TealBot has been defaulted to the player");
            target = GameObject.Find("Player").transform;
        }

        baseTankLogic = GetComponent<BaseTankLogic>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        rb = GetComponent<Rigidbody>();

        lastEulerAngles = transform.eulerAngles;

        triggerRadius = GetComponent<SphereCollider>().radius;
    }

    // Update is called once per frame
    void Update()
    {
        cooldown = cooldown > 0 ? cooldown - Time.deltaTime : 0;
        dstToTarget = Vector3.Distance(transform.position, target.position);

        if (mode != Mode.Shoot && cooldown == 0 && !Physics.Raycast(turret.position, target.position - turret.position, dstToTarget, ~targetLayerMasks, QueryTriggerInteraction.Ignore))
        {
            shootRoutine = StartCoroutine(Shoot());
        }
        else if (shootRoutine != null)
        {
            StopCoroutine(shootRoutine);
            shootRoutine = null;
            mode = Mode.Move;
        }
        
        if (rb != null)
        {
            // Movement
            Vector3 velocity;
            velocityY = baseTankLogic.IsGrounded() ? 0 : velocityY - Time.deltaTime * gravity;
            Vector3 targetDirection = transform.forward;
            
            ObstacleAvoidance();
            
            switch(mode)
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

        // Correcting turret and barrel y rotation to not depend on the parent
        turret.eulerAngles = new Vector3(turret.eulerAngles.x, turret.eulerAngles.y + lastEulerAngles.y - transform.eulerAngles.y, turret.eulerAngles.z);
        barrel.eulerAngles = new Vector3(barrel.eulerAngles.x, barrel.eulerAngles.y + lastEulerAngles.y - transform.eulerAngles.y, barrel.eulerAngles.z);

        // Rotating turret and barrel towards player
        Vector3 targetDir = target.position - turret.position;
        rotToTarget = Quaternion.LookRotation(targetDir);
        turret.rotation = barrel.rotation = turretAnchor = Quaternion.RotateTowards(turretAnchor, rotToTarget, Time.deltaTime * turretRotSpeed);

        // Zeroing x and z eulers of turret and clamping barrel x euler
        turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y, 0);
        barrel.localEulerAngles = new Vector3(Clamping.ClampAngle(barrel.localEulerAngles.x, turretRangeX[0], turretRangeX[1]), barrel.localEulerAngles.y, 0);
        lastEulerAngles = transform.eulerAngles;
    }

    void OnTriggerStay(Collider other)
    {
        if (mode != Mode.Shoot)
        {
            Vector3 desiredDir;
            // Avoiding bullets, mines, and obstacles
            switch (other.tag)
            {
                case "Bullet":
                    // If bullet is going to hit then dodge by moving perpendicular to bullet path
                    RaycastHit bulletHit;
                    if (Physics.Raycast(other.transform.position, other.transform.forward, out bulletHit))
                    {
                        if (bulletHit.transform == transform)
                        {
                            desiredDir = Random.Range(0, 2) == 0 ? other.transform.position - turret.position : other.transform.position - turret.position;

                            // Applying rotation
                            baseTankLogic.RotateTo(desiredDir);
                        }
                    }
                    break;
                case "Mine":
                    // Move in opposite direction of mine
                    desiredDir = transform.position - other.transform.position;

                    // Applying rotation
                    baseTankLogic.RotateTo(desiredDir);
                    break;
            }
        }
    }
    
    void ObstacleAvoidance()
    {
        // Checking Forward
        RaycastHit forwardHit;
        if (Physics.Raycast(body.position, transform.forward, out forwardHit, triggerRadius) || Physics.Raycast(body.position + transform.right, transform.forward, out forwardHit, triggerRadius) || Physics.Raycast(body.position - transform.right, transform.forward, out forwardHit, triggerRadius))
        {
            Debug.DrawLine(body.position, forwardHit.point, Color.red, 0.1f);

            bool[] pathClear = { true, true };
            Vector3 desiredDir;

            // Cross product doesn't give absolute value of angle
            float dotProductY = Vector3.Dot(Vector3.Cross(transform.forward, forwardHit.normal), transform.up);

            mode = Mode.Avoid;

            // Checking Left
            if (Physics.Raycast(body.position, -transform.right, triggerRadius))
            {
                pathClear[0] = false;
            }
            Debug.DrawLine(body.position, body.position - transform.right * 3, Color.red, 0.1f);
            // Checking Right
            if (Physics.Raycast(body.position, transform.right, triggerRadius))
            {
                pathClear[1] = false;
            }
            Debug.DrawLine(body.position, body.position + transform.right * 3, Color.red, 0.1f);

            if (pathClear[0])
            {
                if (pathClear[1])
                {
                    // Rotate left if obstacle is facing left
                    desiredDir = dotProductY < 0 ? -transform.right : transform.right;
                }
                else
                {
                    // Rotate left
                    desiredDir = -transform.right;
                }
            }
            else if (pathClear[1])
            {
                // Rotate right
                desiredDir = transform.right;
            }
            else
            {
                // Rotate backward
                desiredDir = -transform.forward;
            }
            
            // Applying rotation
            baseTankLogic.RotateTo(desiredDir);
        }
        else
        {
            if (mode == Mode.Avoid)
            {
                mode = Mode.Move;
            }
        }
    }
    
    IEnumerator Shoot()
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
}
