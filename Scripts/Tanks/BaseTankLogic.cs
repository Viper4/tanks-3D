using UnityEngine;
using Photon.Pun;
using MyUnityAddons.Math;

public class BaseTankLogic : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private bool player = false;
    [SerializeField] private PlayerControl playerControl;

    [Header("Death")]
    [SerializeField] private Transform explosionEffect;
    [SerializeField] private Transform deathMarker;

    [Header("Slope Settings")]
    public LayerMask barrierLayers;
    [SerializeField] private LayerMask nonSlopeLayers;
    [SerializeField] private bool slopeAlignment = true;
    [SerializeField] private float alignRotationSpeed = 20;

    [Header("Tank Rotation")]
    public float flipAngleThreshold = 20;
    [SerializeField] private bool restrictRotation = false;
    [SerializeField] private float[] pitchRange = { -45, 45 };
    [SerializeField] private float[] rollRange = { -45, 45 };

    [Header("Tank Rotation Noise")]
    public bool noisyRotation;
    [SerializeField] private float tankRotSpeed = 250f;
    [SerializeField] private float tankRotNoiseScale = 5;
    [SerializeField] private float tankRotNoiseSpeed = 0.5f;

    [Header("Turret Rotation")]
    [SerializeField] private float turretRotSpeed = 35f;
    [SerializeField] private Vector2 inaccuracy = new Vector2(8, 35);
    [SerializeField] private float turretNoiseSpeed = 0.15f;
    [SerializeField] private float[] turretRangeX = { -20, 20 };

    private float tankRotSeed = 0;
    private float turretRotSeed = 0;

    Quaternion turretAnchor;
    Vector3 lastEulerAngles;

    [SerializeField] private Transform tankOrigin;
    Rigidbody RB;
    Transform body;
    Transform turret;
    Transform barrel;

    private void Awake() 
    {
        tankRotSeed = Random.Range(-999.0f, 999.0f);
        turretRotSeed = Random.Range(-999.0f, 999.0f);
        
        RB = GetComponent<Rigidbody>();

        body = tankOrigin.Find("Body");
        turret = tankOrigin.Find("Turret");
        barrel = tankOrigin.Find("Barrel");
        turretAnchor = turret.rotation;
        lastEulerAngles = tankOrigin.eulerAngles;
    }

    private void Update()
    {
        if (!GameManager.frozen && Time.timeScale != 0)
        {
            if (slopeAlignment)
            {
                if (Physics.Raycast(tankOrigin.position, Vector3.down, out RaycastHit hit, 1, ~nonSlopeLayers))
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

            if (noisyRotation && !player)
            {
                // Adding noise to rotation
                float noise = tankRotNoiseScale * (Mathf.PerlinNoise(tankRotSeed + Time.time * tankRotNoiseSpeed, tankRotSeed + 1 + Time.time * tankRotNoiseSpeed) - 0.5f);
                Quaternion desiredTankRot = Quaternion.LookRotation(Quaternion.AngleAxis(noise, Vector3.up) * transform.forward);
                RB.MoveRotation(Quaternion.RotateTowards(transform.rotation, desiredTankRot, Time.deltaTime * tankRotSpeed));
            }

            if (!player)
            {
                // Inaccuracy to rotation with noise
                float noiseX = inaccuracy.x * (Mathf.PerlinNoise(turretRotSeed + Time.time * turretNoiseSpeed, turretRotSeed + 1f + Time.time * turretNoiseSpeed) - 0.5f);
                float noiseY = inaccuracy.y * (Mathf.PerlinNoise(turretRotSeed + 4f + Time.time * turretNoiseSpeed, turretRotSeed + 5f + Time.time * turretNoiseSpeed) - 0.5f);

                // Correcting turret and barrel y rotation to not depend on the parent
                turret.eulerAngles = new Vector3(turret.eulerAngles.x, turret.eulerAngles.y + lastEulerAngles.y - transform.eulerAngles.y, turret.eulerAngles.z);
                barrel.eulerAngles = new Vector3(barrel.eulerAngles.x, barrel.eulerAngles.y + lastEulerAngles.y - transform.eulerAngles.y, barrel.eulerAngles.z);

                // Zeroing x and z eulers of turret and clamping barrel x euler
                turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y + noiseY, 0);
                barrel.localEulerAngles = new Vector3(CustomMath.ClampAngle(barrel.localEulerAngles.x + noiseX, turretRangeX[0], turretRangeX[1]), barrel.localEulerAngles.y + noiseY, 0);

                lastEulerAngles = tankOrigin.eulerAngles;
            }
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
                playerControl.Respawn();

                Instantiate(explosionEffect, tankOrigin.position, Quaternion.identity);
                Instantiate(deathMarker, tankOrigin.position + tankOrigin.up * 0.05f, Quaternion.Euler(new Vector3(tankOrigin.eulerAngles.x, 45, tankOrigin.eulerAngles.z)));
                if (PhotonNetwork.OfflineMode)
                {
                    GameManager.frozen = true;
                }
            }
        }
        else
        {
            Instantiate(explosionEffect, tankOrigin.position, Quaternion.identity);
            Instantiate(deathMarker, tankOrigin.position + tankOrigin.up * 0.05f, Quaternion.Euler(new Vector3(tankOrigin.eulerAngles.x, 45, tankOrigin.eulerAngles.z)));

            Transform trackMarks = tankOrigin.Find("TrackMarks");

            if (trackMarks != null)
            {
                trackMarks.parent = null;
                Destroy(trackMarks.GetComponent<TrailEmitter>());
            }

            transform.root.GetComponent<TankManager>().StartCheckTankCount();
            Destroy(gameObject);
        }
    }

    public void RotateTankToVector(Vector3 to)
    {
        float angle = Vector3.Angle(tankOrigin.forward, to);
        angle = angle < 0 ? angle + 360 : angle;

        if (angle > 180 - flipAngleThreshold && angle < 180 + flipAngleThreshold)
        {
            FlipTank();
        }
        else
        {
            RB.MoveRotation(Quaternion.RotateTowards(tankOrigin.rotation, Quaternion.LookRotation(to), Time.deltaTime * tankRotSpeed * 2));
        }
    }

    public void FlipTank()
    {
        if (IsGrounded())
        {
            tankOrigin.forward = -tankOrigin.forward;
        }
    }

    public void RotateTurretTo(Quaternion rotation)
    {
        // Rotating turret and barrel towards vector
        turret.rotation = barrel.rotation = turretAnchor = Quaternion.RotateTowards(turretAnchor, rotation, Time.deltaTime * turretRotSpeed);
    }

    public void RotateTurretTo(Vector3 direction)
    {
        // Rotating turret and barrel towards vector
        Quaternion targetRotation = Quaternion.LookRotation(direction, turret.up);
        turret.rotation = barrel.rotation = turretAnchor = Quaternion.RotateTowards(turretAnchor, targetRotation, Time.deltaTime * turretRotSpeed);
    }

    public void ObstacleAvoidance(RaycastHit forwardHit, float maxDistance, LayerMask barrierLayers)
    {
        Debug.DrawLine(tankOrigin.position, forwardHit.point, Color.red, 0.1f);

        bool[] pathClear = { true, true };
        Vector3 desiredDir;

        // Cross product doesn't give absolute value of angle
        float dotProductY = Vector3.Dot(Vector3.Cross(transform.forward, forwardHit.normal), transform.up);

        // Checking Left
        if (Physics.Raycast(tankOrigin.position, -transform.right, maxDistance, barrierLayers))
        {
            pathClear[0] = false;
            Debug.DrawLine(tankOrigin.position, tankOrigin.position - transform.right * 2, Color.red, 0.1f);
        }
        // Checking Right
        if (Physics.Raycast(tankOrigin.position, transform.right, maxDistance, barrierLayers))
        {
            pathClear[1] = false;
            Debug.DrawLine(tankOrigin.position, tankOrigin.position + transform.right * 2, Color.red, 0.1f);
        }

        if (pathClear[0])
        {
            if (pathClear[1])
            {
                // Rotate left if obstacle is facing left, vice versa
                desiredDir = dotProductY < 0 ? -transform.right : transform.right;
            }
            else
            {
                // Rotate left
                desiredDir = -transform.right;
            }
        }
        else if (pathClear[1])
        {
            // Rotate right
            desiredDir = transform.right;
        }
        else
        {
            // Go backward
            desiredDir = -transform.forward;
        }

        // Applying rotation
        RotateTankToVector(desiredDir);
    }

    public bool IsGrounded()
    {
        return Physics.Raycast(tankOrigin.position + Vector3.up * 0.05f, -tankOrigin.up, 0.1f, ~nonSlopeLayers);
    }
}
