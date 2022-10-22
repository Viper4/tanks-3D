using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class SpectatorUIHandler : MonoBehaviour
{
    [SerializeField] SpectatorControl spectatorControl;
    BaseUIHandler baseUIHandler;

    private void Start()
    {
        baseUIHandler = GetComponent<BaseUIHandler>();

        if (PhotonNetwork.OfflineMode)
        {
            baseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\n" + GameManager.Instance.currentScene.name;
        }
        else
        {
            if (((RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"]).mode != "Co-Op")
            {
                baseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\n" + PhotonNetwork.CurrentRoom.Name;
            }
            else
            {
                baseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\n" + PhotonNetwork.CurrentRoom.Name + "\n" + GameManager.Instance.currentScene.name;
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (baseUIHandler.UIElements["PauseMenu"].gameObject.activeSelf)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        baseUIHandler.UIElements["PauseMenu"].gameObject.SetActive(false);
        spectatorControl.Paused = false;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Pause()
    {
        baseUIHandler.UIElements["PauseMenu"].gameObject.SetActive(true);
        spectatorControl.Paused = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Leave()
    {
        PhotonNetwork.LeaveRoom();
    }
}
