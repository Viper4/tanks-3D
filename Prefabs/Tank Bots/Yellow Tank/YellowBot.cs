using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Math;

public class YellowBot : MonoBehaviour
{
    TargetSystem targetSystem;
    Vector3 targetDir;

    BaseTankLogic baseTankLogic;

    Transform body;
    Transform turret;
    Transform barrel;

    [SerializeField] float maxShootAngle = 30;
    public float[] fireDelay = { 0.3f, 0.45f };

    public float[] layDelay = { 0.3f, 0.6f };

    Rigidbody rb;

    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float avoidSpeed = 4f;
    float speed = 4;

    [SerializeField] float gravity = 8;
    float velocityY = 0;

    FireControl fireControl;
    bool layingMine;
    MineControl mineControl;

    enum Mode
    {
        Move,
        Shoot,
        Avoid,
        Lay
    }
    Mode mode = Mode.Move;

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();

        baseTankLogic = GetComponent<BaseTankLogic>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        rb = GetComponent<Rigidbody>();

        fireControl = GetComponent<FireControl>();
        mineControl = GetComponent<MineControl>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null)
        {
            if (fireControl.canFire && mode != Mode.Shoot && mode != Mode.Avoid && targetSystem.TargetVisible())
            {
                StartCoroutine(Shoot());
            }

            if (mineControl.canLay && !layingMine && mode != Mode.Lay && mode != Mode.Avoid)
            {
                StartCoroutine(LayMine());
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

            // Rotating turret and barrel towards target
            targetDir = targetSystem.currentTarget.position - turret.position;
            baseTankLogic.RotateTurretTo(targetDir);
        }
        else
        {
            rb.velocity = Vector3.zero;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        Vector3 desiredDir;
        switch (other.tag)
        {
            case "Mine":
                // Move in opposite direction of mine
                desiredDir = transform.position - other.transform.position;

                // Applying rotation
                baseTankLogic.RotateTankToVector(desiredDir);
                break;
        }
    }

    IEnumerator Shoot()
    {
        // When angle between barrel and target is less than maxShootAngle, then stop and fire
        float angle = Vector3.Angle(barrel.forward, targetDir);
        if (angle < maxShootAngle)
        {
            // Stops moving and delay in firing
            mode = Mode.Shoot;
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
            StartCoroutine(GetComponent<FireControl>().Shoot());

            mode = Mode.Move;
        }
        else
        {
            yield return null;
        }
    }

    IEnumerator LayMine()
    {
        layingMine = true;
        mode = Mode.Lay;
        yield return new WaitForSeconds(Random.Range(layDelay[0], layDelay[1]));
        StartCoroutine(GetComponent<MineControl>().LayMine());
        Vector3 desiredDir = Quaternion.AngleAxis(Random.Range(-180.0f, 180.0f), Vector3.up) * transform.forward;
        rb.rotation = Quaternion.LookRotation(desiredDir);
        
        transform.position += transform.forward * 0.1f;
        mode = Mode.Move;
        layingMine = false;
    }
}
