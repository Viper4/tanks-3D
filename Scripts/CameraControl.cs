using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float sensitivity = 15;
    [SerializeField] Transform target;
    public Transform reticle;

    PlayerControl playerControl;

    Transform tankOrigin;
    Transform body;
    Transform turret;
    Transform barrel;

    Vector3 lastEulerAngles;

    [SerializeField] float dstFromTarget = 4;

    [SerializeField] Vector2 pitchMinMaxN = new Vector2(-40, 80);
    [SerializeField] Vector2 pitchMinMaxL = new Vector2(-20, 20);

    Vector2 pitchMinMax = new Vector2(-40, 80);

    [SerializeField] float rotationSmoothTime = 0.1f;
    Vector3 rotationSmoothVelocity;
    Vector3 currentRotation;

    [SerializeField] LayerMask ignoreLayerMasks;

    float yaw;
    float pitch;
    bool lockTurret = false;
    bool lockCamera = false;
    bool switchCamera = false;

    // Start is called before the first frame Update
    void Awake()
    {
        // If target is not set, automatically set it to the parent
        if (target == null)
        {
            target = transform.parent;
        }

        tankOrigin = transform.parent.Find("Tank Origin");
        body = tankOrigin.Find("Body");
        turret = tankOrigin.Find("Turret");
        barrel = tankOrigin.Find("Barrel");

        playerControl = transform.parent.GetComponent<PlayerControl>();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        lastEulerAngles = tankOrigin.eulerAngles;
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
        else if (Input.GetKeyDown(playerControl.keyBinds["Lock Camera"]))
        {
            lockCamera = !lockCamera;
        }
        else if(Input.GetKeyDown(playerControl.keyBinds["Switch Camera"]))
        {
            switchCamera = !switchCamera;
        }

        if (!playerControl.Dead && Time.timeScale != 0)
        {
            // Unlinking y eulers of turret, barrel, and target from parent
            turret.eulerAngles = new Vector3(turret.eulerAngles.x, turret.eulerAngles.y + lastEulerAngles.y - tankOrigin.eulerAngles.y, turret.eulerAngles.z);
            barrel.eulerAngles = target.eulerAngles = new Vector3(barrel.eulerAngles.x, barrel.eulerAngles.y + lastEulerAngles.y - tankOrigin.eulerAngles.y, barrel.eulerAngles.z);

            reticle.gameObject.SetActive(true);
            Cursor.visible = false;
            if (switchCamera)
            {
                Cursor.lockState = CursorLockMode.Confined;
                pitchMinMax = pitchMinMaxN;

                body.gameObject.GetComponent<MeshRenderer>().enabled = turret.gameObject.GetComponent<MeshRenderer>().enabled = barrel.gameObject.GetComponent<MeshRenderer>().enabled = true;

                RotateToMousePoint();

                transform.eulerAngles = new Vector3(90, 0, 0);
                transform.position = new Vector3(0, 30 + dstFromTarget, 0);
            }
            else
            {
                if (dstFromTarget == 0)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    pitchMinMax = pitchMinMaxL;

                    turret.rotation = barrel.rotation = Camera.main.transform.rotation;
                    body.gameObject.GetComponent<MeshRenderer>().enabled = turret.gameObject.GetComponent<MeshRenderer>().enabled = barrel.gameObject.GetComponent<MeshRenderer>().enabled = false;
                    reticle.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Confined;
                    pitchMinMax = pitchMinMaxN;

                    body.gameObject.GetComponent<MeshRenderer>().enabled = turret.gameObject.GetComponent<MeshRenderer>().enabled = barrel.gameObject.GetComponent<MeshRenderer>().enabled = true;

                    if (!lockTurret)
                    {
                        RotateToMousePoint();
                    }
                    else
                    {
                        if (Physics.Raycast(target.position, barrel.forward, out RaycastHit barrelHit, Mathf.Infinity, ~ignoreLayerMasks))
                        {
                            reticle.position = Camera.main.WorldToScreenPoint(barrelHit.point);
                        }
                    }
                }
            }
            
            // Zeroing x and z eulers of turret and clamping barrel x euler
            turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y, 0);
            barrel.localEulerAngles = new Vector3(Clamping.ClampAngle(barrel.localEulerAngles.x, pitchMinMaxL.x, pitchMinMaxL.y), barrel.localEulerAngles.y, 0);

            lastEulerAngles = tankOrigin.eulerAngles;
        }
        else
        {
            reticle.position = Input.mousePosition;
        }

        if (!switchCamera)
        {
            if (!lockCamera)
            {
                // Translating inputs from mouse into smoothed rotation of camera
                yaw += Input.GetAxis("Mouse X") * sensitivity / 4;
                pitch -= Input.GetAxis("Mouse Y") * sensitivity / 4;
                pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);

                currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref rotationSmoothVelocity, rotationSmoothTime);
                // Setting rotation and position of camera on previous params and target and dstFromTarget
                transform.eulerAngles = currentRotation;
            }
            transform.position = target.position - transform.forward * dstFromTarget;

            // Prevent clipping of camera
            if (Physics.Raycast(target.position, (transform.position - target.position).normalized, out RaycastHit clippingHit, dstFromTarget, ~ignoreLayerMasks))
            {
                transform.position = clippingHit.point - (transform.position - target.position).normalized;
            }
        }
    }

    void RotateToMousePoint()
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(mouseRay, out RaycastHit mouseHit, Mathf.Infinity, ~ignoreLayerMasks))
        {
            Debug.DrawLine(transform.position, mouseHit.point, Color.red, 0.1f);
            // Rotating turret and barrel towards the mouseHit point
            Vector3 dir = Vector3.RotateTowards(target.forward, mouseHit.point - target.position, Time.deltaTime * 3, 0);
            Quaternion lookRotation = Quaternion.LookRotation(dir, tankOrigin.up);

            turret.rotation = Quaternion.AngleAxis(lookRotation.eulerAngles.y, Vector3.up);
            barrel.rotation = target.rotation = lookRotation;
        }
        reticle.position = Input.mousePosition;
    }
}
