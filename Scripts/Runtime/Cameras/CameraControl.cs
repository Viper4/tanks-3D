using UnityEngine;
using MyUnityAddons.Calculations;
using Photon.Pun;
using UnityEngine.EventSystems;

public class CameraControl : MonoBehaviour
{
    Camera thisCamera;

    [SerializeField] Transform target;
    public Transform reticle;

    [SerializeField] PlayerControl playerControl;
    [SerializeField] BaseUI baseUI;

    Transform tankOrigin;
    Transform body;
    Transform turret;
    Transform barrel;

    Quaternion lastParentRotation;

    [SerializeField] float dstFromTarget = 4;
    [SerializeField] Vector2 targetDstMinMax = new Vector2(0, 30);
    [SerializeField] Vector2 altTargetDstMinMax = new Vector2(30, 60);

    [SerializeField] Vector2 pitchMinMaxN = new Vector2(-40, 80);
    [SerializeField] Vector2 pitchMinMaxL = new Vector2(-20, 20);

    Vector2 pitchMinMax = new Vector2(-40, 80);

    [SerializeField] float rotationSmoothing = 0.05f;
    Vector3 rotationSmoothVelocity;
    Vector3 currentRotation;

    [SerializeField] LayerMask mouseIgnoreLayers;
    [SerializeField] LayerMask cameraIgnoreLayers;

    float yaw;
    float pitch;
    bool lockTurret = false;
    bool lockCamera = false;
    public bool alternateCamera = false;

    public bool invisible = false;

    // Start is called before the first frame Update
    void Start()
    {
        if(PhotonNetwork.OfflineMode || playerControl.GetComponent<PhotonView>().IsMine)
        {
            thisCamera = GetComponent<Camera>();

            // If target is not set, automatically set it to the parent
            if(target == null)
            {
                target = transform.parent;
            }

            tankOrigin = transform.parent.Find("Tank Origin");
            body = tankOrigin.Find("Body");
            turret = tankOrigin.Find("Turret");
            barrel = tankOrigin.Find("Barrel");

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            lastParentRotation = tankOrigin.localRotation;
            foreach(UsernameSystem username in FindObjectsOfType<UsernameSystem>())
            {
                username.UpdateMainCamera();
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(PhotonNetwork.OfflineMode || playerControl.GetComponent<PhotonView>().IsMine)
        {
            if (!GameManager.Instance.paused)
            {
                float zoomRate = Input.GetKey(DataManager.playerSettings.keyBinds["Zoom Control"]) ? DataManager.playerSettings.slowZoomSpeed : DataManager.playerSettings.fastZoomSpeed;
                if (alternateCamera)
                {
                    // Zoom with scroll
                    if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
                    {
                        dstFromTarget = Mathf.Clamp(dstFromTarget - zoomRate, altTargetDstMinMax.x, altTargetDstMinMax.y);
                    }
                    else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
                    {
                        dstFromTarget = Mathf.Clamp(dstFromTarget + zoomRate, altTargetDstMinMax.x, altTargetDstMinMax.y);
                    }
                }
                else
                {
                    // Zoom with scroll
                    if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
                    {
                        dstFromTarget = Mathf.Clamp(dstFromTarget - zoomRate, targetDstMinMax.x, targetDstMinMax.y);
                    }
                    else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
                    {
                        dstFromTarget = Mathf.Clamp(dstFromTarget + zoomRate, targetDstMinMax.x, targetDstMinMax.y);
                    }
                }

                // Lock turret toggle
                if (Input.GetKeyDown(DataManager.playerSettings.keyBinds["Lock Turret"]))
                {
                    lockTurret = !lockTurret;
                    baseUI.UIElements["Lock Turret"].gameObject.SetActive(lockTurret);
                }
                else if (!alternateCamera && Input.GetKeyDown(DataManager.playerSettings.keyBinds["Lock Camera"]))
                {
                    lockCamera = !lockCamera;
                    baseUI.UIElements["Lock Camera"].gameObject.SetActive(lockCamera);
                }
                else if (Input.GetKeyDown(DataManager.playerSettings.keyBinds["Switch Camera"]))
                {
                    alternateCamera = !alternateCamera;
                    lockCamera = false;

                    if (alternateCamera)
                    {
                        dstFromTarget = Mathf.Clamp(dstFromTarget, altTargetDstMinMax.x, altTargetDstMinMax.y);
                        transform.eulerAngles = new Vector3(90, 0, 0);
                    }
                }

                if (!playerControl.Dead && Time.timeScale != 0)
                {
                    // Unlinking y eulers of turret, barrel, and target from parent
                    Quaternion inverseParentRot = Quaternion.Euler(0, lastParentRotation.eulerAngles.y - tankOrigin.localEulerAngles.y, 0);
                    turret.localRotation = inverseParentRot * turret.localRotation;
                    barrel.localRotation = inverseParentRot * barrel.localRotation;

                    reticle.gameObject.SetActive(true);
                    Cursor.visible = false;

                    if (dstFromTarget == 0 && !alternateCamera)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        float eulerXOffset = CustomMath.FormattedAngle(turret.eulerAngles.x);
                        pitchMinMax = new Vector2(pitchMinMaxL.x + eulerXOffset, pitchMinMaxL.y + eulerXOffset);

                        turret.rotation = barrel.rotation = transform.rotation;
                        reticle.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);

                        body.GetComponent<MeshRenderer>().enabled = turret.GetComponent<MeshRenderer>().enabled = barrel.GetComponent<MeshRenderer>().enabled = false;
                    }
                    else
                    {
                        if (!invisible)
                        {
                            body.GetComponent<MeshRenderer>().enabled = turret.GetComponent<MeshRenderer>().enabled = barrel.GetComponent<MeshRenderer>().enabled = true;
                        }

                        Cursor.lockState = CursorLockMode.Confined;
                        pitchMinMax = pitchMinMaxN;

                        if (!lockTurret)
                        {
                            RotateToMousePoint();
                        }
                        else
                        {
                            if (Physics.Raycast(barrel.position + barrel.forward, barrel.forward, out RaycastHit barrelHit, Mathf.Infinity, ~mouseIgnoreLayers))
                            {
                                reticle.position = thisCamera.WorldToScreenPoint(barrelHit.point);
                            }
                        }
                    }

                    // Zeroing x and z eulers of turret and clamping barrel x euler
                    turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y, 0);
                    barrel.localEulerAngles = new Vector3(CustomMath.ClampAngle(barrel.localEulerAngles.x, pitchMinMaxL.x, pitchMinMaxL.y), barrel.localEulerAngles.y, 0);

                    lastParentRotation = tankOrigin.localRotation;
                }

                if (!alternateCamera)
                {
                    if (!lockCamera)
                    {
                        if (Application.isMobilePlatform)
                        {
                            if (Input.touchCount > 0)
                            {
                                foreach (Touch touch in Input.touches)
                                {
                                    if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId) && touch.phase == TouchPhase.Moved)
                                    {
                                        yaw -= touch.deltaPosition.x * DataManager.playerSettings.sensitivity / 8;
                                        pitch += touch.deltaPosition.y * DataManager.playerSettings.sensitivity / 8;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Translating inputs from mouse into smoothed rotation of camera
                            yaw += Input.GetAxis("Mouse X") * DataManager.playerSettings.sensitivity / 8;
                            pitch -= Input.GetAxis("Mouse Y") * DataManager.playerSettings.sensitivity / 8;
                        }

                        pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);

                        if (DataManager.playerSettings.cameraSmoothing)
                        {
                            currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref rotationSmoothVelocity, rotationSmoothing);
                        }
                        else
                        {
                            currentRotation = new Vector3(pitch, yaw, currentRotation.z);
                        }
                        // Setting rotation and position of camera on previous params and target and dstFromTarget
                        transform.eulerAngles = currentRotation;
                    }
                }
            }

