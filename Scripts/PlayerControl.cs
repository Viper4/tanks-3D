using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    CharacterController controller;
    Transform mainCamera;

    Transform turret;
    Transform barrel;
    Transform body;

    UIHandler UIHandler;

    public Dictionary<string, KeyCode> keyBinds { get; set; } = new Dictionary<string, KeyCode>();

    public int lives = 3;
    public int kills = 0;
    public int deaths = 0;
    public int highestRound = 1;

    public bool cheats = false;
    public bool dead { get; set; } = false;

    public float gravity = -12;
    public float movementSpeed = 6;
    float velocityY;

    float currentSpeed;
    public float speedSmoothTime = 0.1f;
    float speedSmoothVelocity;

    public float turnSmoothTime = 0.2f;
    float turnSmoothVelocity;

    // Start is called before the first frame update
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main.transform;

        barrel = transform.Find("Barrel");
        turret = transform.Find("Turret");
        body = transform.Find("Body");

        UIHandler = GameObject.Find("UI").GetComponent<UIHandler>();

        SaveSystem.LoadSettings("settings.json");
    }

    // Update is called once per frame
    void Update()
    {
        if (!dead)
        {
            // Firing bullets
            if (Input.GetKeyDown(keyBinds["Shoot"]))
            {
                StartCoroutine(GetComponent<FireControl>().Shoot());
            }
            else if (Input.GetKeyDown(keyBinds["Lay Mine"]) && controller.isGrounded)
            {
                StartCoroutine(GetComponent<MineControl>().LayMine());
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (UIHandler.UIElements["PauseMenu"].gameObject.activeSelf)
                {
                    UIHandler.Resume();
                }
                else
                {
                    UIHandler.Pause();
                }
            }

            Vector2 input = new Vector2(GetInputAxis("Horizontal"), GetInputAxis("Vertical"));
            Vector2 inputDir = input.normalized;

            // Moving the tank with player input
            float targetSpeed = movementSpeed / 2 * inputDir.magnitude;

            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, GetModifiedSmoothTime(speedSmoothTime));

            velocityY += Time.deltaTime * gravity;
            mainCamera.Find("Anchor").eulerAngles = new Vector3(0, mainCamera.eulerAngles.y, mainCamera.eulerAngles.z);
            Vector3 velocity = mainCamera.right * inputDir.x * currentSpeed + mainCamera.Find("Anchor").forward * inputDir.y * currentSpeed + Vector3.up * velocityY;
            controller.Move(velocity * Time.deltaTime);

            if (controller.isGrounded)
            {
                velocityY = 0;
            }

            // Rotating tank with movement
            if (inputDir != Vector2.zero)
            {
                float targetRotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
                body.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(body.eulerAngles.y, targetRotation, ref turnSmoothVelocity, GetModifiedSmoothTime(turnSmoothTime)) + new Vector3(-90, 0, 0);
            }
        }
        else
        {
            if (cheats && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("Cheat Player Respawn");
                Camera.main.GetComponent<CameraControl>().dead = false;
                dead = false;

                GetComponent<CharacterController>().enabled = true;
                barrel.gameObject.SetActive(true);
                turret.gameObject.SetActive(true);
                body.gameObject.SetActive(true);
            }
        }
    }

    float GetModifiedSmoothTime(float smoothTime)
    {
        if (controller.isGrounded)
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
                if (Input.GetKey(keyBinds["Right"]))
                {
                    horizontal += 1;
                }
                if (Input.GetKey(keyBinds["Left"]))
                {
                    horizontal -= 1;
                }
                return horizontal;
            case "Vertical":
                float vertical = 0;
                if (Input.GetKey(keyBinds["Forward"]))
                {
                    vertical += 1;
                }
                if (Input.GetKey(keyBinds["Backward"]))
                {
                    vertical -= 1;
                }
                return vertical;
        }
        
        return 0;
    }

    public IEnumerator Restart()
    {
        yield return new WaitForSeconds(3);

        StartCoroutine(GameObject.Find("SceneLoader").GetComponent<SceneLoader>().LoadScene(0));
    }
}
