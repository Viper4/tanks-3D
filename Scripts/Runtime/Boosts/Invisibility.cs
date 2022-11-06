using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Invisibility : MonoBehaviour
{
    [SerializeField] Transform disappearEffect;
    [SerializeField] Transform circleEffect;

    [SerializeField] Transform tankOrigin;
    [SerializeField] CameraControl cameraControl;

    [SerializeField] MeshRenderer[] visibleRenderers;
    [SerializeField] GameObject username;

    Coroutine invisibilityRoutine;

    [PunRPC]
    public void SetInvisible(float duration)
    {
        if (invisibilityRoutine != null)
        {
            StopCoroutine(invisibilityRoutine);
            invisibilityRoutine = null;
        }
        invisibilityRoutine = StartCoroutine(InvisibilityRoutine(duration));
    }

    IEnumerator InvisibilityRoutine(float duration)
    {
        cameraControl.invisible = true;
        Instantiate(disappearEffect, tankOrigin.position, tankOrigin.rotation);
        Instantiate(circleEffect, tankOrigin);

        for (int i = 0; i < visibleRenderers.Length; i++)
        {
            visibleRenderers[i].enabled = false;
        }
        if (username != null)
        {
            username.SetActive(false);
        }

        yield return new WaitForSeconds(duration);

        cameraControl.invisible = false;
        Instantiate(disappearEffect, tankOrigin.position, tankOrigin.rotation);

        for (int i = 0; i < visibleRenderers.Length; i++)
        {
            visibleRenderers[i].enabled = true;
        }
        if (username != null)
        {
            username.SetActive(true);
        }

        invisibilityRoutine = null;
    }
}
