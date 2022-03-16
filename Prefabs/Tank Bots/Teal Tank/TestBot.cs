using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TestBot : MonoBehaviour
{
    public Transform target;
    float dstToTarget;
    Quaternion rotToTarget;

    [SerializeField] LayerMask targetLayerMasks;

    Transform turret;
    Transform barrel;
    Transform anchor;

    public float shootRadius = 30f;

    public float[] reactionTime = { 0.3f, 0.45f };

    public float[] fireDelay = { 0.3f, 0.6f };
    float cooldown = 0;

    Rigidbody rb;

    [SerializeField] float turretRotSpeed = 50f;
    [SerializeField] float barrelRotRangeX = 20f;

    [SerializeField] float tankRotSpeed = 10f;
    Vector3 desiredDir;
    Vector3 lastEulerAngles;
    
    [SerializeField] float noiseScale = 1;
    [SerializeField] float noiseSpeed = 1f;
    [SerializeField] bool randomizeSeed = true;
    [SerializeField] float seed = 0;

    public float speed = 5;

    enum State
    {
        Idle,
        Moving,
        Shooting
    }
    State state = State.Idle;

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
        
        if(randomizeSeed)
        {
            seed = Random.Range(0f, 99.0f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        cooldown = cooldown > 0 ? cooldown - Time.deltaTime : 0;
        dstToTarget = Vector3.Distance(transform.position, target.position);

        if (dstToTarget < shootRadius && state != State.Shooting && cooldown == 0)
        {
            // origin is offset forward by 1.7 to prevent ray from hitting this tank
            Vector3 origin = anchor.position + anchor.forward * 1.7f;
            // If nothing blocking player
            if (!Physics.Raycast(origin, target.position - origin, dstToTarget, ~targetLayerMasks))
            {
                StartCoroutine(Shoot());
            }
        }
        
        if (rb != null)
        {
            // Movement
            if (state == State.Moving)
            {
                rb.velocity = transform.forward * speed; 
            }
            // Rotation
            float noise = noiseScale * (Mathf.PerlinNoise(seed + Time.time * noiseSpeed, (seed + 1) + Time.time * noiseSpeed) - 0.5f);
            Quaternion desiredTankRot = Quaternion.LookRotation(desiredDir * Quaternion.AngleAxis(noise, Vector3.up));
            rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, desiredTankRot, Time.deltaTime * tankRotSpeed));
        }

        // Correcting turret and barrel rotation to not depend on parent rotation
        turret.rotation = barrel.rotation = anchor.rotation *= Quaternion.Euler(lastEulerAngles - transform.eulerAngles);

        // Rotating turret and barrel towards target
        Vector3 dirToTarget = target.position - anchor.position;
        rotToTarget = Quaternion.LookRotation(dirToTarget);
        Quaternion desiredTurretRot = anchor.rotation = barrel.rotation = Quaternion.RotateTowards(anchor.rotation, rotToTarget, Time.deltaTime * turretRotSpeed);

        barrel.rotation *= Quaternion.Euler(-90, 0, 0);

        turret.eulerAngles = new Vector3(-90, desiredTurretRot.eulerAngles.y, desiredTurretRot.eulerAngles.z);
        barrel.eulerAngles = new Vector3(Clamping.ClampAngle(barrel.eulerAngles.x, -90 - barrelRotRangeX, -90 + barrelRotRangeX), barrel.eulerAngles.y, barrel.eulerAngles.z);

        lastEulerAngles = transform.eulerAngles;
    }

    private void OnTriggerStay(Collider other)
    {    
        // Avoiding bullets, mines, and obstacles
        switch (other.tag)
        {
            case "Bullet":
                // When bullet is going to hit then dodge by moving perpendicular to bullet path
                RaycastHit hit;
                if(Physics.Raycast(other.transform.position, other.transform.forward, out hit))
                {
                    if(hit.transform == transform)
                    {
                        desiredDir = Random.Range(0, 2) == 0 ? Rotate90CW(other.transform.position - anchor.position) : Rotate90CCW(other.transform.position - anchor.position);
                    }
                }
                break;
            case "Mine":
                // Move in opposite direction of mine
                desiredDir = anchor.position - other.transform.position;
                break;
            default:
                // Obstacle Avoidance
                bool[] pathClear = { true, true, true };
                float hitAngle = 0;
                RaycastHit hit;
                // Forward
                if(Physics.Raycast(anchor.position + anchor.forward * 1.7f, anchor.forward, out hit, 3))
                {
                    // Dot product doesn't give absolute value of angle
                    hitAngle = Vector3.Dot(anchor.forward, hit.normal);
                    pathClear[1] = false;
                }

                if(!pathClear[1])
                {
                    // Checking Left
                    if(Physics.Raycast(anchor.position - anchor.right * 1.7f, -anchor.right, out hit, 3))
                    {
                        pathClear[0] = false;
                    }
                    // Checking Right
                    if (Physics.Raycast(anchor.position + anchor.right * 1.7f, anchor.right, out hit, 3))
                    {
                        pathClear[2] = false;
                    }
                    
                    if (pathClear[0]) 
                    {
                        if (pathClear[2]) 
                        {
                            // If the obstacle is directly in front of tank, turn left or right randomly
                            if(hitAngle == 0 || hitAngle == 360)
                            {
                                desiredDir = Random.Range(0, 2) == 0 ? -anchor.right : anchor.right;
                            }
                            else
                            {
                                // Turn left if obstacle is facing left
                                desiredDir = hitAngle < 0 ? -anchor.right : anchor.right;
                            }
                        }
                        else 
                        {
                            // Rotate left
                            desiredDir = -anchor.right;
                        }
                    }
                    else if (pathClear[2]) 
                    {
                        // Rotate right
                        desiredDir = anchor.right;
                    }
                    else
                    {
                        // Rotate backward
                        desiredDir = -anchor.forward;
                    }
                }
                else 
                {
                    desiredDir = anchor.forward;
                }
                break;
        }
    }
    
    IEnumerator Shoot()
    {
        state = State.Shooting;

        // Waiting for tank to move forward a bit more
        yield return new WaitForSeconds(0.5f);
        
        // Reaction time from seeing player
        yield return new WaitForSeconds(Random.Range(reactionTime[0], reactionTime[1]));
        cooldown = GetComponent<FireControl>().fireCooldown;
        StartCoroutine(GetComponent<FireControl>().Shoot());
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));

        state = State.Idle;
    }

    // Gets a random point in a cone of a certain direction
    Vector3 GetRandomPoint(Vector3 origin, Vector3 dir, float minRadius, float maxRadius, float angle)
    {
        float randomDst = Random.Range(minRadius, maxRadius);

        // When 15 random points are picked and none are a valid position, return the origin position
        for (int i = 0; i < 16; i++)
        {
            // Random angle from -angle/2 and angle/2
            float randomAngle = Random.Range(angle * -0.5f, angle * 0.5f);

            // Generating random direction along y axis rotation within randomAngle from dir vector
            Vector3 randomDir = Quaternion.AngleAxis(randomAngle, Vector3.up) * dir;

            // Reversing direction after 8 failed tries
            if (i > 8)
            {
                randomDir *= -1;
            }

            // Checking if point is reachable on navmesh and getting in game point on navmesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(origin + (randomDir * randomDst), out hit, maxRadius, 1))
            {
                return hit.position;
            }
        }

        return origin;
    }
    
    // clockwise
    Vector3 Rotate90CW(Vector3 aDir)
    {
        return new Vector3(aDir.z, 0, -aDir.x);
    }
    // counter clockwise
    Vector3 Rotate90CCW(Vector3 aDir)
    {
        return new Vector3(-aDir.z, 0, aDir.x);
    }
}
