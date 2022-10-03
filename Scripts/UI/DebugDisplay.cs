using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.Profiling;
using MyUnityAddons.CustomConversions;

public class DebugDisplay : MonoBehaviour
{
    [SerializeField] GameObject debugMenu;

    [SerializeField] Color textHighlightColor;
    [SerializeField] TextMeshProUGUI versionText;
    [SerializeField] TextMeshProUGUI unityVersionText;
    [SerializeField] TextMeshProUGUI punVersionText;
    [SerializeField] TextMeshProUGUI fpsText;
    [SerializeField] TextMeshProUGUI memoryText;
    [SerializeField] TextMeshProUGUI pingText;
    [SerializeField] float refreshRate = 1;

    string textHighlightHexCode = "#FFFFFF80";

    int frameCount = 0;
    float dt = 0.0f;
    int fps = 0;

    // Start is called before the first frame update
    void Start()
    {
        textHighlightHexCode = "#" + ColorUtility.ToHtmlStringRGBA(textHighlightColor);
    }

    private void Update()
    {
        frameCount++;
        dt += Time.deltaTime;
        if (dt > 1.0 / refreshRate)
        {
            fps = (int)(frameCount / dt);
            frameCount = 0;
            dt -= 1.0f / refreshRate;
            RefreshDisplay();
        }

        if (Input.GetKeyDown(DataManager.playerSettings.keyBinds["Debug Menu"])) 
        {
            debugMenu.SetActive(!debugMenu.activeSelf);
            RefreshDisplay();
        }
    }

    void RefreshDisplay()
    {
        if (debugMenu.activeInHierarchy)
        {
            versionText.text = "<mark=" + textHighlightHexCode + ">Tanks 3D " + Application.version + "</mark>";
            unityVersionText.text = "<mark=" + textHighlightHexCode + ">Unity " + Application.unityVersion + "</mark>";
            punVersionText.text = "<mark=" + textHighlightHexCode + ">PUN " + PhotonNetwork.PunVersion + "</mark>";

            fpsText.text = "<mark=" + textHighlightHexCode + ">" + fps + " FPS </mark>";

            string usedMemory = Conversions.SizeSuffix(Profiler.GetMonoUsedSizeLong());
            string totalMemory = Conversions.SizeSuffix(Profiler.GetMonoHeapSizeLong());
            memoryText.text = "<mark=" + textHighlightHexCode + ">Heap size: " + usedMemory + " / " + totalMemory + "</mark>";

            if (PhotonNetwork.OfflineMode)
            {
                pingText.text = "<mark=" + textHighlightHexCode + ">Offline</mark>";
            }
            else
            {
                if (PhotonNetwork.CurrentRoom != null)
                {
                    pingText.text = "<mark=" + textHighlightHexCode + ">" + PhotonNetwork.CurrentRoom.Name + " (" + PhotonNetwork.MasterClient.NickName + "): " + PhotonNetwork.GetPing() + " ms</mark>";
                }
                else
                {
                    pingText.text = "<mark=" + textHighlightHexCode + ">Lobby: " + PhotonNetwork.GetPing() + " ms</mark>";
                }
            }
        }
    }
}
