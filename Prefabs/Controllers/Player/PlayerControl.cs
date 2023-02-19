using System.Collections;
using UnityEngine;
using Photon.Pun;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.EventSystems;

public class PlayerControl : MonoBehaviour
{
    public ClientManager clientManager;

    MobileInput mobileInput;
    [SerializeField] Rigidbody RB;
    [SerializeField] BaseTankLogic baseTankLogic;
    [SerializeField] Transform mainCamera;
    [SerializeField] Transform tankOrigin;
    FireControl fireControl;
    MineControl mineControl;

    [SerializeField] bool cheats = false;
    public bool godMode = false;
    public bool Dead { get; set; } = false;
    public bool showHUD = true;

    float currentSpeed;
    public float speedSmoothTime = 0.05f;
    float speedSmoothVelocity;

    public float turnSmoothTime = 0.2f;
    float turnSmoothVelocity;
    float velocityY = 0;

    [SerializeField] LayerMask ignoreLayerMasks;

    private void Awake()
    {
        mobileInput = new MobileInput();
    }

    private void Start()
    {
        fireControl = GetComponent<FireControl>();
        mineControl = GetComponent<MineControl>();
        if (Application.isMobilePlatform)
        {
            MobileWebAppHandler.Instance.EnablePlayerMode(this);
        }
        GameManager.Instance.UpdatePlayerWithSettings(transform);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(PhotonNetwork.OfflineMode || clientManager.photonView.IsMine)
        {
            if(!Dead && !GameManager.Instance.frozen)
            {
                if(Time.timeScale != 0)
                {
                    Vector2 inputDir = Vector2.zero;

                    if(!GameManager.Instance.paused)
                    {
                        if (Application.isMobilePlatform)
                        {
                            if(Input.touchCount > 0)
                            {
                                foreach(Touch touch in Input.touches)
                                {
                                    if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId) && touch.phase == TouchPhase.Began)
                                    {
                                        StartCoroutine(fireControl.Shoot());
                                    }
                                }
                            }
                            inputDir = mobileInput.Player.Move.ReadValue<Vector2>();
                        }
                        else
                        {
                            if (Input.GetKeyDown(DataManager.playerSettings.keyBinds["Shoot"]))
                            {
                                StartCoroutine(fireControl.Shoot());
                            }
                            else if (Input.GetKeyDown(DataManager.playerSettings.keyBinds["Lay Mine"]) && baseTankLogic.IsGrounded())
                            {
                                StartCoroutine(mineControl.LayMine());
                            }
                            else if (Input.GetKeyDown(DataManager.playerSettings.keyBinds["Toggle HUD"]))
                            {
                                showHUD = !showHUD;
                            }
                            inputDir = new Vector2(GetInputAxis("Horizontal"), GetInputAxis("Vertical")).normalized;
                        }
                    }

                    // Moving the tank with player input
                    float targetSpeed = baseTankLogic.normalSpeed * 0.5f * inputDir.magnitude;

                    currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, GetModifiedSmoothTime(speedSmoothTime));

                    Vector3 velocityDir = tankOrigin.forward;

                    if(Physics.Raycast(tankOrigin.position, -tankOrigin.up, out RaycastHit middleHit, 1, ~ignoreLayerMasks) && Physics.Raycast(tankOrigin.position + tankOrigin.forward, -tankOrigin.up, out RaycastHit frontHit, 1, ~ignoreLayerMasks))
                    {
                        velocityDir = frontHit.point - middleHit.point;
                    }

                    Vector3 velocity = currentSpeed * velocityDir;

                    velocityY = !baseTankLogic.IsGrounded() && baseTankLogic.useGravity ? velocityY + Time.deltaTime * Physics.gravity.y : 0;
                    velocityY = Mathf.Clamp(velocityY, -baseTankLogic.velocityLimit, baseTankLogic.velocityLimit);

                    RB.velocity = velocity + Vector3.up * velocityY;

                    // Rotating tank with movement
                    if(inputDir != Vector2.zero)
                    {
                        float targetRotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
                        float angle = tankOrigin.eulerAngles.y - targetRotation;
                        angle = angle < 0 ? angle + 360 : angle;
                        if(angle > 180 - baseTankLogic.flipAngleThreshold && angle < 180 + baseTankLogic.flipAngleThreshold)
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
                RB.velocity = Vector3.zero;
            }

            if(cheats)
            {
                if(Input.GetKey(KeyCode.LeftAlt))
                {
                    if(Input.GetKeyDown(KeyCode.N))
                    {
                        Debug.Log("Cheat Next Level");
                        GameManager.Instance.LoadNextScene();
                    }
                    else if(Input.GetKeyDown(KeyCode.R))
                    {
                        Debug.Log("Cheat Reload");
                        GameManager.Instance.LoadScene(-1);
                    }
                    else if(Input.GetKeyDown(KeyCode.B))
                    {
                        Debug.Log("Cheat Reset");
                        Dead = false;

                        tankOrigin.Find("Body").GetComponent<MeshRenderer>().enabled = true;
                        tankOrigin.Find("Turret").GetComponent<MeshRenderer>().enabled = true;
                        tankOrigin.Find("Barrel").GetComponent<MeshRenderer>().enabled = true;
                    }
                    else if(Input.GetKeyDown(KeyCode.G))
                    {
                        Debug.Log("God Mode Toggled");

                        godMode = !godMode;
                    }
                }
            }
        }
    }

    private void OnEnable()
    {
        mobileInput.Enable();
    }

    private void OnDisable()
    {
        if (Application.isMobilePlatform)
            MobileWebAppHandler.Instance.DisablePlayerMode();
        mobileInput.Disable();
    }

    public void LayMine()
    {
        if ((PhotonNetwork.OfflineMode || clientManager.photonView.IsMine) && !Dead && !GameManager.Instance.frozen && Time.timeScale != 0 && !GameManager.Instance.paused && baseTankLogic.IsGrounded())
        {
            StartCoroutine(mineControl.LayMine());
        }
    }

    float GetModifiedSmoothTime(float smoothTime)
    {
        if(baseTankLogic.IsGrounded())
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
                if (Input.GetKey(DataManager.playerSettings.keyBinds["Right"]))
                {
                    horizontal += 1;
                }
                if (Input.GetKey(DataManager.playerSettings.keyBinds["Left"]))
                {
                    horizontal -= 1;
                }
                return horizontal;
            case "Vertical":
                float vertical = 0;
                if (Input.GetKey(DataManager.playerSettings.keyBinds["Forward"]))
                {
                    vertical += 1;
                }
                if (Input.GetKey(DataManager.playerSettings.keyBinds["Backward"]))
                {
                    vertical -= 1;
                }
                return vertical;
        }
        
        return 0;
    }

    public void OnDeath()
    {
        if(!PhotonNetwork.OfflineMode)
        {
            if(GetComponent<PhotonView>().IsMine)
            {
                DataManager.playerData.deaths++;
                PhotonHashtable playerProperties = new PhotonHashtable();
                if(DataManager.roomSettings.mode == "Co-Op")
                    GameManager.Instance.canSpawn = false;
                playerProperties.Add("deaths", DataManager.playerData.deaths);
                PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
            }
            PlayerManager.Instance.OnPlayerDeath(tankOrigin);
        }
        else
        {
            DataManager.playerData.deaths++;
            DataManager.playerData.lives--;

            if(DataManager.playerData.lives > 0)
            {
                GameManager.Instance.LoadScene(-1, 3, true);
            }
            else
            {
                GameManager.Instance.LoadScene("End Scene", 3, true);
            }
        }
    }

    [PunRPC]
    public void ReactivatePlayer()
    {
        Dead = false;
        tankOrigin.GetComponent<CapsuleCollider>().enabled = true;

        tankOrigin.Find("Body").gameObject.SetActive(true);
        tankOrigin.Find("Turret").gameObject.SetActive(true);
        tankOrigin.Find("Barrel").gameObject.SetActive(true);
    }
}
