using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    Rigidbody rb;
    BaseTankLogic baseTankLogic;
    Transform mainCamera;

    Transform tankOrigin;

    PlayerUIHandler playerUIHandler;

    [SerializeField] bool cheats = false;
    public bool godMode = false;
    public bool Dead { get; set; } = false;

    [SerializeField] float movementSpeed = 6;

    float currentSpeed;
    public float speedSmoothTime = 0.1f;
    float speedSmoothVelocity;

    public float turnSmoothTime = 0.2f;
    float turnSmoothVelocity;

    public float gravity = 10;
    float velocityY = 0;

    [SerializeField] LayerMask ignoreLayerMasks;

    // Start is called before the first frame Update
    void Awake()
    {
        baseTankLogic = GetComponent<BaseTankLogic>();
        mainCamera = Camera.main.transform;

        tankOrigin = transform.Find("Tank Origin");
        rb = tankOrigin.GetComponent<Rigidbody>();

        playerUIHandler = GameObject.Find("UI").GetComponent<PlayerUIHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!Dead && !SceneLoader.frozen)
        {
            if (Time.timeScale != 0)
            {
                // Firing bullets
                if (Input.GetKeyDown(SaveSystem.currentSettings.keyBinds["Shoot"]))
                {
                    StartCoroutine(GetComponent<FireControl>().Shoot());
                }
                else if (Input.GetKeyDown(SaveSystem.currentSettings.keyBinds["Lay Mine"]) && baseTankLogic.IsGrounded())
                {
                    StartCoroutine(GetComponent<MineControl>().LayMine());
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (BaseUIHandler.UIElements["PauseMenu"].gameObject.activeSelf)
                    {
                        playerUIHandler.Resume();
                    }
                    else
                    {
                        playerUIHandler.Pause();
                    }
                }

                if (baseTankLogic.IsGrounded())
                {
                    velocityY = 0;
                }
                else
                {
                    velocityY -= gravity * Time.deltaTime;
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
                        tankOrigin.forward = -tankOrigin.forward;
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
                    SceneLoader.sceneLoader.LoadScene(false);
                }
                else if (Input.GetKeyDown(KeyCode.B))
                {
                    Debug.Log("Cheat Reset");
                    Dead = false;

                    tankOrigin.Find("Body").gameObject.SetActive(false);
                    tankOrigin.Find("Turret").gameObject.SetActive(false);
                    tankOrigin.Find("Barrel").gameObject.SetActive(false);
                }
                else if (Input.GetKeyDown(KeyCode.G))
                {
                    Debug.Log("God Mode Toggled");

                    godMode = !godMode;
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
                if (Input.GetKey(SaveSystem.currentSettings.keyBinds["Right"]))
                {
                    horizontal += 1;
                }
                if (Input.GetKey(SaveSystem.currentSettings.keyBinds["Left"]))
                {
                    horizontal -= 1;
                }
                return horizontal;
            case "Vertical":
                float vertical = 0;
                if (Input.GetKey(SaveSystem.currentSettings.keyBinds["Forward"]))
                {
                    vertical += 1;
                }
                if (Input.GetKey(SaveSystem.currentSettings.keyBinds["Backward"]))
                {
                    vertical -= 1;
                }
                return vertical;
        }
        
        return 0;
    }

    public void Respawn()
    {
        SaveSystem.currentPlayerData.lives--;
        SaveSystem.currentPlayerData.deaths++;

        if (SaveSystem.currentPlayerData.lives > 0)
        {
            SceneLoader.sceneLoader.LoadScene(false, -1, 3);
        }
        else
        {
            SceneLoader.sceneLoader.LoadScene(true, 0, 3);
        }
    }
}