            // Prevent clipping of camera
            if (Physics.Raycast(target.position, -transform.forward, out RaycastHit clippingHit, dstFromTarget, ~cameraIgnoreLayers))
            {
                transform.position = clippingHit.point + transform.forward * 0.1f;
            }
            else
            {
                transform.position = target.position - transform.forward * dstFromTarget;
            }
        }
    }

    void RotateToMousePoint()
    {
        if (Application.isMobilePlatform)
        {
            if (Input.touchCount > 0)
            {
                foreach (Touch touch in Input.touches)
                {
                    if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        Ray mouseRay = thisCamera.ScreenPointToRay(touch.position);

                        if (Physics.Raycast(mouseRay, out RaycastHit mouseHit, Mathf.Infinity, ~mouseIgnoreLayers))
                        {
                            Debug.DrawLine(transform.position, mouseHit.point, Color.green, 0.1f);
                            // Rotating turret and barrel towards the mouseHit point
                            barrel.rotation = turret.rotation = Quaternion.LookRotation(mouseHit.point - target.position, tankOrigin.up);
                        }
                        else
                        {
                            Debug.DrawRay(transform.position, mouseRay.direction * 5, Color.red, 0.1f);
                            barrel.rotation = turret.rotation = Quaternion.LookRotation(mouseRay.direction, tankOrigin.up);
                        }
                        reticle.position = touch.position;
                        break;
                    }
                }
            }
        }
        else
        {
            Ray mouseRay = thisCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(mouseRay, out RaycastHit mouseHit, Mathf.Infinity, ~mouseIgnoreLayers))
            {
                Debug.DrawLine(transform.position, mouseHit.point, Color.green, 0.1f);
                // Rotating turret and barrel towards the mouseHit point
                barrel.rotation = turret.rotation = Quaternion.LookRotation(mouseHit.point - target.position, tankOrigin.up);
            }
            else
            {
                Debug.DrawRay(transform.position, mouseRay.direction * 5, Color.red, 0.1f);
                barrel.rotation = turret.rotation = Quaternion.LookRotation(mouseRay.direction, tankOrigin.up);
            }
            reticle.position = Input.mousePosition;
        }
    }

    public void SwitchToAltCamera()
    {
        alternateCamera = true;
        lockCamera = false;

        dstFromTarget = Mathf.Clamp(dstFromTarget, altTargetDstMinMax.x, altTargetDstMinMax.y);
        transform.eulerAngles = new Vector3(90, 0, 0);
    }

    public void SetDstFromTarget(float dst)
    {
        alternateCamera = false;
        lockCamera = false;

        dstFromTarget = Mathf.Clamp(dst, targetDstMinMax.x, targetDstMinMax.y);
    }
}