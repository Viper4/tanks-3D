using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileWebAppHandler : MonoBehaviour
{
    public static MobileWebAppHandler Instance;

    [SerializeField] GameObject mobileUI;
    [SerializeField] GameObject joystick;
    [SerializeField] GameObject mine;

    PlayerControl playerControl;
    int cameraView = 0;

    // Start is called before the first frame update
    void Start()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        if (Application.isMobilePlatform)
        {
            mobileUI.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Resume()
    {
        joystick.SetActive(true);
        mine.SetActive(true);
    }

    public void Pause()
    {
        joystick.SetActive(false);
        mine.SetActive(false);
    }

    public void EnablePlayerMode(PlayerControl playerControl)
    {
        this.playerControl = playerControl;
        mine.SetActive(true);
        joystick.SetActive(true);
        UpdateView();
    }

    public void DisablePlayerMode()
    {
        playerControl = null;
        mine.SetActive(false);
        joystick.SetActive(false);
    }

    public void TogglePause()
    {
        PlayerUI playerUI = FindObjectOfType<PlayerUI>();

        if (GameManager.Instance.paused)
        {
            if (PhotonChatController.Instance.chatBoxActive)
            {
                PhotonChatController.Instance.Resume();
            }

            if (playerUI != null)
            {
                playerUI.Resume();
            }
            else
            {
                SpectatorUI spectatorUI = FindObjectOfType<SpectatorUI>();
                if (spectatorUI != null)
                {
                    spectatorUI.Resume();
                }
            }
        }
        else
        {
            if (playerUI != null)
            {
                playerUI.Pause();
            }
            else
            {
                SpectatorUI spectatorUI = FindObjectOfType<SpectatorUI>();
                if (spectatorUI != null)
                {
                    spectatorUI.Pause();
                }
            }
        }
    }

    public void ToggleChat()
    {
        if (!PhotonNetwork.OfflineMode)
        {
            if (PhotonChatController.Instance.chatBoxActive)
            {
                PhotonChatController.Instance.Resume();
            }
            else
            {
                PhotonChatController.Instance.Pause();
            }
        }
    }

    public void ToggleHUD()
    {
        if (playerControl != null)
            playerControl.showHUD = !playerControl.showHUD;
    }

    public void ToggleView()
    {
        cameraView++;
        if(cameraView > 1)
        {
            cameraView = 0;
        }
        UpdateView();
    }

    void UpdateView()
    {
        if (Camera.main != null && Camera.main.TryGetComponent<CameraControl>(out var cameraControl))
        {
            switch (cameraView)
            {
                /*case 0:
                    cameraControl.SetDstFromTarget(0);
                    break;*/
                case 0:
                    cameraControl.SetDstFromTarget(6);
                    break;
                case 1:
                    cameraControl.SwitchToAltCamera();
                    break;
            }
        }
    }

    public void LayMine()
    {
        if(playerControl != null)
            playerControl.LayMine();
    }
}
