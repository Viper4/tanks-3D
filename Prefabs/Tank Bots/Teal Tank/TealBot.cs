using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Math;

public class TealBot : MonoBehaviour
{
    TargetSystem targetSystem;

    BaseTankLogic baseTankLogic;

    Transform body;
    Transform turret;

    public float[] fireDelay = { 0.3f, 0.45f };

    Rigidbody rb;
    
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float avoidSpeed = 2f;
    float speed = 4;

    [SerializeField] float gravity = 10;
    float velocityY = 0;
    
    FireControl fireControl;
    bool shooting = false;

    enum Mode
    {
        Move,
        Shoot,
        Avoid
    }
    Mode mode = Mode.Move;

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();

        baseTankLogic = GetComponent<BaseTankLogic>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");

        rb = GetComponent<Rigidbody>();

        fireControl = GetComponent<FireControl>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!GameManager.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null)
        {
            if (fireControl.canFire && mode != Mode.Shoot && !shooting && targetSystem.TargetInLineOfFire())
            {
                StartCoroutine(Shoot());
            }

            if (rb != null)
            {
                // Movement
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
                    case Mode.Shoot:
                        speed = 0;

                        baseTankLogic.noisyRotation = false;
                        break;
                }

                velocity = targetDirection * speed + Vector3.up * velocityY;

                rb.velocity = velocity;
            }

            // Rotating turret and barrel towards player
            Vector3 targetDir = targetSystem.currentTarget.position - turret.position;
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
        // Avoiding mines
        switch (other.tag)
        {
            case "Mine":
                if (GameManager.autoPlay || targetSystem.chooseTarget)
                {
                    // Move in opposite direction of mine
                    desiredDir = transform.position - other.transform.position;

                    // Applying rotation
                    baseTankLogic.RotateTankToVector(desiredDir);
                }
                else if (other.GetComponent<MineBehaviour>().owner != targetSystem.currentTarget.root)
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
        shooting = true;
        // Stops moving and delay in firing
        mode = Mode.Shoot;
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(GetComponent<FireControl>().Shoot());

        mode = Mode.Move;
        shooting = false;
    }
}
