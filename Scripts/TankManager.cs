using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartCheckTankCount()
    {
        StartCoroutine(CheckTankCount());
    }

    // Have to wait before checking childCount since mines can blow up multiple tanks simultaneously
    IEnumerator CheckTankCount()
    {
        yield return new WaitForEndOfFrame();
        if (!SceneLoader.autoPlay)
        {
            if (transform.childCount < 1)
            {
                SceneLoader.frozen = true;
                SceneLoader.sceneLoader.LoadNextScene(3, true);
            }
        }
        else
        {
            if (transform.childCount < 2)
            {
                Time.timeScale = 0.2f;
                SceneLoader.sceneLoader.LoadScene(-1, 3f);
            }
        }
    }
}
