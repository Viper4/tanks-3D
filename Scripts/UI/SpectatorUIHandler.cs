using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;

public class SpectatorUIHandler : MonoBehaviour
{
    [SerializeField] SpectatorControl spectatorControl;
    [SerializeField] ClientManager clientManager;
    BaseUIHandler baseUIHandler;

    private void Start()
    {
        baseUIHandler = GetComponent<BaseUIHandler>();

        baseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\n " + PhotonNetwork.CurrentRoom.Name;

        if (clientManager.photonView.IsMine)
        {
            GameObject[] allUIs = GameObject.FindGameObjectsWithTag("PlayerUI");
            foreach (GameObject UI in allUIs)
            {
                if (UI != gameObject)
                {
                    UI.SetActive(false);
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
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
        SceneManager.LoadScene("Lobby");
    }
}
