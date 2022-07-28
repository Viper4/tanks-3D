using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Math;

public class RedBot : MonoBehaviour
{
    TargetSelector targetSelector;
    Vector3 targetDir;

    BaseTankLogic baseTankLogic;

    Transform body;
    Transform turret;
    Transform barrel;

    public float[] reactionTime = { 0.28f, 0.425f };
    public float[] fireDelay = { 0.2f, 0.45f };

    Rigidbody rb;

    float angleToTarget;
    [SerializeField] float maxShootAngle = 5;

    [Tooltip("Threshold angle to start rotating tank to target")] [SerializeField] float maxTargetAngle = 100;

    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float avoidSpeed = 3f;
    float speed = 5;

    [SerializeField] float gravity = 8;
    float velocityY = 0;

    FireControl fireControl;
    bool shooting = false;

    enum Mode
    {
        Move,
        Shoot,
        Avoid,
        Defend
    }
    Mode mode = Mode.Move;

    // Start is called before the first frame Update
    void Start()
    {
        targetSelector = GetComponent<TargetSelector>();

        baseTankLogic = GetComponent<BaseTankLogic>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        rb = GetComponent<Rigidbody>();

        fireControl = GetComponent<FireControl>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.frozen && Time.timeScale != 0 && targetSelector.currentTarget != null)
        {
            targetDir = targetSelector.currentTarget.position - turret.position;
            angleToTarget = Vector3.Angle(transform.forward, targetDir);

            if (fireControl.canFire && mode != Mode.Shoot && !shooting && Physics.Raycast(barrel.position, targetSelector.currentTarget.position - barrel.position, out RaycastHit barrelHit, Mathf.Infinity, ~baseTankLogic.transparentLayers, QueryTriggerInteraction.Ignore))
            {
                // Ray hits the capsule collider which is on Tank Origin for player and the 2nd topmost transform for tank bots
                if (barrelHit.transform.root.name == "Player" && targetSelector.currentTarget.root.name == "Player")
                {
                    StartCoroutine(Shoot());
                }
                else if (barrelHit.transform == targetSelector.currentTarget.parent || barrelHit.transform == targetSelector.currentTarget) // target for tank bots is the turret, everything else is its own transform
                {
                    StartCoroutine(Shoot());
                }
            }

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
                    mode = Mode.Move;
                }

                switch (mode)
                {
                    case Mode.Move:
                        // Resetting currentTarget to primary and trying to move to target
                        if (!GameManager.autoPlay && !targetSelector.findTarget && targetSelector.primaryTarget != null)
                        {
                            targetSelector.currentTarget = targetSelector.primaryTarget;
                        }
                        // Only rotating towards target when target is getting behind this tank
                        if (angleToTarget > maxTargetAngle)
                        {
                            baseTankLogic.RotateTankToVector(targetDir);
                        }

                        speed = moveSpeed;

                        baseTankLogic.noisyRotation = true;
                        break;
                    case Mode.Defend:
                        // OnTriggerStay handles rotation
                        speed = moveSpeed;

                        baseTankLogic.noisyRotation = true;
                        mode = Mode.Move;
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
            }

            baseTankLogic.RotateTurretTo(targetDir);
        }
        else
        {
            rb.velocity = Vector3.zero;
        }
    }

    void OnTriggerStay(Collider other)
    {
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
                    baseTankLogic.RotateTankToVector(desiredDir);

                    // Firing at bullets
                    targetSelector.currentTarget = other.transform;
                }
                break;
            case "Mine":
                if (GameManager.autoPlay || targetSelector.findTarget)
                {
                    // Move in opposite direction of mine
                    desiredDir = transform.position - other.transform.position;

                    // Applying rotation
                    baseTankLogic.RotateTankToVector(desiredDir);
                }
                else if (other.GetComponent<MineBehaviour>().owner != targetSelector.primaryTarget.root)
                {
                    // Move in opposite direction of mine
                    desiredDir = transform.position - other.transform.position;

                    // Applying rotation
                    baseTankLogic.RotateTankToVector(desiredDir);
                }
                break;
        }
    }

    IEnumerator Shoot()
    {
        // When angle between barrel and target is less than shootAngle, then stop and fire
        float shootAngle = mode == Mode.Defend ? maxShootAngle * 4f : maxShootAngle;
        if (Vector3.Angle(barrel.forward, targetDir) < shootAngle)
        {
            shooting = true;

            // Keeps moving until reaction time from seeing player is reached
            yield return new WaitForSeconds(Random.Range(reactionTime[0], reactionTime[1]));

            // Stops moving and delay in firing
            mode = Mode.Shoot;
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
            StartCoroutine(GetComponent<FireControl>().Shoot());

            mode = Mode.Move;
            shooting = false;
        }
    }
}
