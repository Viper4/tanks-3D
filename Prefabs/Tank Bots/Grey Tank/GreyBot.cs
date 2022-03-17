using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GreyBot : MonoBehaviour
{
    public Transform target;
    float dstToTarget;
    Quaternion rotToTarget;

    public LayerMask layerMasks;

    Transform turret;
    Transform barrel;
    Transform anchor;

    public float shootRadius = 30;

    public float[] reactionTime = { 0.7f, 1.25f };

    public float[] fireDelay = { 0.3f, 0.6f };
    float cooldown = 0;

    Rigidbody rb;

    float turretSeed;
    [SerializeField] Vector2 inaccuracy = new Vector2(10, 25);
    [SerializeField] float turretNoiseSpeed = 0.15f;
    [SerializeField] float turretRotSpeed = 25f;

    [SerializeField] float barrelRotRangeX = 20;

    Vector3 desiredDir;
    Vector3 lastEulerAngles;

    [SerializeField] float tankRotNoiseScale = 5;
    [SerializeField] float tankRotNoiseSpeed = 0.5f;
    [SerializeField] bool randomizeSeed = true;
    [SerializeField] float tankRotSeed = 0;

    public float speed = 3;

    float triggerRadius = 3f;

    enum Mode
    {
        Idle,
        Move,
        Shoot
    }
    Mode mode = Mode.Idle;

    // Start is called before the first frame update
    void Awake()
    {
        if(target == null)
        {
            Debug.Log("The variable target of GreyBot has been defaulted to player's Camera Target");
            target = GameObject.Find("Player").transform.Find("Camera Target");
        }

        turretSeed = Random.Range(-99, 99);

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

        if(dstToTarget < shootRadius && mode != Mode.Shoot && cooldown == 0)
        {
            // origin is offset forward by 1.7 to prevent ray from hitting this tank
            Vector3 origin = anchor.position + anchor.forward * 1.7f;
            // If nothing blocking player
            if (!Physics.Raycast(origin, target.position - origin, dstToTarget, ~layerMasks))
            {
                //Debug.DrawLine(origin, origin + target.position - origin, Color.blue);

                StartCoroutine(Shoot());
            }
        }

        if (rb != null)
        {
            // Movement
            if (mode == Mode.Move)
            {
                rb.velocity = transform.forward * speed;
            }

            if (mode != Mode.Shoot)
            {
                // Rotation
                float noise = tankRotNoiseScale * (Mathf.PerlinNoise(tankRotSeed + Time.time * tankRotNoiseSpeed, (tankRotSeed + 1) + Time.time * tankRotNoiseSpeed) - 0.5f);
                Quaternion desiredTankRot = Quaternion.LookRotation(Quaternion.AngleAxis(noise, Vector3.up) * desiredDir);
                rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, desiredTankRot, Time.deltaTime * tankRotNoiseSpeed));
            }
        }

        // Inaccuracy to rotation with noise
        float noiseX = inaccuracy.x * (Mathf.PerlinNoise(turretSeed + Time.time * turretNoiseSpeed, turretSeed + 1f + Time.time * turretNoiseSpeed) - 0.5f);
        float noiseY = inaccuracy.y * (Mathf.PerlinNoise(turretSeed + 4f + Time.time * turretNoiseSpeed, turretSeed + 5f + Time.time * turretNoiseSpeed) - 0.5f);

        // Correcting turret and barrel rotation to not depend on parent rotation
        turret.rotation = barrel.rotation = anchor.rotation *= Quaternion.Euler(lastEulerAngles - transform.eulerAngles);

        // Rotating turret and barrel towards player
        Vector3 dir = target.position - anchor.position;
        rotToTarget = Quaternion.LookRotation(dir);
        Quaternion desiredRot = anchor.rotation = barrel.rotation = Quaternion.RotateTowards(anchor.rotation, rotToTarget, Time.deltaTime * turretRotSpeed);
        //Quaternion desiredRot = anchor.rotation = barrel.rotation = Quaternion.Lerp(anchor.rotation, rotToTarget, Time.deltaTime * turretRotSpeed);
        barrel.rotation *= Quaternion.Euler(-90, 0, 0);

        turret.eulerAngles = new Vector3(-90, desiredRot.eulerAngles.y + noiseY, desiredRot.eulerAngles.z);
        barrel.eulerAngles = new Vector3(Clamping.ClampAngle(barrel.eulerAngles.x + noiseX, -90 - barrelRotRangeX, -90 + barrelRotRangeX), barrel.eulerAngles.y + noiseY, barrel.eulerAngles.z);

        lastEulerAngles = transform.eulerAngles;
    }

    private void OnTriggerStay(Collider other)
    {
        // Avoiding obstacles
        switch (other.tag)
        {
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
                    mode = Mode.Idle;

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

                    Debug.Log(dotProduct);
                    if (pathClear[0])
                    {
                        if (pathClear[2])
                        {
                            // If the obstacle is directly in front of tank, turn left or right randomly
                            if (dotProduct == 0)
                            {
                                // Rotate left or right
                                desiredDir = Random.Range(0, 2) == 0 ? -transform.right : transform.right;
                            }
                            else
                            {
                                // Rotate left if obstacle is facing left
                                desiredDir = dotProduct < 0 ? -transform.right : transform.right;
                            }
                        }
                        else
                        {
                            // Rotate left
                            desiredDir = -transform.right;
                        }
                    }
                    else if (pathClear[2])
                    {
                        // Rotate right
                        desiredDir = transform.right;
                    }
                    else
                    {
                        // Rotate backward
                        desiredDir = -transform.forward;
                    }
                }
                else
                {
                    mode = Mode.Move;

                    desiredDir = transform.forward;
                }
                break;
        }
    }

    IEnumerator Shoot()
    {
        // When angle between barrel and target is less than minAngle degrees, then stop and fire
        float angle = Quaternion.Angle(barrel.rotation, rotToTarget * Quaternion.Euler(-90, 0, 0));
        if (angle < 45)
        {
            mode = Mode.Shoot;

            // Waiting for tank to move forward a bit more
            yield return new WaitForSeconds(0.5f);

            // Reaction time from seeing player
            yield return new WaitForSeconds(Random.Range(reactionTime[0], reactionTime[1]));
            cooldown = GetComponent<FireControl>().fireCooldown;
            StartCoroutine(GetComponent<FireControl>().Shoot());
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));

            mode = Mode.Move;
        }
        else
        {
            yield return null;
        }
    }
}
