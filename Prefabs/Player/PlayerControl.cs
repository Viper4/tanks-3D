using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerControl : MonoBehaviour
{
    public MultiplayerManager multiplayerManager;
    public DataSystem dataSystem;

    [SerializeField] Rigidbody rb;
    [SerializeField] BaseTankLogic baseTankLogic;
    [SerializeField] Transform mainCamera;

    [SerializeField] Transform tankOrigin;

    [SerializeField] PlayerUIHandler playerUIHandler;
    [SerializeField] BaseUIHandler baseUIHandler;

    [SerializeField] bool cheats = false;
    public bool godMode = false;
    public bool Dead { get; set; } = false;
    public bool Paused { get; set; } = false;
    public bool Timing { get; set; } = true;

    bool respawning = false;

    [SerializeField] float movementSpeed = 6;

    float currentSpeed;
    public float speedSmoothTime = 0.1f;
    float speedSmoothVelocity;

    public float turnSmoothTime = 0.2f;
    float turnSmoothVelocity;

    public float gravity = 10;
    float velocityY = 0;
    [SerializeField] float speedLimit = 15;

    [SerializeField] LayerMask ignoreLayerMasks;

    // Update is called once per frame
    void LateUpdate()
    {
        if (multiplayerManager.ViewIsMine())
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (baseUIHandler.UIElements["PauseMenu"].gameObject.activeSelf)
                {
                    playerUIHandler.Resume();
                }
                else
                {
                    playerUIHandler.Pause();
                }
            }

            if (!Dead && !SceneLoader.frozen && !Paused)
            {
                if (Time.timeScale != 0)
                {
                    // Firing bullets
                    if (Input.GetKeyDown(dataSystem.currentSettings.keyBinds["Shoot"]))
                    {
                        StartCoroutine(GetComponent<FireControl>().Shoot());
                    }
                    else if (Input.GetKeyDown(dataSystem.currentSettings.keyBinds["Lay Mine"]) && baseTankLogic.IsGrounded())
                    {
                        StartCoroutine(GetComponent<MineControl>().LayMine());
                    }

                    if (baseTankLogic.IsGrounded())
                    {
                        velocityY = 0;
                    }
                    else
                    {
                        velocityY -= gravity * Time.deltaTime;
                        velocityY = Mathf.Clamp(velocityY, -speedLimit, speedLimit);
                    }

                    Vector2 inputDir = new Vector2(GetInputAxis("Horizontal"), GetInputAxis("Vertical")).normalized;

                    // Moving the tank with player input
                    float targetSpeed = movementSpeed / 2 * inputDir.magnitude;

                    currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, GetModifiedSmoothTime(speedSmoothTime));

                    mainCamera.Find("Anchor").eulerAngles = new Vector3(0, mainCamera.eulerAngles.y, mainCamera.eulerAngles.z);

                    Vector3 velocityDir = tankOrigin.forward;

                    RaycastHit middleHit;
                    RaycastHit frontHit;
                    if (Physics.Raycast(tankOrigin.position + tankOrigin.up * 1.22f, -tankOrigin.up, out middleHit, 3, ~ignoreLayerMasks) && Physics.Raycast(tankOrigin.position + tankOrigin.up * 1.22f + tankOrigin.forward, -tankOrigin.up, out frontHit, 3, ~ignoreLayerMasks))
                    {
                        velocityDir = frontHit.point - middleHit.point;
                    }

                    Vector3 velocity = currentSpeed * velocityDir + Vector3.up * velocityY;

                    rb.velocity = velocity;

                    // Rotating tank with movement
                    if (inputDir != Vector2.zero)
                    {
                        float targetRotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
                        float angle = tankOrigin.eulerAngles.y - targetRotation;
                        angle = angle < 0 ? angle + 360 : angle;
                        if (angle > 180 - baseTankLogic.flipAngleThreshold && angle < 180 + baseTankLogic.flipAngleThreshold)
                        {
                            baseTankLogic.FlipTank();
                        }
                        else
                        {
                            tankOrigin.eulerAngles = new Vector3(tankOrigin.eulerAngles.x, Mathf.SmoothDampAngle(tankOrigin.eulerAngles.y, targetRotation, ref turnSmoothVelocity, GetModifiedSmoothTime(turnSmoothTime)), tankOrigin.eulerAngles.z);
                        }
                    }
                }
            }
            else
            {
                rb.velocity = Vector3.zero;
            }

            if (cheats)
            {
                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    if (Input.GetKeyDown(KeyCode.N))
                    {
                        Debug.Log("Cheat Next Level");
                        SceneLoader.sceneLoader.LoadNextScene();
                    }
                    else if (Input.GetKeyDown(KeyCode.R))
                    {
                        Debug.Log("Cheat Reload");
                        SceneLoader.sceneLoader.LoadScene(-1);
                    }
                    else if (Input.GetKeyDown(KeyCode.B))
                    {
                        Debug.Log("Cheat Reset");
                        Dead = false;

                        tankOrigin.Find("Body").GetComponent<MeshRenderer>().enabled = true;
                        tankOrigin.Find("Turret").GetComponent<MeshRenderer>().enabled = true;
                        tankOrigin.Find("Barrel").GetComponent<MeshRenderer>().enabled = true;
                    }
                    else if (Input.GetKeyDown(KeyCode.G))
                    {
                        Debug.Log("God Mode Toggled");

                        godMode = !godMode;
                    }
                }
            }
        }
    }

    float GetModifiedSmoothTime(float smoothTime)
    {
        if (baseTankLogic.IsGrounded())
        {
            return smoothTime;
        }

        return smoothTime / 0.1f;
    }

    private float GetInputAxis(string axis)
    {
        switch (axis)
        {
            case "Horizontal":
                float horizontal = 0;
                if (Input.GetKey(dataSystem.currentSettings.keyBinds["Right"]))
                {
                    horizontal += 1;
                }
                if (Input.GetKey(dataSystem.currentSettings.keyBinds["Left"]))
                {
                    horizontal -= 1;
                }
                return horizontal;
            case "Vertical":
                float vertical = 0;
                if (Input.GetKey(dataSystem.currentSettings.keyBinds["Forward"]))
                {
                    vertical += 1;
                }
                if (Input.GetKey(dataSystem.currentSettings.keyBinds["Backward"]))
                {
                    vertical -= 1;
                }
                return vertical;
        }
        
        return 0;
    }

    public void Respawn()
    {
        dataSystem.currentPlayerData.deaths++;

        if (multiplayerManager.inMultiplayer)
        {
            if (!respawning)
            {
                StartCoroutine(MultiplayerRespawn());
            }
        }
        else
        {
            dataSystem.currentPlayerData.lives--;

            if (dataSystem.currentPlayerData.lives > 0)
            {
                SceneLoader.sceneLoader.LoadScene(-1, 3, true);
            }
            else
            {
                SceneLoader.sceneLoader.LoadScene("End Scene", 3, true);
            }
        }
    }

    IEnumerator MultiplayerRespawn()
    {
        respawning = true;
        yield return new WaitForSeconds(3);

        Dead = false;

        tankOrigin.localPosition = Vector3.zero;
        tankOrigin.localRotation = Quaternion.identity;
        FindObjectOfType<SpawnPlayers>().RespawnPlayer(tankOrigin);

        if (multiplayerManager.inMultiplayer)
        {
            multiplayerManager.photonView.RPC("ReactivatePlayer", RpcTarget.All, new object[] { true });
        }
        else
        {
            ReactivatePlayer(false);
        }

        respawning = false;
    }
    
    [PunRPC]
    void ReactivatePlayer(bool RPC)
    {
        tankOrigin.GetComponent<CapsuleCollider>().enabled = true;

        tankOrigin.Find("Body").gameObject.SetActive(true);
        tankOrigin.Find("Turret").gameObject.SetActive(true);
        tankOrigin.Find("Barrel").gameObject.SetActive(true);

        if (RPC)
        {
            tankOrigin.Find("TrackMarks").GetComponent<PhotonView>().RPC("ResetTrails", RpcTarget.All);
        }
        else
        {
            tankOrigin.Find("TrackMarks").GetComponent<TrailEmitter>().ResetTrails();
        }
    }
}
