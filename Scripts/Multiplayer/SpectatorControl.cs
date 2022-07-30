using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.CustomPhoton;

public class SpectatorControl : MonoBehaviour
{
    [SerializeField] PhotonView PV;
    [SerializeField] DataManager dataSystem;

    [SerializeField] Camera myCamera;
    [SerializeField] Rigidbody RB;

    public float sensitivity = 15;
    [SerializeField] Transform target;
    int targetIndex = -1;

    [SerializeField] float dstFromTarget = 4;

    [SerializeField] Vector2 targetDstLimit = new Vector2(0, 50);

    [SerializeField] float movementSpeed = 6;
    [SerializeField] float speedLimit = 100;

    [SerializeField] float rotationSmoothTime = 0.1f;
    Vector3 rotationSmoothVelocity;
    Vector3 currentRotation;

    float yaw;
    float pitch;

    public bool Paused { get; set; }

    // Update is called once per frame
    void LateUpdate()
    {
        if (PV.IsMine)
        {
            if (!Paused)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                float zoomRate = Input.GetKey(KeyCode.LeftShift) ? 0.5f : 5f;

                Vector3 inputDir = new Vector3(GetInputAxis("x"), GetInputAxis("y"), GetInputAxis("z")).normalized;

                float targetSpeed = movementSpeed / 2 * inputDir.magnitude;

                if (Input.GetMouseButtonDown(0) && CustomNetworkHandling.NonSpectatorList.Length != 0)
                {
                    targetIndex = targetIndex + 1 > CustomNetworkHandling.NonSpectatorList.Length ? 0 : targetIndex + 1;
                    target = CustomNetworkHandling.FindPhotonView(CustomNetworkHandling.NonSpectatorList[targetIndex]).transform;
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
                        targetIndex = -1;
                    }

                    // Zoom with scroll
                    if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
                    {
                        dstFromTarget = Mathf.Clamp(dstFromTarget - zoomRate, targetDstLimit.x, targetDstLimit.y);
                    }
                    else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
                    {
                        dstFromTarget = Mathf.Clamp(dstFromTarget + zoomRate, targetDstLimit.x, targetDstLimit.y);
                    }

                    if (dstFromTarget == 0)
                    {
                        myCamera.transform.rotation = target.rotation;
                    }
                    else
                    {
                        MouseCameraRotation();
                    }

                    myCamera.transform.position = target.position - myCamera.transform.forward * dstFromTarget;
                }

                Vector3 velocity = targetSpeed * (Quaternion.AngleAxis(myCamera.transform.eulerAngles.y, Vector3.up) * inputDir);
                RB.velocity = velocity;
            }
        }
        else
        {
            Destroy(this);
        }
    }

    private float GetInputAxis(string axis)
    {
        switch (axis)
        {
            case "x":
                float x = 0;
                if (Input.GetKey(dataSystem.currentPlayerSettings.keyBinds["Right"]))
                {
                    x += 1;
                }
                if (Input.GetKey(dataSystem.currentPlayerSettings.keyBinds["Left"]))
                {
                    x -= 1;
                }
                return x;
            case "y":
                float y = 0;
                if (Input.GetKey(dataSystem.currentPlayerSettings.keyBinds["Up"]))
                {
                    y += 1;
                }
                if (Input.GetKey(dataSystem.currentPlayerSettings.keyBinds["Down"]))
                {
                    y -= 1;
                }
                return y;
            case "z":
                float z = 0;
                if (Input.GetKey(dataSystem.currentPlayerSettings.keyBinds["Forward"]))
                {
                    z += 1;
                }
                if (Input.GetKey(dataSystem.currentPlayerSettings.keyBinds["Backward"]))
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
        myCamera.transform.eulerAngles = currentRotation;
    }
}
