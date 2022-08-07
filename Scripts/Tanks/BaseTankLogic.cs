using UnityEngine;
using Photon.Pun;
using MyUnityAddons.Math;
using MyUnityAddons.CustomPhoton;

public class BaseTankLogic : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private bool player = false;
    [SerializeField] private PlayerControl playerControl;

    [Header("Death")]
    [SerializeField] private Transform explosionEffect;
    [SerializeField] private Transform deathMarker;
    [SerializeField] Transform effectsParent;

    [Header("Slope Settings")]
    public LayerMask barrierLayers;
    [SerializeField] private LayerMask slopeLayers;
    [SerializeField] private bool slopeAlignment = true;
    [SerializeField] private float alignRotationSpeed = 20;

    [Header("Tank Movement")]
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
    [SerializeField] private float[] turretRangeX = { -20, 20 };
    [HideInInspector] public bool overrideRotation = false;

    private float tankRotSeed = 0;
    private float turretRotSeed = 0;

    public Vector3 targetTurretDir;
    public Vector3 targetTankDir;
    Vector3 lastEulerAngles;

    [SerializeField] private Transform tankOrigin;
    //Rigidbody RB;
    Transform body;
    Transform turret;
    Transform barrel;
    Rigidbody RB;

    private void Start() 
    {
        tankRotSeed = Random.Range(-999.0f, 999.0f);
        turretRotSeed = Random.Range(-999.0f, 999.0f);
        
        body = tankOrigin.Find("Body");
        turret = tankOrigin.Find("Turret");
        barrel = tankOrigin.Find("Barrel");
        RB = tankOrigin.GetComponent<Rigidbody>();
        lastEulerAngles = tankOrigin.eulerAngles;
        speed = normalSpeed;
        targetTankDir = tankOrigin.forward;
        if (GameManager.autoPlay)
        {
            effectsParent = GameObject.Find("ToClear").transform;
        }
    }

    private void Update()
    {
        if (!GameManager.frozen && Time.timeScale != 0)
        {
            if (slopeAlignment)
            {
                if (Physics.Raycast(tankOrigin.position, Vector3.down, out RaycastHit hit, 1, slopeLayers))
                {
                    // Rotating to align with slope
                    Quaternion alignedRotation = Quaternion.FromToRotation(tankOrigin.up, hit.normal);
                    tankOrigin.rotation = Quaternion.Slerp(tankOrigin.rotation, alignedRotation * tankOrigin.rotation, alignRotationSpeed * Time.deltaTime);
                }
            }

            if (restrictRotation)
            {
                // Ensuring tank doesn't flip over
                tankOrigin.eulerAngles = new Vector3(CustomMath.ClampAngle(tankOrigin.eulerAngles.x, pitchRange[0], pitchRange[1]), tankOrigin.eulerAngles.y, CustomMath.ClampAngle(tankOrigin.eulerAngles.z, rollRange[0], rollRange[1]));
            }

            Vector3 velocityDirection = Vector3.zero;
            if (!player)
            {
                if (!stationary)
                {
                    if (obstacleAvoidance)
                    {
                        float normalAngle = 181;
                        bool[] pathClear = { true, true };
                        if (Physics.Raycast(body.position, body.forward, out RaycastHit forwardHit, 3, barrierLayers))
                        {
                            normalAngle = Vector3.SignedAngle(body.forward, forwardHit.normal, body.up);
                            Debug.DrawLine(body.position, forwardHit.point, Color.red, 0.1f);
                        }
                        else if (Physics.Raycast(body.position - body.right * 0.75f, body.forward, out RaycastHit forwardHitL, 3, barrierLayers))
                        {
                            normalAngle = Vector3.SignedAngle(body.forward, forwardHitL.normal, body.up);
                            Debug.DrawLine(body.position - body.right * 0.75f, forwardHitL.point, Color.red, 0.1f);
                        }
                        else if (Physics.Raycast(body.position + body.right * 0.75f, body.forward, out RaycastHit forwardHitR, 3, barrierLayers))
                        {
                            normalAngle = Vector3.SignedAngle(body.forward, forwardHitR.normal, body.up);
                            Debug.DrawLine(body.position + body.right * 0.75f, forwardHitR.point, Color.red, 0.1f);
                        }

                        if (normalAngle != 181)
                        {
                            speed = avoidSpeed;

                            if (Physics.Raycast(body.position, -body.right, out RaycastHit leftHit, 2.5f, barrierLayers))
                            {
                                pathClear[0] = false;
                                Debug.DrawLine(body.position, leftHit.point, Color.red, 0.1f);
                            }
                            else if (Physics.Raycast(body.position + body.forward, -body.right, out RaycastHit leftHitF, 2.5f, barrierLayers))
                            {
                                pathClear[0] = false;
                                Debug.DrawLine(body.position + body.forward, leftHitF.point, Color.red, 0.1f);
                            }
                            else if (Physics.Raycast(body.position - body.forward, -body.right, out RaycastHit leftHitB, 2.5f, barrierLayers))
                            {
                                pathClear[0] = false;
                                Debug.DrawLine(body.position - body.forward, leftHitB.point, Color.red, 0.1f);
                            }
                            if (Physics.Raycast(body.position, body.right, out RaycastHit rightHit, 2.5f, barrierLayers))
                            {
                                pathClear[1] = false;
                                Debug.DrawLine(body.position, rightHit.point, Color.red, 0.1f);
                            }
                            else if (Physics.Raycast(body.position + body.forward, body.right, out RaycastHit rightHitF, 2.5f, barrierLayers))
                            {
                                pathClear[1] = false;
                                Debug.DrawLine(body.position + body.forward, rightHitF.point, Color.red, 0.1f);
                            }
                            else if (Physics.Raycast(body.position - body.forward, body.right, out RaycastHit rightHitB, 2.5f, barrierLayers))
                            {
                                pathClear[1] = false;
                                Debug.DrawLine(body.position - body.forward, rightHitB.point, Color.red, 0.1f);
                            }

                            if (pathClear[0])
                            {
                                if (pathClear[1])
                                {
                                    if (normalAngle < 0)
                                    {
                                        targetTankDir = -tankOrigin.right;
                                    }
                                    else
                                    {
                                        targetTankDir = tankOrigin.right;
                                    }
                                }
                                else
                                {
                                    targetTankDir = -tankOrigin.right;
                                }
                            }
                            else if (pathClear[1])
                            {
                                targetTankDir = tankOrigin.right;
                            }
                            else
                            {
                                targetTankDir = -tankOrigin.forward;
                            }
                        }
                        else
                        {
                            speed = normalSpeed;
                        }
                    }

                    // Tank rotation
                    float noise = 0;
                    if (tankNoise)
                    {
                        // Adding noise to rotation
                        noise = tankRotNoiseScale * (Mathf.PerlinNoise(tankRotSeed + Time.time * tankRotNoiseSpeed, tankRotSeed + 1 + Time.time * tankRotNoiseSpeed) - 0.5f);
                    }

                    RotateTankToVector(Quaternion.AngleAxis(noise, tankOrigin.up) * targetTankDir);
                }

                // Tank movement
                if (RB != null)
                {
                    velocityDirection = transform.forward;
                    if (Physics.Raycast(transform.position, -transform.up, out RaycastHit middleHit, 1) && Physics.Raycast(transform.position + transform.forward, -transform.up, out RaycastHit frontHit, 1))
                    {
                        velocityDirection = frontHit.point - middleHit.point;
                    }

                    velocityY = !IsGrounded() && useGravity ? velocityY - Time.deltaTime * gravity : 0;
                    velocityY = Mathf.Clamp(velocityY, -velocityLimit, velocityLimit);

                    RB.velocity = stationary ? Vector3.up * velocityY : velocityDirection * speed + Vector3.up * velocityY;
                }

                if (turretRotation)
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

                    // Zeroing x and z eulers of turret and clamping barrel x euler
                    turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y, 0);
                    barrel.localEulerAngles = new Vector3(CustomMath.ClampAngle(barrel.localEulerAngles.x, turretRangeX[0], turretRangeX[1]), barrel.localEulerAngles.y, 0);

                    lastEulerAngles = tankOrigin.eulerAngles;
                }                
            }
        }
        else
        {
            RB.velocity = Vector3.zero;
        }
    }

    [PunRPC]
    public void ExplodeTank()
    {
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

                Instantiate(explosionEffect, tankOrigin.position, Quaternion.identity, effectsParent);
                Instantiate(deathMarker, tankOrigin.position + tankOrigin.up * deathMarker.localPosition.y, Quaternion.Euler(new Vector3(tankOrigin.eulerAngles.x, 45, tankOrigin.eulerAngles.z)), effectsParent);
                if (PhotonNetwork.OfflineMode)
                {
                    GameManager.frozen = true;
                }
            }
        }
        else
        {
            Instantiate(explosionEffect, tankOrigin.position, Quaternion.identity, effectsParent);
            if (Physics.Raycast(tankOrigin.position, Vector3.down, out RaycastHit groundHit, Mathf.Infinity, slopeLayers))
            {
                Instantiate(deathMarker, groundHit.point + groundHit.normal * deathMarker.localPosition.y, Quaternion.FromToRotation(Vector3.up, groundHit.normal), effectsParent);
            }
            else
            {
                Instantiate(deathMarker, tankOrigin.position + tankOrigin.up * deathMarker.localPosition.y, Quaternion.Euler(new Vector3(tankOrigin.eulerAngles.x, 45, tankOrigin.eulerAngles.z)), effectsParent);
            }

            Transform trackMarks = tankOrigin.Find("TrackMarks");

            if (trackMarks != null)
            {
                trackMarks.parent = null;
                Destroy(trackMarks.GetComponent<TrailEmitter>());
            }

            if (PhotonNetwork.OfflineMode)
            {
                transform.root.GetComponent<TankManager>().StartCheckTankCount(null);
            }
            else
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    transform.root.GetComponent<TankManager>().StartCheckTankCount(PhotonNetwork.MasterClient.FindPhotonView());
                }
            }

            Destroy(gameObject);
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
        }
    }

    public bool IsGrounded()
    {
        return Physics.Raycast(tankOrigin.position + Vector3.up * 0.05f, -tankOrigin.up, 0.1f, slopeLayers);
    }
}
