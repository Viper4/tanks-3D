using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TealBot : MonoBehaviour
{
    public Transform target;
    float dstToTarget;
    Quaternion rotTowardsTarget;

    public LayerMask layerMasks;

    Transform turret;
    Transform barrel;
    Transform anchor;

    public float[] wanderRadius = { 2, 5 };
    public float shootRadius = 30;

    public float[] reactionTime = { 0.3f, 0.45f };

    public float[] fireDelay = { 0.3f, 0.6f };
    float cooldown = 0;

    public float rotateSpeed = 1f;

    public float rotateRangeX = 20;

    Vector3 lastEulerAngles;

    enum Mode
    {
        Wandering,
        Dodging
    }
    Mode mode = Mode.Wandering;

    enum State
    {
        Idle,
        Moving,
        Shooting
    }
    State state = State.Idle;

    NavMeshAgent agent;

    // Start is called before the first frame update
    void Awake()
    {
        if (target == null)
        {
            Debug.Log("The variable target of GreyBot has been defaulted to player's Camera Target");
            target = GameObject.Find("Player").transform.Find("Camera Target");
        }

        agent = GetComponent<NavMeshAgent>();
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

        if (agent != null && state == State.Idle && mode == Mode.Wandering)
        {
            StartCoroutine(MoveTo(GetRandomPoint(anchor.position, transform.forward, wanderRadius[0], wanderRadius[1], 90)));
        }

        // Correcting turret and barrel rotation to not depend on parent rotation
        turret.rotation = barrel.rotation = anchor.rotation *= Quaternion.Euler(lastEulerAngles - transform.eulerAngles);

        // Rotating turret and barrel towards target
        Vector3 dir = target.position - anchor.position;
        rotTowardsTarget = Quaternion.LookRotation(dir);
        Quaternion desiredRot = anchor.rotation = barrel.rotation = Quaternion.RotateTowards(anchor.rotation, rotTowardsTarget, Time.deltaTime * rotateSpeed);

        //Quaternion desiredRot = anchor.rotation = barrel.rotation = Quaternion.Lerp(anchor.rotation, rotTowardsTarget, Time.deltaTime * rotateSpeed);
        barrel.rotation *= Quaternion.Euler(-90, 0, 0);

        turret.eulerAngles = new Vector3(-90, desiredRot.eulerAngles.y, desiredRot.eulerAngles.z);
        barrel.eulerAngles = new Vector3(Clamping.ClampAngle(barrel.eulerAngles.x, -90 - rotateRangeX, -90 + rotateRangeX), barrel.eulerAngles.y, barrel.eulerAngles.z);

        lastEulerAngles = transform.eulerAngles;
    }

    private void OnTriggerEnter(Collider other)
    {
        switch (other.tag)
        {
            case "Bullet":
                if(other.GetComponent<BulletBehaviour>().owner != transform)
                {
                    Debug.Log("Here");
                    mode = Mode.Dodging;
                    Vector3 dir = Random.Range(0, 2) == 0 ? Rotate90CW(other.transform.position - anchor.position) : Rotate90CCW(other.transform.position - anchor.position);
                    StartCoroutine(MoveTo(GetRandomPoint(anchor.position, dir, wanderRadius[0] * 0.5f, wanderRadius[1] * 0.5f, 10)));
                }
                break;
            case "Mine":
                if (other.GetComponent<MineBehaviour>().owner != transform)
                {
                    mode = Mode.Dodging;
                    Vector3 dir = Random.Range(0, 2) == 0 ? Rotate90CW(other.transform.position - anchor.position) : Rotate90CCW(other.transform.position - anchor.position);
                    StartCoroutine(MoveTo(GetRandomPoint(anchor.position, dir, wanderRadius[0] * 0.5f, wanderRadius[1] * 0.5f, 10)));
                }
                break;
        }
    }

    IEnumerator Shoot()
    {
        state = State.Shooting;

        // Waiting for tank to move forward a bit more
        yield return new WaitForSeconds(0.5f);

        // Pausing agent movement
        agent.isStopped = true;
        // Reaction time from seeing player
        yield return new WaitForSeconds(Random.Range(reactionTime[0], reactionTime[1]));
        cooldown = GetComponent<FireControl>().fireCooldown;
        StartCoroutine(GetComponent<FireControl>().Shoot());
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));

        agent.isStopped = false;

        state = State.Idle;
    }

    IEnumerator MoveTo(Vector3 position)
    {
        state = State.Moving;

        if(mode == Mode.Dodging)
        {
            Debug.DrawLine(anchor.position, position, Color.green, 5);
        }
        else
        {
            Debug.DrawLine(anchor.position, position, Color.blue, 2);
        }

        agent.SetDestination(position);

        yield return new WaitUntil(AtEndOfPath);

        state = State.Idle;
        if(mode == Mode.Dodging)
        {
            mode = Mode.Wandering;
        }
    }

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

    bool AtEndOfPath()
    {
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            return true;
        }

        return false;
    }
}
