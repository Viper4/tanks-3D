using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader sceneLoader;

    public static bool frozen;
    public static bool autoPlay;

    bool loadingScene = false;

    Transform loadingScreen;
    Transform progressBar;
    Transform label;
    Transform startButton;

    void Start()
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
            sceneLoader.OnSceneLoad();

            Destroy(gameObject);
        }
    }

    public void OnSceneLoad()
    {
        loadingScene = false;

        if (SceneManager.GetActiveScene().buildIndex > 0)
        {
            Time.timeScale = 0;
            autoPlay = false;
            frozen = true;

            sceneLoader.loadingScreen.gameObject.SetActive(true);
            sceneLoader.progressBar.gameObject.SetActive(false);
            sceneLoader.startButton.gameObject.SetActive(true);

            sceneLoader.label.Find("Level").GetComponent<Text>().text = SceneManager.GetActiveScene().name;
            sceneLoader.label.Find("EnemyTanks").GetComponent<Text>().text = "Enemy tanks: " + GameObject.Find("Tanks").transform.childCount;
            sceneLoader.label.Find("Lives").GetComponent<Text>().text = "Lives: " + GameObject.Find("Player").GetComponent<PlayerControl>().lives;
        }
        else
        {
            autoPlay = true;
            StartCoroutine(ReloadAutoPlay());
        }
    }

    public void LoadNextScene(float delay = 0)
    {
        int activeSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (activeSceneIndex > 0)
        {
            StartCoroutine(LoadSceneRoutine(true, activeSceneIndex + 1, delay));
        }
        else
        {
            StartCoroutine(LoadSceneRoutine(false, activeSceneIndex + 1, delay));
        }
    }

    // Outside classes can't start coroutines in here for whatever reason
    public void LoadScene(bool save, int sceneIndex = -1, float delay = 0)
    {
        StartCoroutine(LoadSceneRoutine(save, sceneIndex, delay));
    }

    private IEnumerator LoadSceneRoutine(bool save, int sceneIndex, float delay)
    {
        if (!loadingScene)
        {
            loadingScene = true;

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

            if (!autoPlay)
            {
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
        }
    }

    private IEnumerator ReloadAutoPlay(float startDelay = 0)
    {
        yield return new WaitForEndOfFrame(); // Waiting for scripts and scene to fully load
        loadingScreen.gameObject.SetActive(false);

        Time.timeScale = 0;
        frozen = true;
        GameObject.Find("Level").GetComponent<LevelGenerator>().GenerateLevel();
        yield return new WaitForSecondsRealtime(startDelay);
        Time.timeScale = 1;
        frozen = false;
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
}
