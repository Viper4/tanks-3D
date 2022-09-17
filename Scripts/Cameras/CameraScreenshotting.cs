using MyUnityAddons.Calculations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraScreenshotting : MonoBehaviour
{
    [SerializeField] RectTransform screenshotPopup;
    Image spriteImage;
    Animation anim;

    Coroutine popupRoutine = null;

    private void Start()
    {
        spriteImage = screenshotPopup.GetChild(0).GetComponent<Image>();
        anim = screenshotPopup.GetComponent<Animation>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(DataManager.playerSettings.keyBinds["Screenshot"]))
        {
            StartCoroutine(ScreenCapture());
        }
    }

    IEnumerator ScreenCapture()
    {
        yield return new WaitForEndOfFrame();
        int width = Screen.width;
        int height = Screen.height;
        Texture2D screenshotTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Rect rect = new Rect(0, 0, width, height);
        screenshotTexture.ReadPixels(rect, 0, 0);
        screenshotTexture.Apply();

        byte[] byteArray = screenshotTexture.EncodeToPNG();
        string fileName = "screenshot" + width + "x" + height + "_" + System.DateTime.Now.ToString("MM-dd-yyyy_HH-mm-ss");
        Debug.Log(fileName);
        System.IO.File.WriteAllBytes(Application.dataPath + "/Screenshots/" + fileName + ".png", byteArray);

        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
        }

        popupRoutine = StartCoroutine(ShowScreenShotPopup(screenshotTexture));
    }

    IEnumerator ShowScreenShotPopup(Texture2D screenshotTexture)
    {
        if (anim.isPlaying)
        {
            anim.Stop();
        }
        screenshotPopup.gameObject.SetActive(true);
        spriteImage.sprite = CustomMath.ImageToSprite(screenshotTexture);
        anim.Play();
        yield return new WaitUntil(() => !anim.isPlaying);
        screenshotPopup.gameObject.SetActive(false);
        popupRoutine = null;
    }

    public void OpenScreenshotFolder()
    {
        Application.OpenURL("file://" + Application.dataPath + "/Screenshots/");
    }
}
