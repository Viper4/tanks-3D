using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float sensitivity = 15;
    [SerializeField] Transform target;
    public Transform reticle;

    PlayerControl playerControl;

    Transform body;
    Transform turret;
    Transform barrel;

    [SerializeField] float dstFromTarget = 4;

    [SerializeField] Vector2 pitchMinMaxN = new Vector2(-40, 80);
    [SerializeField] Vector2 pitchMinMaxL = new Vector2(-20, 20);

    Vector2 pitchMinMax = new Vector2(-40, 80);

    [SerializeField] float rotationSmoothTime = 0.1f;
    Vector3 rotationSmoothVelocity;
    Vector3 currentRotation;

    [SerializeField] LayerMask ignoreLayerMasks;

    Vector3 lastEulerAngles;

    float yaw;
    float pitch;
    bool lockTurret = false;

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

        lastEulerAngles = transform.parent.eulerAngles;

        playerControl = transform.parent.GetComponent<PlayerControl>();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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
        if (!playerControl.Dead && Time.timeScale != 0)
        {
            reticle.gameObject.SetActive(true);
            Cursor.visible = false;
            if (dstFromTarget == 0)
            {
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

                    if (Physics.Raycast(mouseRay, out RaycastHit mouseHit, Mathf.Infinity, ~ignoreLayerMasks))
                    {
                        // Rotating turret and barrel towards the mouseHit point
                        Vector3 dir = Vector3.RotateTowards(target.forward, mouseHit.point - target.position, Time.deltaTime * 3, 0);
                        Quaternion lookRotation = Quaternion.LookRotation(dir);

                        turret.rotation = barrel.rotation = target.rotation = lookRotation;
                    }
                    reticle.position = Input.mousePosition;
                }
                else
                {
                    if (Physics.Raycast(target.position, barrel.forward, out RaycastHit barrelHit, Mathf.Infinity, ~ignoreLayerMasks))
                    {
                        reticle.position = Camera.main.WorldToScreenPoint(barrelHit.point);
                    }
                }
            }

            // Correcting turret and barrel y rotation to not depend on the parent
            turret.eulerAngles = barrel.eulerAngles = new Vector3(barrel.eulerAngles.x, barrel.eulerAngles.y + (lastEulerAngles.y - transform.parent.eulerAngles.y), barrel.eulerAngles.z);

            // Locking turret x euler to 0 deg, and clamping barrel x euler between pitchMinMaxL
            turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y, turret.localEulerAngles.z);
            barrel.localEulerAngles = new Vector3(Clamping.ClampAngle(barrel.localEulerAngles.x, pitchMinMaxL.x, pitchMinMaxL.y), barrel.localEulerAngles.y, barrel.localEulerAngles.z);
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
        if (Physics.Raycast(target.position, (transform.position - target.position).normalized, out RaycastHit clippingHit, dstFromTarget, ~ignoreLayerMasks))
        {
            transform.position = clippingHit.point - (transform.position - target.position).normalized;
        }
        lastEulerAngles = transform.parent.eulerAngles;
    }
}
