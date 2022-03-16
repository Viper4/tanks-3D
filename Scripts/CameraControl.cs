using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float sensitivity = 15;
    public Transform target;
    public Transform reticle;

    PlayerControl playerControl;

    Transform body;
    Transform turret;
    Transform barrel;

    public float dstFromTarget = 4;

    public Vector2 pitchMinMaxN = new Vector2(-40, 80);
    public Vector2 pitchMinMaxL = new Vector2(-20, 20);

    Vector2 pitchMinMax = new Vector2(-40, 80);

    public float rotationSmoothTime = 0.1f;
    Vector3 rotationSmoothVelocity;
    Vector3 currentRotation;

    public LayerMask ignoreLayerMasks;

    float yaw;
    float pitch;
    bool lockTurret = false;

    public bool dead { get; set; } = false;

    // Start is called before the first frame update
    void Awake()
    {
        // If target is not set, automatically set it to the parent
        if (target == null)
        {
            target = transform.parent;
        }

        body = transform.parent.Find("Body");
        turret = transform.parent.Find("Turret");
        barrel = transform.parent.Find("Barrel");

        playerControl = transform.parent.GetComponent<PlayerControl>();
    }

    // Update is called once per frame
    void Update()
    {
        float zoomRate = Input.GetKey(KeyCode.LeftShift) ? 0.5f : 5f;
        // Zoom with scroll
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
        {
            dstFromTarget = Mathf.Clamp(dstFromTarget - zoomRate, 0, 30);
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
        {
            dstFromTarget = Mathf.Clamp(dstFromTarget + zoomRate, 0, 30);
        }

        // Lock turret toggle
        if (Input.GetKeyDown(playerControl.keyBinds["Lock Turret"]))
        {
            lockTurret = !lockTurret;
        }

        // 1st person vs. 3rd person turret control
        if (!dead && Time.timeScale != 0)
        {
            reticle.gameObject.SetActive(true);
            Cursor.visible = false;
            if (dstFromTarget == 0)
            {
                barrel.rotation = turret.rotation = target.rotation = Camera.main.transform.rotation;
                barrel.rotation *= Quaternion.Euler(-90, 0, 0);

                body.gameObject.GetComponent<MeshRenderer>().enabled = turret.gameObject.GetComponent<MeshRenderer>().enabled = barrel.gameObject.GetComponent<MeshRenderer>().enabled = false;
                reticle.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
                
                Cursor.lockState = CursorLockMode.Locked;
                pitchMinMax = pitchMinMaxL;
            }
            else
            {
                body.gameObject.GetComponent<MeshRenderer>().enabled = turret.gameObject.GetComponent<MeshRenderer>().enabled = barrel.gameObject.GetComponent<MeshRenderer>().enabled = true;

                Cursor.lockState = CursorLockMode.Confined;
                pitchMinMax = pitchMinMaxN;

                if (!lockTurret)
                {
                    Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

                    RaycastHit mouseHit;
                    int playerLayerMask = 1 << 7;
                    if (Physics.Raycast(mouseRay, out mouseHit, Mathf.Infinity, ~playerLayerMask))
                    {
                        // Rotating turret and barrel towards the mouseHit point
                        Vector3 dir = Vector3.RotateTowards(target.forward, mouseHit.point - target.position, Time.deltaTime * 3, 0);
                        Quaternion lookRotation = Quaternion.LookRotation(dir);

                        turret.rotation = barrel.rotation = target.rotation = lookRotation;
                        barrel.rotation *= Quaternion.Euler(-90, 0, 0);
                    }
                    reticle.position = Input.mousePosition;
                }
                else
                {
                    RaycastHit turretHit;
                    if(Physics.Raycast(target.position, turret.forward, out turretHit, Mathf.Infinity, ~ignoreLayerMasks))
                    {
                        reticle.position = Camera.main.WorldToScreenPoint(turretHit.point);
                    }
                }
            }

            // Locking turret x euler to -90 deg, and clamping barrel x euler between pitchMinMaxL
            turret.eulerAngles = new Vector3(-90, turret.eulerAngles.y, turret.eulerAngles.z);
            barrel.eulerAngles = new Vector3(Clamping.ClampAngle(barrel.eulerAngles.x, -90 + pitchMinMaxL.x, -90 + pitchMinMaxL.y), barrel.eulerAngles.y, barrel.eulerAngles.z);
        }
        else
        {
            reticle.position = Input.mousePosition;
        }
        // Translating inputs from mouse into smoothed rotation of camera
        yaw += Input.GetAxis("Mouse X") * sensitivity / 8;
        pitch -= Input.GetAxis("Mouse Y") * sensitivity / 8;
        pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);

        currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref rotationSmoothVelocity, rotationSmoothTime);
        // Setting rotation and position of camera on previous params and target and dstFromTarget
        transform.eulerAngles = currentRotation;

        transform.position = target.position - transform.forward * dstFromTarget;

        // Prevent clipping of camera
        RaycastHit clippingHit;
        if (Physics.Raycast(target.position, (transform.position - target.position).normalized, out clippingHit, dstFromTarget, ~ignoreLayerMasks))
        {
            if(clippingHit.transform.name != "Player")
            {
                transform.position = clippingHit.point - (transform.position - target.position).normalized;
            }
        }
    }
}
