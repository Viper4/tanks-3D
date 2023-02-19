using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class SpectatorUI : MonoBehaviour
{
    BaseUI baseUI;

    private void Start()
    {
        baseUI = GetComponent<BaseUI>();

        if(PhotonNetwork.OfflineMode)
        {
            baseUI.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\n" + GameManager.Instance.currentScene.name;
        }
        else
        {
            if(DataManager.roomSettings.mode != "Co-Op")
            {
                baseUI.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\n" + PhotonNetwork.CurrentRoom.Name;
            }
            else
            {
                baseUI.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\n" + PhotonNetwork.CurrentRoom.Name + "\n" + GameManager.Instance.currentScene.name;
            }
        }
        PhotonChatController.Instance.Resume(false);
        Resume(false);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(PhotonChatController.Instance.chatBoxActive)
            {
                PhotonChatController.Instance.Resume();
            }
            else
            {
                if (baseUI.UIElements["PauseMenu"].gameObject.activeSelf)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }
    }

    public void Resume(bool changeCursor = true)
    {
        baseUI.UIElements["PauseMenu"].gameObject.SetActive(false);
        baseUI.UIElements["Settings"].gameObject.SetActive(false);
        baseUI.UIElements["Change Teams"].gameObject.SetActive(false);
        GameManager.Instance.paused = false;

        if (changeCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        if (Application.isMobilePlatform)
            MobileWebAppHandler.Instance.Resume();
    }

    public void Pause()
    {
        baseUI.UIElements["PauseMenu"].gameObject.SetActive(true);
        GameManager.Instance.paused = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (Application.isMobilePlatform)
            MobileWebAppHandler.Instance.Pause();
    }

    public void Leave()
    {
        PhotonNetwork.LeaveRoom();
    }
}
