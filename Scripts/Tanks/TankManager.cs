using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class TankManager : MonoBehaviour
{
    public bool lastCampaignScene = false;
    public List<Transform> deadBots = new List<Transform>();
    bool checking = false;

    public void StartCheckTankCount()
    {
        StartCoroutine(CheckTankCount());
    }

    // Have to wait before checking childCount since mines can blow up multiple tanks simultaneously
    IEnumerator CheckTankCount()
    {
        if (!checking)
        {
            checking = true;
            yield return new WaitForEndOfFrame();
            if (!GameManager.autoPlay)
            {
                if (transform.childCount < 1)
                {
                    GameManager.frozen = true;

                    if (PhotonNetwork.OfflineMode)
                    {
                        if (lastCampaignScene)
                        {
                            GameManager.Instance.LoadScene("End Scene", 3, true);
                        }
                        else
                        {
                            GameManager.Instance.LoadNextScene(3, true);
                        }
                    }
                    else
                    {
                        if (PhotonNetwork.IsMasterClient)
                        {
                            PhotonHashtable roomProperties = new PhotonHashtable()
                            {
                                { "RoomSettings", (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"]}
                            };
                            if (lastCampaignScene)
                            {
                                ((RoomSettings)roomProperties["RoomSettings"]).map = "End Scene";
                                GameManager.Instance.PhotonLoadScene("End Scene", 3, true, false);
                            }
                            else
                            {
                                ((RoomSettings)roomProperties["RoomSettings"]).map = SceneManager.GetSceneByBuildIndex(GameManager.Instance.currentScene.buildIndex + 1).name;
                                GameManager.Instance.PhotonLoadNextScene(3, true);
                            }
                            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
                        }
                        else
                        {
                            if (lastCampaignScene)
                            {
                                GameManager.Instance.PhotonLoadScene("End Scene", 3, true, false);
                            }
                            else
                            {
                                GameManager.Instance.PhotonLoadNextScene(3, true);
                            }
                        }
                    }
                }
            }
            else
            {
                if (transform.childCount < 2)
                {
                    Time.timeScale = 0.2f;
                    yield return new WaitForSecondsRealtime(4);
                    StartCoroutine(GameManager.Instance.ResetAutoPlay(2.5f));
                }
            }
            checking = false;
        }
    }
}
