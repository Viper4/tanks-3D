using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader sceneLoader;

    public static bool frozen;

    Transform loadingScreen;
    Transform progressBar;
    Transform label;
    Transform startButton;

    void Awake()
    {
        SaveSystem.Init();

        if (sceneLoader == null)
        {
            sceneLoader = this;
            DontDestroyOnLoad(transform);

            loadingScreen = transform.Find("LoadingScreen");
            progressBar = loadingScreen.Find("Progress Bar");
            label = loadingScreen.Find("LabelBackground");
            startButton = loadingScreen.Find("Start");

            OnSceneLoad();
        }
        else if (sceneLoader != this)
        {
            Destroy(gameObject);
        }
    }

    public void OnSceneLoad()
    {
        Time.timeScale = 0;
        frozen = true;

        progressBar.gameObject.SetActive(false);
        startButton.gameObject.SetActive(true);

        label.Find("Level").GetComponent<Text>().text = "Level " + (SceneManager.GetActiveScene().buildIndex + 1).ToString();
        label.Find("Tanks").GetComponent<Text>().text = "Enemy tanks: " + GameObject.Find("Tanks").transform.childCount;
        label.Find("Lives").GetComponent<Text>().text = "Lives: " + GameObject.Find("Player").GetComponent<PlayerControl>().lives;
    }

    public void LoadNextScene(float delay = 0)
    {
        int activeSceneIndex = SceneManager.GetActiveScene().buildIndex;

        StartCoroutine(LoadScene(true, activeSceneIndex + 1, delay));
    }

    public IEnumerator LoadScene(bool save, int sceneIndex = -1, float delay = 0)
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

        yield return new WaitForSecondsRealtime(delay);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);

        loadingScreen.gameObject.SetActive(true);
        startButton.gameObject.SetActive(false);
        progressBar.gameObject.SetActive(true);

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / .9f);

            progressBar.GetComponent<Slider>().value = progress;
            progressBar.Find("Text").GetComponent<Text>().text = progress * 100 + "%";
            yield return null;
        }
    }

    public void StartGame()
    {
        loadingScreen.gameObject.SetActive(false);
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        GameObject.Find("Player").transform.Find("UI").GetComponent<UIHandler>().Resume();
        yield return new WaitForSecondsRealtime(3);
        frozen = false;
    }

    public bool CurrentSceneLoaded()
    {
        return SceneManager.GetActiveScene().isLoaded;
    }
}
