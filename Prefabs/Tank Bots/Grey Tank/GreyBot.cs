using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreyBot : MonoBehaviour
{
    public Transform target;
    float dstToTarget;
    Quaternion rotToTarget;

    [SerializeField] LayerMask targetLayerMasks;

    Transform body;
    Transform turret;
    Transform barrel;

    public float shootRadius = 30;

    public float[] reactionTime = { 0.7f, 1.25f };

    public float[] fireDelay = { 0.3f, 0.6f };
    float cooldown = 0;

    Rigidbody rb;

    [SerializeField] Vector2 inaccuracy = new Vector2(10, 25);

    [SerializeField] bool randomizeSeed = true;

    [SerializeField] float turretRotSpeed = 25f;
    [SerializeField] float turretNoiseSpeed = 0.15f;
    [SerializeField] float turretRotSeed = 0;

    [SerializeField] float barrelRotRangeX = 20;

    [SerializeField] float tankRotSpeed = 250f;
    [SerializeField] float tankRotNoiseScale = 5;
    [SerializeField] float tankRotNoiseSpeed = 0.5f;
    [SerializeField] float tankRotSeed = 0;

    Vector3 lastEulerAngles;

    public float speed = 3;
    public float gravity = 5;
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

    // Start is called before the first frame update
    void Awake()
    {
        if (target == null)
        {
            Debug.Log("The variable target of GreyBot has been defaulted to player's Camera Target");
            target = GameObject.Find("Player").transform.Find("Camera Target");
        }

        turretRotSeed = Random.Range(-99, 99);

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        rb = GetComponent<Rigidbody>();

        lastEulerAngles = body.eulerAngles;

        if (randomizeSeed)
        {
            turretRotSeed = Random.Range(0f, 99.0f);
            tankRotSeed = Random.Range(0f, 99.0f);
        }

        triggerRadius = GetComponent<SphereCollider>().radius;
    }

    // Update is called once per frame
    void Update()
    {
        cooldown = cooldown > 0 ? cooldown - Time.deltaTime : 0;
        dstToTarget = Vector3.Distance(body.position, target.position);
        // Origin is offset forward by 1.7 to prevent ray from hitting this tank
        Vector3 rayOrigin = body.position + body.forward * 1.7f;

        if (dstToTarget < shootRadius && mode != Mode.Shoot && cooldown == 0)
        {
            // If nothing blocking player
            if (!Physics.Raycast(rayOrigin, target.position - rayOrigin, dstToTarget, ~targetLayerMasks))
            {
                StartCoroutine(Shoot());
            }
        }
        else if (shootRoutine != null && Physics.Raycast(rayOrigin, target.position - rayOrigin, dstToTarget, ~targetLayerMasks))
        {
            StopCoroutine(shootRoutine);
            shootRoutine = null;
            mode = Mode.Move;
        }

        if (rb != null)
        {
            Vector3 velocity;
            velocityY = Physics.Raycast(transform.position + Vector3.up * 0.05f, -Vector3.up, 0.1f) ? 0 : velocityY - Time.deltaTime * gravity;

            if (mode == Mode.Move || mode == Mode.Avoid)
            {
                velocity = transform.forward * speed;
            }
            else
            {
                velocity = Vector3.zero;
            }

            rb.velocity = velocity + Vector3.up * velocityY;

            if (mode == Mode.Move)
            {
                // Adding noise to rotation
                float noise = tankRotNoiseScale * (Mathf.PerlinNoise(tankRotSeed + Time.time * tankRotNoiseSpeed, (tankRotSeed + 1) + Time.time * tankRotNoiseSpeed) - 0.5f);
                Quaternion desiredTankRot = Quaternion.LookRotation(Quaternion.AngleAxis(noise, Vector3.up) * transform.forward);
                rb.rotation = Quaternion.RotateTowards(transform.rotation, desiredTankRot, Time.deltaTime * tankRotSpeed);
            }
        }

        // Inaccuracy to rotation with noise
        float noiseX = inaccuracy.x * (Mathf.PerlinNoise(turretRotSeed + Time.time * turretNoiseSpeed, turretRotSeed + 1f + Time.time * turretNoiseSpeed) - 0.5f);
        float noiseY = inaccuracy.y * (Mathf.PerlinNoise(turretRotSeed + 4f + Time.time * turretNoiseSpeed, turretRotSeed + 5f + Time.time * turretNoiseSpeed) - 0.5f);

        // Correcting turret and barrel y rotation to not depend on the parent
        turret.eulerAngles = barrel.eulerAngles = new Vector3(barrel.eulerAngles.x, barrel.eulerAngles.y + (lastEulerAngles.y - transform.eulerAngles.y), barrel.eulerAngles.z);

        // Rotating turret and barrel towards player
        Vector3 targetDir = target.position - body.position;
        rotToTarget = Quaternion.LookRotation(targetDir);
        turret.rotation = barrel.rotation = Quaternion.RotateTowards(turret.rotation, rotToTarget, Time.deltaTime * turretRotSpeed);

        turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y + noiseY, turret.localEulerAngles.z);
        barrel.localEulerAngles = new Vector3(Clamping.ClampAngle(barrel.localEulerAngles.x + noiseX, barrelRotRangeX, barrelRotRangeX), barrel.localEulerAngles.y + noiseY, barrel.localEulerAngles.z);

        lastEulerAngles = transform.eulerAngles;
    }

    private void OnTriggerStay(Collider other)
    {
        Vector3 desiredDir;

        // Obstacle Avoidance
        bool[] pathClear = { true, true, true };
        float dotProductY = 0;
        Vector3 origin = transform.position + Vector3.up * 0.7f;

        // Checking Forward
        if (Physics.Raycast(origin + transform.forward * 1.11f, transform.forward, out RaycastHit forwardHit, triggerRadius) || Physics.Raycast(origin + transform.forward * 1.11f + transform.right * 1, transform.forward, out forwardHit, triggerRadius) || Physics.Raycast(origin + transform.forward * 1.11f - transform.right * 1, transform.forward, out forwardHit, triggerRadius)) // testing forward from center, left, right
        {
            // Avoid obstacle if the obstacle slant is <35 degrees
            if (Mathf.Abs(forwardHit.transform.eulerAngles.x) < 35 && Mathf.Abs(forwardHit.transform.eulerAngles.z) < 35)
            {
                // Dot product doesn't give absolute value of angle
                dotProductY = Vector3.Dot(Vector3.Cross(transform.forward, forwardHit.normal), transform.up);
                pathClear[1] = false;
            }
        }
        Debug.DrawLine(origin + transform.forward * 1.11f, origin + transform.forward * triggerRadius, Color.blue, 0.1f);

        if (!pathClear[1])
        {
            mode = Mode.Avoid;

            // Checking Left
            if (Physics.Raycast(body.position - transform.right * 1.11f, -transform.right, triggerRadius))
            {
                pathClear[0] = false;
            }
            Debug.DrawLine(origin - transform.right * 1.11f, body.position - transform.right * 3, Color.red, 0.1f);
            // Checking Right
            if (Physics.Raycast(origin + transform.right * 1.11f, transform.right, triggerRadius))
            {
                pathClear[2] = false;
            }
            Debug.DrawLine(origin + transform.right * 1.11f, body.position + transform.right * 3, Color.red, 0.1f);

            // Over rotating on left and right to prevent jittering
            if (pathClear[0])
            {
                if (pathClear[2])
                {
                    // If the obstacle is directly in front of tank, turn left or right randomly
                    if (dotProductY == 0)
                    {
                        // Rotate left or right
                        desiredDir = Random.Range(0, 2) == 0 ? Quaternion.AngleAxis(-10, Vector3.up) * -transform.right : Quaternion.AngleAxis(10, Vector3.up) * transform.right;
                    }
                    else
                    {
                        // Rotate left if obstacle is facing left
                        desiredDir = dotProductY < 0 ? Quaternion.AngleAxis(-10, Vector3.up) * -transform.right : Quaternion.AngleAxis(10, Vector3.up) * transform.right;
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
                // Go backward
                speed = speed < 0 ? speed : -speed;
                desiredDir = transform.forward;
            }

            // Applying rotation
            rb.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(desiredDir), Time.deltaTime * tankRotSpeed);
        }
        else
        {
            // Go forward if going backward
            speed = speed < 0 ? -speed : speed;
            if (mode == Mode.Avoid)
            {
                mode = Mode.Move;
            }
        }
    }

    IEnumerator Shoot()
    {
        // When angle between barrel and target is less than minAngle degrees, then stop and fire
        float angle = Quaternion.Angle(barrel.rotation, rotToTarget * Quaternion.Euler(-90, 0, 0));
        if (angle < 45)
        {
            // Waiting for tank to move forward a bit more
            yield return new WaitForSeconds(0.5f);
            mode = Mode.Shoot;

            // Reaction time from seeing player
            yield return new WaitForSeconds(Random.Range(reactionTime[0], reactionTime[1]));
            cooldown = GetComponent<FireControl>().fireCooldown;
            StartCoroutine(GetComponent<FireControl>().Shoot());
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));

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
