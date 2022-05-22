using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseTankLogic : MonoBehaviour
{
    [SerializeField] Transform tankOrigin;
    [SerializeField] Transform explosionEffect;
    [SerializeField] Transform deathMarker;

    [SerializeField] float[] pitchRange = { -45, 45 };
    [SerializeField] float[] rollRange = { -45, 45 };

    [SerializeField] bool frozenRotation = true;
    [SerializeField] float alignRotationSpeed = 20;
    [SerializeField] LayerMask notSlopeLayerMask;
    
    [SerializeField] float tankRotSpeed = 250f;

    public bool noisyRotation;
    [SerializeField] bool randomizeSeed = true;
    [SerializeField] float tankRotNoiseScale = 5;
    [SerializeField] float tankRotNoiseSpeed = 0.5f;
    [SerializeField] float tankRotSeed = 0;

    public float flipAngleThreshold = 20;

    Rigidbody rb;

    private void Awake() 
    {
        if (randomizeSeed) 
        {
            tankRotSeed = Random.Range(-99.0f, 99.0f);
        }
        
        rb = GetComponent<Rigidbody>();
    }
    
    private void Update()
    {
        if (!SceneLoader.frozen)
        {
            if (frozenRotation)
            {
                if (Physics.Raycast(tankOrigin.position, Vector3.down, out RaycastHit hit, 1, ~notSlopeLayerMask))
                {
                    // Rotating to align with slope
                    Quaternion alignedRotation = Quaternion.FromToRotation(tankOrigin.up, hit.normal);
                    tankOrigin.rotation = Quaternion.Slerp(tankOrigin.rotation, alignedRotation * tankOrigin.rotation, alignRotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                // Ensuring tank doesn't flip over
                tankOrigin.eulerAngles = new Vector3(Clamping.ClampAngle(tankOrigin.eulerAngles.x, pitchRange[0], pitchRange[1]), tankOrigin.eulerAngles.y, Clamping.ClampAngle(tankOrigin.eulerAngles.z, rollRange[0], rollRange[1]));
            }

            if (noisyRotation)
            {
                // Adding noise to rotation
                float noise = tankRotNoiseScale * (Mathf.PerlinNoise(tankRotSeed + Time.time * tankRotNoiseSpeed, (tankRotSeed + 1) + Time.time * tankRotNoiseSpeed) - 0.5f);
                Quaternion desiredTankRot = Quaternion.LookRotation(Quaternion.AngleAxis(noise, Vector3.up) * transform.forward);
                rb.rotation = Quaternion.RotateTowards(transform.rotation, desiredTankRot, Time.deltaTime * tankRotSpeed);
            }
        }
    }
    
    public void RotateToVector(Vector3 to)
    {
        float angle = Vector3.Angle(tankOrigin.forward, to);
        angle = angle < 0 ? angle + 360 : angle;

        if (angle > 180 - flipAngleThreshold && angle < 180 + flipAngleThreshold)
        {
            tankOrigin.forward = -tankOrigin.forward;
        }
        else
        {
            rb.MoveRotation(Quaternion.RotateTowards(tankOrigin.rotation, Quaternion.LookRotation(to), Time.deltaTime * tankRotSpeed * 2));
        }
    }

    public void Explode()
    {
        Instantiate(explosionEffect, tankOrigin.position, Quaternion.Euler(-90, 0, 0));
        Instantiate(deathMarker, tankOrigin.position + tankOrigin.up * 0.05f, Quaternion.Euler(new Vector3(tankOrigin.eulerAngles.x, 45, tankOrigin.eulerAngles.z)));

        if (transform.name == "Player")
        {
            SceneLoader.frozen = true;

            PlayerControl playerControl = GetComponent<PlayerControl>();

            playerControl.Dead = true;
            playerControl.lives--;
            playerControl.deaths++;

            tankOrigin.Find("Body").gameObject.SetActive(false);
            tankOrigin.Find("Turret").gameObject.SetActive(false);
            tankOrigin.Find("Barrel").gameObject.SetActive(false);

            playerControl.Respawn();
        }
        else
        {
            Transform trackMarks = tankOrigin.Find("TrackMarks");

            if (transform.root.childCount == 1)
            {
                SceneLoader.frozen = true;
                SceneLoader.sceneLoader.LoadNextScene(3);
            }

            trackMarks.parent = null;
            Destroy(trackMarks.GetComponent<TrailEmitter>());
            Destroy(gameObject);
        }
    }

    public void ObstacleAvoidance(RaycastHit forwardHit, float maxDistance)
    {
        Debug.DrawLine(tankOrigin.position, forwardHit.point, Color.red, 0.1f);

        bool[] pathClear = { true, true };
        Vector3 desiredDir;

        // Cross product doesn't give absolute value of angle
        float dotProductY = Vector3.Dot(Vector3.Cross(transform.forward, forwardHit.normal), transform.up);

        // Checking Left
        if (Physics.Raycast(tankOrigin.position, -transform.right, maxDistance))
        {
            pathClear[0] = false;
            Debug.DrawLine(tankOrigin.position, tankOrigin.position - transform.right * 2, Color.red, 0.1f);
        }
        // Checking Right
        if (Physics.Raycast(tankOrigin.position, transform.right, maxDistance))
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
        RotateToVector(desiredDir);
    }

    public bool IsGrounded()
    {
        return Physics.Raycast(tankOrigin.position + Vector3.up * 0.05f, -tankOrigin.up, 0.1f, ~LayerMask.NameToLayer("Tank"));
    }
}
