using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankManager : MonoBehaviour
{
    public void StartCheckTankCount()
    {
        StartCoroutine(CheckTankCount());
    }

    // Have to wait before checking childCount since mines can blow up multiple tanks simultaneously
    IEnumerator CheckTankCount()
    {
        yield return new WaitForEndOfFrame();
        if (!GameManager.autoPlay)
        {
            if (transform.childCount < 1)
            {
                GameManager.frozen = true;
                GameManager.gameManager.LoadNextScene(3, true);
            }
        }
        else
        {
            if (transform.childCount < 2)
            {
                Time.timeScale = 0.2f;
                GameManager.gameManager.LoadScene(-1, 3f);
            }
        }
    }
}
