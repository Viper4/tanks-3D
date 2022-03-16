using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Runtime.Serialization.Formatters.Binary;

public class SceneLoader : MonoBehaviour
{
    Transform loadingScreen;
    Transform progressBar;
    Transform label;
    Transform startButton;

    void Awake()
    {
        SaveSystem.Init();
    }

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(transform);
        loadingScreen = transform.Find("LoadingScreen");
        progressBar = loadingScreen.Find("Progress Bar");
        label = loadingScreen.Find("LabelBackground");
        startButton = loadingScreen.Find("Start");
    }

    public void LoadNextScene()
    {
        StartCoroutine(LoadScene(SceneManager.GetActiveScene().buildIndex + 1));
    }

    public IEnumerator LoadScene(int sceneIndex)
    {
        Debug.Log("LoadScene called");

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);

        loadingScreen.gameObject.SetActive(true);
        startButton.gameObject.SetActive(false);
        progressBar.gameObject.SetActive(true);

        label.Find("Level").GetComponent<Text>().text = "Level " + (sceneIndex + 1).ToString();
        // Removing one for the player tank
        label.Find("Tanks").GetComponent<Text>().text = "Enemy tanks: " + (GameObject.FindGameObjectsWithTag("Tank").Length - 1);
        label.Find("Lives").GetComponent<Text>().text = "Lives: " + GameObject.Find("Player").GetComponent<PlayerControl>().lives;

        //Changed !asyncLoad.isDone to progress because it doesn't let code run after the loop run

        float progress = 0;
        while (progress < 1)
        {
            progress = Mathf.Clamp01(asyncLoad.progress / .9f);

            progressBar.GetComponent<Slider>().value = progress;
            progressBar.Find("Text").GetComponent<Text>().text = progress * 100 + "%";
            yield return null;
        }
        Debug.Log("Got here");
        StartCoroutine(AfterLoad());
    }

    IEnumerator AfterLoad()
    {
        Debug.Log("After Load");
        Time.timeScale = 0;

        progressBar.gameObject.SetActive(false);
        startButton.gameObject.SetActive(true);
        yield return null;
    }

    public void StartGame()
    {
        loadingScreen.gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    public bool CurrentSceneLoaded()
    {
        return SceneManager.GetActiveScene().isLoaded;
    }
}
