using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TestBot : MonoBehaviour
{
    public Transform target;
    float dstToTarget;
    Quaternion rotTowardsTarget;

    public LayerMask layerMasks;

    Transform turret;
    Transform barrel;
    Transform anchor;

    public float shootRadius = 30;

    public float[] reactionTime = { 0.3f, 0.45f };

    public float[] fireDelay = { 0.3f, 0.6f };
    float cooldown = 0;

    public float turretRotSpeed = 50f;
    public float barrelRotRangeX = 20;

    public float tankRotSpeed = 10f;

    public float speed = 5;

    Vector3 lastEulerAngles;

    Rigidbody rb;

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

        rb = GetComponent<Rigidbody>();
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");
        anchor = transform.Find("Anchor");

        lastEulerAngles = anchor.eulerAngles;
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
            if (!Physics.Raycast(origin, target.position - origin, dstToTarget, ~layerMasks))
            {
                StartCoroutine(Shoot());
            }
        }

        if (rb != null && state == State.Idle)
        {
            rb.velocity = transform.forward * speed;
        }

        // Correcting turret and barrel rotation to not depend on parent rotation
        turret.rotation = barrel.rotation = anchor.rotation *= Quaternion.Euler(lastEulerAngles - transform.eulerAngles);

        // Rotating turret and barrel towards target
        Vector3 dir = target.position - anchor.position;
        rotTowardsTarget = Quaternion.LookRotation(dir);
        Quaternion desiredRot = anchor.rotation = barrel.rotation = Quaternion.RotateTowards(anchor.rotation, rotTowardsTarget, Time.deltaTime * turretRotSpeed);

        //Quaternion desiredRot = anchor.rotation = barrel.rotation = Quaternion.Lerp(anchor.rotation, rotTowardsTarget, Time.deltaTime * rotateSpeed);
        barrel.rotation *= Quaternion.Euler(-90, 0, 0);

        turret.eulerAngles = new Vector3(-90, desiredRot.eulerAngles.y, desiredRot.eulerAngles.z);
        barrel.eulerAngles = new Vector3(Clamping.ClampAngle(barrel.eulerAngles.x, -90 - barrelRotRangeX, -90 + barrelRotRangeX), barrel.eulerAngles.y, barrel.eulerAngles.z);

        lastEulerAngles = transform.eulerAngles;
    }

    private void OnTriggerStay(Collider other)
    {
        // Obstacle Avoidance
        if(Physics.Raycast(anchor.position + anchor.forward * 1.7f, anchor.forward, 3))
        {

        }
        if(Physics.Raycast(anchor.position + anchor.right * 1.7f, anchor.right, 3))
        {

        }
        if (Physics.Raycast(anchor.position - anchor.right * 1.7f, -anchor.right, 3))
        {

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Vector3 dir;
        switch (other.tag)
        {
            case "Bullet":
                // When bullet is going to hit then dodge by moving perpendicular to bullet path
                RaycastHit hit;
                if(Physics.Raycast(other.transform.position, other.transform.forward, out hit))
                {
                    if(hit.transform == transform)
                    {
                        dir = Random.Range(0, 2) == 0 ? Rotate90CW(other.transform.position - anchor.position) : Rotate90CCW(other.transform.position - anchor.position);
                        rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * tankRotSpeed));
                    }
                }
                break;
            case "Mine":
                // Move in opposite direction of mine
                dir = anchor.position - other.transform.position;
                rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * tankRotSpeed));
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
