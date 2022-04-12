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

        if (SceneManager.GetActiveScene().name != "CustomLevel")
        {
            StartCoroutine(OnSceneLoad());
        }
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
        int activeSceneIndex = SceneManager.GetActiveScene().buildIndex;

        SaveSystem.SavePlayerData("PlayerData.json", GameObject.Find("Player").GetComponent<PlayerControl>(), activeSceneIndex + 1);

        StartCoroutine(LoadScene(false, activeSceneIndex + 1));
    }

    public IEnumerator LoadScene(bool save, int sceneIndex = -1)
    {
        Debug.Log("LoadScene called");

        if (sceneIndex < 0)
        {
            sceneIndex = SceneManager.GetActiveScene().buildIndex;
        }

        if (save)
        {
            SaveSystem.SavePlayerData("PlayerData.json", GameObject.Find("Player").GetComponent<PlayerControl>(), sceneIndex);
        }

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
    }

    IEnumerator OnSceneLoad()
    {
        yield return new WaitForFixedUpdate();
        // All coroutines are stopped on scene load, so we have to do this instead
        if (SceneManager.GetActiveScene().name != "CustomLevel")
        {
            Time.timeScale = 0;

            progressBar.gameObject.SetActive(false);
            startButton.gameObject.SetActive(true);
        }
        else
        {

        }
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
