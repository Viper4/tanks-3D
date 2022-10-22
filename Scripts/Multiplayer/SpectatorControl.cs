using UnityEngine;
using MyUnityAddons.CustomPhoton;
using Photon.Realtime;

public class SpectatorControl : MonoBehaviour
{
    [SerializeField] Rigidbody RB;

    public float sensitivity = 15;
    [SerializeField] Transform target;
    int targetIndex = -1;

    [SerializeField] float dstFromTarget = 4;

    [SerializeField] Vector2 targetDstLimit = new Vector2(0, 50);

    [SerializeField] float movementSpeed = 6;
    [SerializeField] float speedLimit = 100;

    public float rotationSmoothTime = 0.1f;
    Vector3 rotationSmoothVelocity;
    Vector3 currentRotation;

    float yaw;
    float pitch;

    public bool Paused { get; set; }

    private void Start()
    {
        currentRotation = transform.eulerAngles;
        pitch = transform.eulerAngles.x;
        yaw = transform.eulerAngles.y;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Paused && Time.timeScale != 0)
        {
            // Updating every username to rotate to this camera for this client
            UsernameSystem[] allUsernames = FindObjectsOfType<UsernameSystem>();
            foreach (UsernameSystem username in allUsernames)
            {
                username.UpdateTextMeshTo(transform, false);
            }

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            float zoomRate = Input.GetKey(KeyCode.LeftShift) ? 0.5f : 5f;

            Vector3 inputDir = new Vector3(GetInputAxis("x"), GetInputAxis("y"), GetInputAxis("z")).normalized;

            float targetSpeed = movementSpeed / 2 * inputDir.magnitude;

            Player[] nonSpectatorList = CustomNetworkHandling.NonSpectatorList;
            if (!GameManager.Instance.frozen && Input.GetMouseButtonDown(0) && nonSpectatorList.Length != 0)
            {
                if (target != null && target.TryGetComponent<MeshRenderer>(out var oldMeshRenderer))
                {
                    oldMeshRenderer.enabled = true;
                }
                targetIndex++;
                if (targetIndex > nonSpectatorList.Length - 1)
                {
                    targetIndex = -1;
                }
                else
                {
                    target = nonSpectatorList[targetIndex].FindPhotonView().transform.Find("Tank Origin/Barrel");
                    if (target != null && target.TryGetComponent<MeshRenderer>(out var newMeshRenderer))
                    {
                        newMeshRenderer.enabled = false;
                    }
                }
            }

            if (target == null)
            {
                targetIndex = -1;
            }

            if (targetIndex == -1)
            {
                // Speed up/down with scroll
                if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
                {
                    movementSpeed = Mathf.Clamp(movementSpeed - zoomRate, 0, speedLimit);
                }
                else if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
                {
                    movementSpeed = Mathf.Clamp(movementSpeed + zoomRate, 0, speedLimit);
                }

                MouseCameraRotation();

                target = transform;
                dstFromTarget = 0;
            }
            else
            {
                if (targetSpeed != 0)
                {
                    if (target.TryGetComponent<MeshRenderer>(out var meshRenderer))
                    {
                        meshRenderer.enabled = true;
                    }
                    targetIndex = -1;
                }

                // Zoom with scroll
                if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
                {
                    dstFromTarget = Mathf.Clamp(dstFromTarget - zoomRate, targetDstLimit.x, targetDstLimit.y);
                    if (dstFromTarget == 0)
                    {
                        if (target.TryGetComponent<MeshRenderer>(out var meshRenderer))
                        {
                            meshRenderer.enabled = false;
                        }
                    }
                }
                else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
                {
                    dstFromTarget = Mathf.Clamp(dstFromTarget + zoomRate, targetDstLimit.x, targetDstLimit.y);

                    if (target.TryGetComponent<MeshRenderer>(out var meshRenderer))
                    {
                        meshRenderer.enabled = true;
                    }
                }

                if (dstFromTarget == 0)
                {
                    transform.rotation = target.rotation;
                }
                else
                {
                    MouseCameraRotation();
                }

                transform.position = target.position - transform.forward * dstFromTarget;
            }

            Vector3 velocity = targetSpeed * (Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up) * inputDir);
            RB.velocity = velocity;
        }
    }

    private float GetInputAxis(string axis)
    {
        switch (axis)
        {
            case "x":
                float x = 0;
                if (Input.GetKey(DataManager.playerSettings.keyBinds["Right"]))
                {
                    x += 1;
                }
                if (Input.GetKey(DataManager.playerSettings.keyBinds["Left"]))
                {
                    x -= 1;
                }
                return x;
            case "y":
                float y = 0;
                if (Input.GetKey(DataManager.playerSettings.keyBinds["Up"]))
                {
                    y += 1;
                }
                if (Input.GetKey(DataManager.playerSettings.keyBinds["Down"]))
                {
                    y -= 1;
                }
                return y;
            case "z":
                float z = 0;
                if (Input.GetKey(DataManager.playerSettings.keyBinds["Forward"]))
                {
                    z += 1;
                }
                if (Input.GetKey(DataManager.playerSettings.keyBinds["Backward"]))
                {
                    z -= 1;
                }
                return z;
        }

        return 0;
    }

    private void MouseCameraRotation()
    {
        // Translating inputs from mouse into smoothed rotation of camera
        yaw += Input.GetAxis("Mouse X") * sensitivity / 4;
        pitch -= Input.GetAxis("Mouse Y") * sensitivity / 4;
        pitch = Mathf.Clamp(pitch, -90, 90);

        currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref rotationSmoothVelocity, rotationSmoothTime);
        // Setting rotation and position of camera on previous params and target and dstFromTarget
        transform.eulerAngles = currentRotation;
    }
}
