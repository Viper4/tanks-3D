using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TealBot : MonoBehaviour
{
    public Transform target;
    float dstToTarget;
    Quaternion rotToTarget;

    [SerializeField] LayerMask targetLayerMasks;

    Transform turret;
    Transform barrel;
    Transform anchor;

    public float shootRadius = 99f;

    public float[] reactionTime = { 0.3f, 0.45f };

    public float[] fireDelay = { 0.3f, 0.6f };
    float cooldown = 0;

    Rigidbody rb;

    [SerializeField] bool randomizeSeed = true;

    [SerializeField] float turretRotSpeed = 25f;

    [SerializeField] float barrelRotRangeX = 20;

    [SerializeField] float tankRotSpeed = 250f;
    [SerializeField] float tankRotNoiseScale = 5;
    [SerializeField] float tankRotNoiseSpeed = 0.5f;
    [SerializeField] float tankRotSeed = 0;

    Vector3 lastEulerAngles;

    public float speed = 5;
    public float gravity = 5;
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

    // Start is called before the first frame update
    void Awake()
    {
        if (target == null)
        {
            Debug.Log("The variable target of GreyBot has been defaulted to player's Camera Target");
            target = GameObject.Find("Player").transform.Find("Camera Target");
        }

        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");
        anchor = transform.Find("Anchor");

        rb = GetComponent<Rigidbody>();

        lastEulerAngles = anchor.eulerAngles;

        if (randomizeSeed)
        {
            tankRotSeed = Random.Range(0f, 99.0f);
        }

        triggerRadius = GetComponent<SphereCollider>().radius;
    }

    // Update is called once per frame
    void Update()
    {
        cooldown = cooldown > 0 ? cooldown - Time.deltaTime : 0;
        dstToTarget = Vector3.Distance(transform.position, target.position);
        // origin is offset forward by 1.7 to prevent ray from hitting this tank
        Vector3 rayOrigin = anchor.position + anchor.forward * 1.7f;

        if (dstToTarget < shootRadius && mode != Mode.Shoot && cooldown == 0)
        {
            // If nothing blocking player
            if (!Physics.Raycast(rayOrigin, target.position - rayOrigin, dstToTarget, ~targetLayerMasks))
            {
                shootRoutine = StartCoroutine(Shoot());
            }
        }
        else if(shootRoutine != null && Physics.Raycast(rayOrigin, target.position - rayOrigin, dstToTarget, ~targetLayerMasks))
        {
            StopCoroutine(shootRoutine);
            shootRoutine = null;
            mode = Mode.Move;
        }

        if (rb != null)
        {
            // Movement
            Vector3 velocity;
            velocityY = Physics.Raycast(transform.position + Vector3.up * 0.05f, -Vector3.up, 0.1f) ? 0 : velocityY - Time.deltaTime * gravity;
            Vector3 targetDirection = transform.forward;
            
            if (mode == Mode.Move || mode == Mode.Avoid)
            {
                if (Physics.Raycast(transform.position, -transform.up, out RaycastHit middleHit, 1) && Physics.Raycast(transform.position + transform.forward, -transform.up, out RaycastHit frontHit, 1))
                {
                    targetDirection = frontHit - middleHit;
                }
            }
            else
            {
                targetDirection = Vector3.zero;
            }

            if (mode == Mode.Move)
            {
                // Adding noise to rotation
                float noise = tankRotNoiseScale * (Mathf.PerlinNoise(tankRotSeed + Time.time * tankRotNoiseSpeed, (tankRotSeed + 1) + Time.time * tankRotNoiseSpeed) - 0.5f);
                Quaternion desiredTankRot = Quaternion.LookRotation(Quaternion.AngleAxis(noise, Vector3.up) * transform.forward);
                rb.rotation = Quaternion.RotateTowards(transform.rotation, desiredTankRot, Time.deltaTime * tankRotSpeed);
            }
            
            velocity = targetDirection * speed + Vector3.up * velocityY;

            rb.velocity = velocity;
        }

        // Correcting turret and barrel y rotation to not depend on parent rotation
        turret.rotation = barrel.rotation *= Quaternion.AngleAxis(lastEulerAngles.y - transform.eulerAngles.y, Vector3.up);

        // Rotating turret and barrel towards target
        Vector3 dirToTarget = target.position - turret.position;
        rotToTarget = Quaternion.LookRotation(dirToTarget);
        Quaternion desiredTurretRot = barrel.rotation = Quaternion.RotateTowards(barrel.rotation, rotToTarget, Time.deltaTime * turretRotSpeed);

        barrel.localEulerAngles = new Vector3(Clamping.ClampAngle(barrel.localEulerAngles.x, barrelRotRangeX, barrelRotRangeX), barrel.localEulerAngles.y, 0);

        lastEulerAngles = transform.eulerAngles;
    }

    private void OnTriggerStay(Collider other)
    {
        if (mode != Mode.Shoot)
        {
            Vector3 desiredDir;
            // Avoiding bullets, mines, and obstacles
            switch (other.tag)
            {
                case "Bullet":
                    // If bullet is going to hit then dodge by Move perpendicular to bullet path
                    RaycastHit bulletHit;
                    if (Physics.Raycast(other.transform.position, other.transform.forward, out bulletHit))
                    {
                        if (bulletHit.transform == transform)
                        {
                            desiredDir = Random.Range(0, 2) == 0 ? Quaternion.AngleAxis(90, Vector3.up) * (other.transform.position - turret.position) : Quaternion.AngleAxis(-90, Vector3.up) * (other.transform.position - turret.position);

                            // Applying rotation
                            rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(desiredDir), Time.deltaTime * tankRotSpeed * 1.4f));
                        }
                    }
                    break;
                case "Mine":
                    // Move in opposite direction of mine
                    desiredDir = transform.position - other.transform.position;

                    // Applying rotation
                    rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(desiredDir), Time.deltaTime * tankRotSpeed * 1.2f));
                    break;
                default:
                    // Obstacle Avoidance
                    bool[] pathClear = { true, true, true };
                    float dotProduct = 0;
                    Vector3 origin = transform.position + Vector3.up * 0.7f;

                    // Checking Forward
                    RaycastHit forwardHit;
                    if (Physics.Raycast(origin + transform.forward * 1.11f, transform.forward, out forwardHit, triggerRadius)) // center
                    {
                        // Cross product doesn't give absolute value of angle
                        dotProduct = Vector3.Dot(Vector3.Cross(transform.forward, forwardHit.normal), transform.up);
                        pathClear[1] = false;
                    }
                    else if (Physics.Raycast(origin + transform.forward * 1.11f + transform.right * 1, transform.forward, out forwardHit, triggerRadius)) // left side
                    {
                        dotProduct = Vector3.Dot(Vector3.Cross(transform.forward, forwardHit.normal), transform.up);
                        pathClear[1] = false;
                    }
                    else if (Physics.Raycast(origin + transform.forward * 1.11f - transform.right * 1, transform.forward, out forwardHit, triggerRadius)) // right side
                    {
                        dotProduct = Vector3.Dot(Vector3.Cross(transform.forward, forwardHit.normal), transform.up);
                        pathClear[1] = false;
                    }
                    Debug.DrawLine(origin + transform.forward * 1.11f, origin + transform.forward * triggerRadius, Color.blue, 0.1f);

                    if (!pathClear[1])
                    {
                        mode = Mode.Avoid;

                        // Checking Left
                        if (Physics.Raycast(anchor.position - transform.right * 1.11f, -transform.right, triggerRadius))
                        {
                            pathClear[0] = false;
                        }
                        Debug.DrawLine(origin - transform.right * 1.11f, anchor.position - transform.right * 3, Color.red, 0.1f);
                        // Checking Right
                        if (Physics.Raycast(origin + transform.right * 1.11f, transform.right, triggerRadius))
                        {
                            pathClear[2] = false;
                        }
                        Debug.DrawLine(origin + transform.right * 1.11f, anchor.position + transform.right * 3, Color.red, 0.1f);

                        // Over rotating on left and right to prevent jittering
                        if (pathClear[0])
                        {
                            if (pathClear[2])
                            {
                                // If the obstacle is directly in front of tank, turn left or right randomly
                                if (dotProduct == 0)
                                {
                                    // Rotate left or right
                                    desiredDir = Random.Range(0, 2) == 0 ? Quaternion.AngleAxis(-10, Vector3.up) * -transform.right : Quaternion.AngleAxis(10, Vector3.up) * transform.right;
                                }
                                else
                                {
                                    // Rotate left if obstacle is facing left
                                    desiredDir = dotProduct < 0 ? Quaternion.AngleAxis(-10, Vector3.up) * -transform.right : Quaternion.AngleAxis(10, Vector3.up) * transform.right;
                                }
                            }
                            else
                            {
                                // Rotate left
                                desiredDir = Quaternion.AngleAxis(-10, Vector3.up) * -transform.right;
                            }
                        }
                        else if (pathClear[2])
                        {
                            // Rotate right
                            desiredDir = Quaternion.AngleAxis(10, Vector3.up) * transform.right;
                        }
                        else
                        {
                            // Rotate backward
                            desiredDir = -transform.forward;
                        }

                        // Applying rotation
                        rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(desiredDir), Time.deltaTime * tankRotSpeed));
                    }
                    else
                    {
                        mode = Mode.Move;
                    }
                    break;
            }
        }
    }

    IEnumerator Shoot()
    {
        mode = Mode.Shoot;

        // Waiting for tank to move forward a bit more
        yield return new WaitForSeconds(0.5f);

        // Reaction time from seeing player
        yield return new WaitForSeconds(Random.Range(reactionTime[0], reactionTime[1]));
        cooldown = GetComponent<FireControl>().fireCooldown;
        StartCoroutine(GetComponent<FireControl>().Shoot());
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));

        shootRoutine = null;
        mode = Mode.Move;
    }
}
