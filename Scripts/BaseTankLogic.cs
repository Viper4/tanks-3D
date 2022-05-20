using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseTankLogic : MonoBehaviour
{
    [SerializeField] Transform tankOrigin;
    [SerializeField] Transform explosionEffect;

    [SerializeField] float[] pitchRange = { -45, 45 };
    [SerializeField] float[] rollRange = { -45, 45 };

    [SerializeField] bool frozenRotation = true;
    [SerializeField] float alignRotationSpeed = 20;
    [SerializeField] LayerMask notSlopeLayerMask;
    
    [SerializeField] float tankRotSpeed = 250f;
    
    public bool noisyRotation
    [SerializeField] bool randomizeSeed = true;
    [SerializeField] float tankRotNoiseScale = 5;
    [SerializeField] float tankRotNoiseSpeed = 0.5f;
    [SerializeField] float tankRotSeed = 0;
    
    RigidBody rb;

    private void Awake() 
    {
        if (randomizeSeed) 
        {
            tankRotSeed = Random.Range(-99.0f, 99.0f);
        }
        
        rb = GetComponent<RigidBody>();
    }
    
    private void Update()
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
    
    public void RotateTo(Vector3 desiredDir)
    {
        float angle = Vector3.Angle(desiredDir, tankOrigin.forward);
        angle = angle < 0 ? angle + 360 : angle;

        if (angle > 170 && angle < 190)
        {
            tankOrigin.forward = -tankOrigin.forward;
        }
        else
        {
            rb.MoveRotation(Quaternion.RotateTowards(tankOrigin.rotation, Quaternion.LookRotation(desiredDir), Time.deltaTime * tankRotSpeed));
        }
    }
    
    public void Explode()
    {
        Instantiate(explosionEffect, tankOrigin.position, Quaternion.Euler(-90, 0, 0));

        if (transform.name == "Player")
        {
            PlayerControl playerControl = GetComponent<PlayerControl>();

            playerControl.Dead = true;
            playerControl.lives--;
            playerControl.deaths++;

            tankOrigin.gameObject.SetActive(false);

            playerControl.Respawn();
        }
        else
        {
            if (transform.root.childCount == 1)
            {
                SceneLoader.sceneLoader.LoadNextScene(3);
            }

            Destroy(gameObject);
        }
    }

    public bool IsGrounded()
    {
        return Physics.Raycast(tankOrigin.position + Vector3.up * 0.05f, -tankOrigin.up, 0.1f, ~LayerMask.NameToLayer("Tank"));
    }
}
