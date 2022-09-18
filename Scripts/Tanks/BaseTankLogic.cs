using UnityEngine;
using Photon.Pun;
using MyUnityAddons.Calculations;
using MyUnityAddons.CustomPhoton;
using System.Collections;

public class BaseTankLogic : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private bool player = false;
    [SerializeField] private PlayerControl playerControl;

    [Header("Death")]
    [SerializeField] private Transform explosionEffect;
    [SerializeField] private Transform deathMarker;
    public Transform effectsParent;

    [Header("Slope Settings")]
    public LayerMask barrierLayers;
    [SerializeField] private LayerMask slopeLayers;
    [SerializeField] private bool slopeAlignment = true;
    [SerializeField] private float alignRotationSpeed = 20;

    [Header("Tank Movement")]
    [SerializeField] float maxSlopeAngle = 35;
    public bool stationary = false;
    [SerializeField] bool obstacleAvoidance = true;
    public float normalSpeed = 5;
    [SerializeField] float avoidSpeed = 2.5f;
    public bool useGravity = true;
    public float gravity = 8;
    public float velocityLimit = 25;
    float speed = 5;
    float velocityY = 0;

    [Header("Tank Rotation")]
    public float flipAngleThreshold = 20;
    [SerializeField] private bool restrictRotation = false;
    [SerializeField] private float[] pitchRange = { -45, 45 };
    [SerializeField] private float[] rollRange = { -45, 45 };

    [Header("Tank Rotation Noise")]
    public bool tankNoise = false;
    [SerializeField] private float tankRotSpeed = 250f;
    [SerializeField] private float tankRotNoiseScale = 5;
    [SerializeField] private float tankRotNoiseSpeed = 0.5f;

    [Header("Turret Rotation")]
    [SerializeField] bool turretRotation = true;
    [SerializeField] bool turretNoise = false;
    [SerializeField] private float turretRotSpeed = 35f;
    [SerializeField] private Vector2 inaccuracy = new Vector2(4, 6);
    [SerializeField] private float turretNoiseSpeed = 0.25f;
    public float[] turretRangeX = { -20, 20 };
    public float[] turretRangeY = { -180, 180 };
    [HideInInspector] public bool overrideRotation = false;

    private float tankRotSeed = 0;
    private float turretRotSeed = 0;

    public Vector3 targetTurretDir;
    public Vector3 targetTankDir;
    Vector3 lastEulerAngles;

    public bool disabled = false;
    [SerializeField] private Transform tankOrigin;
    Collider tankCollider;
    float colliderExtentsY = 0;
    Transform body;
    Transform turret;
    Transform barrel;
    Transform trackMarks;
    Rigidbody rb;

    private void Start() 
    {
        tankRotSeed = Random.Range(-999.0f, 999.0f);
        turretRotSeed = Random.Range(-999.0f, 999.0f);

        tankCollider = tankOrigin.GetComponent<Collider>();
        Quaternion storedRotation = tankOrigin.rotation;
        tankOrigin.rotation = Quaternion.identity;
        colliderExtentsY = tankCollider.bounds.extents.y;
        tankOrigin.rotation = storedRotation;

        body = tankOrigin.Find("Body");
        turret = tankOrigin.Find("Turret");
        barrel = tankOrigin.Find("Barrel");
        trackMarks = tankOrigin.Find("TrackMarks");
        rb = tankOrigin.GetComponent<Rigidbody>();
        lastEulerAngles = tankOrigin.eulerAngles;
        speed = normalSpeed;
        targetTankDir = body.forward;
        if (GameManager.Instance.autoPlay)
        {
            effectsParent = GameObject.Find("ToClear").transform;
        }
    }

    private void Update()
    {
        if (!disabled && !GameManager.Instance.frozen && Time.timeScale != 0)
        {
            speed = normalSpeed;

            if (slopeAlignment)
            {
                if (Physics.Raycast(tankCollider.bounds.center, Vector3.down, out RaycastHit hit, colliderExtentsY + 0.2f, slopeLayers))
                {
                    // Rotating to align with slope
                    Quaternion alignedRotation = Quaternion.FromToRotation(tankOrigin.up, hit.normal);
                    tankOrigin.rotation = Quaternion.RotateTowards(tankOrigin.rotation, alignedRotation * tankOrigin.rotation, alignRotationSpeed * Time.deltaTime);
                }
            }

            if (restrictRotation)
            {
                // Ensuring tank doesn't flip over
                tankOrigin.eulerAngles = new Vector3(CustomMath.ClampAngle(tankOrigin.eulerAngles.x, pitchRange[0], pitchRange[1]), tankOrigin.eulerAngles.y, CustomMath.ClampAngle(tankOrigin.eulerAngles.z, rollRange[0], rollRange[1]));
            }

            if (!player)
            {
                if (!stationary)
                {
                    if (obstacleAvoidance)
                    {
                        float normalAngleY = 181;
                        float slopeAngle = 0; 

                        bool[] pathClear = { true, true };
                        if (Physics.Raycast(body.position, body.forward, out RaycastHit forwardHit, 1.5f, barrierLayers))
                        {
                            normalAngleY = Vector3.SignedAngle(body.forward, forwardHit.normal, body.up);
                            slopeAngle = Vector3.Angle(forwardHit.normal, Vector3.up);

                            Debug.DrawLine(body.position, forwardHit.point, Color.red, 0.1f);
                        }
                        else if (Physics.Raycast(body.position, body.forward - body.right, out RaycastHit forwardHitL, 1.5f, barrierLayers))
                        {
                            normalAngleY = Vector3.SignedAngle(body.forward, forwardHitL.normal, body.up);
                            slopeAngle = Vector3.Angle(forwardHitL.normal, Vector3.up);

                            Debug.DrawLine(body.position, forwardHitL.point, Color.red, 0.1f);
                        }
                        else if (Physics.Raycast(body.position, body.forward + body.right, out RaycastHit forwardHitR, 1.5f, barrierLayers))
                        {
                            normalAngleY = Vector3.SignedAngle(body.forward, forwardHitR.normal, body.up);
                            slopeAngle = Vector3.Angle(forwardHitR.normal, Vector3.up);

                            Debug.DrawLine(body.position, forwardHitR.point, Color.red, 0.1f);
                        }

                        if (slopeAngle > maxSlopeAngle)
                        {
                            if (normalAngleY != 181)
                            {
                                speed = avoidSpeed;

                                if (Physics.Raycast(body.position, -body.right, out RaycastHit leftHit, 1.5f, barrierLayers))
                                {
                                    pathClear[0] = false;
                                    Debug.DrawLine(body.position, leftHit.point, Color.red, 0.1f);
                                }
                                else if (Physics.Raycast(body.position + body.forward, -body.right, out RaycastHit leftHitF, 1.5f, barrierLayers))
                                {
                                    pathClear[0] = false;
                                    Debug.DrawLine(body.position + body.forward, leftHitF.point, Color.red, 0.1f);
                                }
                                else if (Physics.Raycast(body.position - body.forward, -body.right, out RaycastHit leftHitB, 1.5f, barrierLayers))
                                {
                                    pathClear[0] = false;
                                    Debug.DrawLine(body.position - body.forward, leftHitB.point, Color.red, 0.1f);
                                }
                                if (Physics.Raycast(body.position, body.right, out RaycastHit rightHit, 1.25f, barrierLayers))
                                {
                                    pathClear[1] = false;
                                    Debug.DrawLine(body.position, rightHit.point, Color.red, 0.1f);
                                }
                                else if (Physics.Raycast(body.position + body.forward, body.right, out RaycastHit rightHitF, 1.5f, barrierLayers))
                                {
                                    pathClear[1] = false;
                                    Debug.DrawLine(body.position + body.forward, rightHitF.point, Color.red, 0.1f);
                                }
                                else if (Physics.Raycast(body.position - body.forward, body.right, out RaycastHit rightHitB, 1.5f, barrierLayers))
                                {
                                    pathClear[1] = false;
                                    Debug.DrawLine(body.position - body.forward, rightHitB.point, Color.red, 0.1f);
                                }

                                if (pathClear[0])
                                {
                                    if (pathClear[1])
                                    {
                                        if (normalAngleY < 0)
                                        {
                                            targetTankDir = -body.right;
                                        }
                                        else
                                        {
                                            targetTankDir = body.right;
                                        }
                                    }
                                    else
                                    {
                                        targetTankDir = -body.right;
                                    }
                                }
                                else if (pathClear[1])
                                {
                                    targetTankDir = body.right;
                                }
                                else
                                {
                                    targetTankDir = -body.forward;
                                }
                            }
                        }
                    }

                    // Tank rotation
                    if (tankNoise)
                    {
                        // Adding noise to rotation
                        float noise = tankRotNoiseScale * (Mathf.PerlinNoise(tankRotSeed + Time.time * tankRotNoiseSpeed, tankRotSeed + 1 + Time.time * tankRotNoiseSpeed) - 0.5f);
                        RotateTankToVector(Quaternion.AngleAxis(noise, tankOrigin.up) * targetTankDir);
                    }
                    else
                    {
                        RotateTankToVector(targetTankDir);
                    }
                }

                // Tank movement
                if (rb != null)
                {
                    Vector3 velocityDirection = transform.forward;
                    if (Physics.Raycast(transform.position, -transform.up, out RaycastHit middleHit, 1) && Physics.Raycast(transform.position + transform.forward, -transform.up, out RaycastHit frontHit, 1))
                    {
                        velocityDirection = frontHit.point - middleHit.point;
                    }

                    if (useGravity)
                    {
                        velocityY = !IsGrounded() ? velocityY - Time.deltaTime * gravity : 0;
                        velocityY = Mathf.Clamp(velocityY, -velocityLimit, velocityLimit);

                        rb.velocity = stationary ? Vector3.up * velocityY : velocityDirection * speed + Vector3.up * velocityY;
                    }
                    else // Using rigidbody gravity instead
                    {
                        rb.velocity = stationary ? Vector3.up * rb.velocity.y : velocityDirection * speed + Vector3.up * rb.velocity.y;
                    }
                }

                if (turretRotation && targetTurretDir != Vector3.zero)
                {
                    float noiseX = 0;
                    float noiseY = 0;

                    if (turretNoise)
                    {
                        // Inaccuracy to rotation with noise
                        noiseX = inaccuracy.x * (Mathf.PerlinNoise(turretRotSeed + Time.time * turretNoiseSpeed, turretRotSeed + 1f + Time.time * turretNoiseSpeed) - 0.5f);
                        noiseY = inaccuracy.y * (Mathf.PerlinNoise(turretRotSeed + 4f + Time.time * turretNoiseSpeed, turretRotSeed + 5f + Time.time * turretNoiseSpeed) - 0.5f);
                    }

                    // Rotating turret and barrel towards vector
                    Quaternion targetTurretRot = Quaternion.LookRotation(Quaternion.AngleAxis(noiseY, turret.up) * Quaternion.AngleAxis(noiseX, turret.right) * targetTurretDir, turret.up);
                    turret.rotation = barrel.rotation = Quaternion.RotateTowards(barrel.rotation, targetTurretRot, Time.deltaTime * turretRotSpeed);

                    // Correcting turret and barrel y rotation to not depend on the parent
                    turret.eulerAngles = new Vector3(turret.eulerAngles.x, turret.eulerAngles.y + lastEulerAngles.y - transform.eulerAngles.y, turret.eulerAngles.z);
                    barrel.eulerAngles = new Vector3(barrel.eulerAngles.x, barrel.eulerAngles.y + lastEulerAngles.y - transform.eulerAngles.y, barrel.eulerAngles.z);

                    // Zeroing x and z eulers of turret and clamping x and y eulers
                    float clampedX = CustomMath.ClampAngle(barrel.localEulerAngles.x, turretRangeX[0], turretRangeX[1]);
                    float clampedY = CustomMath.ClampAngle(turret.localEulerAngles.y, turretRangeY[0], turretRangeY[1]);

                    turret.localEulerAngles = new Vector3(0, clampedY, 0);
                    barrel.localEulerAngles = new Vector3(clampedX, clampedY, 0);

                    lastEulerAngles = tankOrigin.eulerAngles;
                }                
            }
        }
        else
        {
            rb.velocity = Vector3.zero;
        }
    }



    [PunRPC]
    public void ExplodeTank()
    {
        Instantiate(explosionEffect, tankOrigin.position, Quaternion.identity, effectsParent);
        if (Physics.Raycast(tankOrigin.position + Vector3.up, Vector3.down, out RaycastHit groundHit, Mathf.Infinity, slopeLayers))
        {
            Transform newMarker = Instantiate(deathMarker, groundHit.point + groundHit.normal * deathMarker.localPosition.y, Quaternion.identity, effectsParent);
            newMarker.up = groundHit.normal;
        }
        else
        {
            Instantiate(deathMarker, tankOrigin.position + tankOrigin.up * deathMarker.localPosition.y, Quaternion.Euler(new Vector3(tankOrigin.eulerAngles.x, 45, tankOrigin.eulerAngles.z)), effectsParent);
        }

        if (player)
        {
            if (!playerControl.godMode)
            {
                tankOrigin.GetComponent<Collider>().enabled = false;
                body.gameObject.SetActive(false);
                turret.gameObject.SetActive(false);
                barrel.gameObject.SetActive(false);

                playerControl.Dead = true;
                playerControl.OnDeath();

                if (PhotonNetwork.OfflineMode)
                {
                    GameManager.Instance.frozen = true;
                }
            }
        }
        else
        {
            Transform trackMarks = tankOrigin.Find("TrackMarks");

            if (trackMarks != null)
            {
                trackMarks.parent = effectsParent;
                Destroy(trackMarks.GetComponent<TrailEmitter>());
            }

            if (transform.CompareTag("AI Tank"))
            {
                GeneticAlgorithmBot bot = GetComponent<GeneticAlgorithmBot>();
                bot.Dead = true;
                transform.SetParent(null);

                tankOrigin.GetComponent<Collider>().enabled = false;
                body.gameObject.SetActive(false);
                turret.gameObject.SetActive(false);
                barrel.gameObject.SetActive(false);
            }
            else
            {
                if (!GameManager.Instance.inLobby && GameManager.Instance.autoPlay)
                {
                    if (transform.root.TryGetComponent<TankManager>(out var tankManager))
                    {
                        tankManager.RespawnTank(tankOrigin);
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
                else
                {
                    transform.SetParent(null);
                    if (transform.root.TryGetComponent<TankManager>(out var tankManager))
                    {
                        tankManager.StartCheckTankCount();
                    }
                    Destroy(gameObject);
                }
            }
        }
    }

    public void AvoidMine(Transform mine, float angle = 90)
    {
        stationary = false;
        Vector3 mineDirection = mine.position - transform.position;
        mineDirection.y = tankOrigin.forward.y;
        Vector3 clockwise = Quaternion.AngleAxis(angle, turret.up) * mineDirection;
        Vector3 counterClockwise = Quaternion.AngleAxis(-angle, turret.up) * mineDirection;

        bool oppositeHit = Physics.Raycast(body.position, -mineDirection, 5, barrierLayers);
        bool clockwiseHit = Physics.Raycast(body.position, clockwise, 5, barrierLayers);
        bool counterClockwiseHit = Physics.Raycast(body.position, counterClockwise, 5, barrierLayers);

        if (!oppositeHit)
        {
            targetTankDir = -mineDirection;
        }
        else
        {
            if (!clockwiseHit && !counterClockwiseHit)
            {
                if (Mathf.Abs(Vector3.SignedAngle(tankOrigin.forward, clockwise, transform.up)) <= Mathf.Abs(Vector3.SignedAngle(tankOrigin.forward, counterClockwise, transform.up)))
                {
                    targetTankDir = clockwise;
                }
                else
                {
                    targetTankDir = counterClockwise;
                }
            }
            else if (!clockwiseHit)
            {
                targetTankDir = clockwise;
            }
            else if (!counterClockwiseHit)
            {
                targetTankDir = counterClockwise;
            }
        }
    }

    public void AvoidBullet(Transform bullet, float angle = 90)
    {
        stationary = false;
        Vector3 otherForward = bullet.forward;
        otherForward.y = tankOrigin.forward.y;
        Vector3 clockwise = Quaternion.AngleAxis(angle, tankOrigin.up) * otherForward;
        Vector3 counterClockwise = Quaternion.AngleAxis(-angle, tankOrigin.up) * otherForward;
        Debug.DrawLine(tankOrigin.position, tankOrigin.position + clockwise, Color.green, 1);
        Debug.DrawLine(tankOrigin.position, tankOrigin.position + counterClockwise, Color.green, 1);

        if (CustomMath.SqrDistance(tankOrigin.position + clockwise, bullet.position) >= CustomMath.SqrDistance(tankOrigin.position + counterClockwise, bullet.position))
        {
            targetTankDir = clockwise;
        }
        else
        {
            targetTankDir = counterClockwise;
        }
    }

    public void RotateTankToVector(Vector3 to, bool master = false)
    {
        if (!overrideRotation || master)
        {
            float angle = Mathf.Abs(Vector3.SignedAngle(tankOrigin.forward, to, tankOrigin.up));

            if (angle > 180 - flipAngleThreshold && angle < 180 + flipAngleThreshold)
            {
                FlipTank();
            }
            else
            {
                tankOrigin.rotation = Quaternion.RotateTowards(tankOrigin.rotation, Quaternion.LookRotation(to), Time.deltaTime * tankRotSpeed);
            }

            Debug.DrawLine(tankOrigin.position, tankOrigin.position + to * 2, Color.blue, 0.1f);
        }
    }

    public void FlipTank()
    {
        if (IsGrounded())
        {
            tankOrigin.forward = -tankOrigin.forward;
            body.forward = -body.forward;
            turret.forward = -turret.forward;
            barrel.forward = -barrel.forward;
            trackMarks.forward = -trackMarks.forward;
        }
    }

    public bool IsGrounded()
    {
        return Physics.Raycast(tankCollider.bounds.center, -tankOrigin.up, colliderExtentsY + 0.01f, slopeLayers);
    }

    [PunRPC]
    public void ReactivateTank()
    {
        disabled = false;
        tankOrigin.GetComponent<CapsuleCollider>().enabled = true;

        tankOrigin.Find("Body").gameObject.SetActive(true);
        tankOrigin.Find("Turret").gameObject.SetActive(true);
        tankOrigin.Find("Barrel").gameObject.SetActive(true);
    }
}
