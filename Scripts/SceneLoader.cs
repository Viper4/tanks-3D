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

    bool timing = false;

    bool loadingScene = false;

    Transform loadingScreen;
    Transform progressBar;
    Transform label;
    Transform startButton;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

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

    private void Update()
    {
        if (timing)
        {
            SaveSystem.currentPlayerData.time += Time.deltaTime;
        }
    }

    public void OnSceneLoad()
    {
        timing = false;
        StopAllCoroutines();
        loadingScene = false;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (currentSceneIndex == 11)
        {
            Transform stats = BaseUIHandler.UIElements["StatsMenu"].Find("Stats");
            sceneLoader.loadingScreen.gameObject.SetActive(false);

            stats.Find("Time").GetComponent<Text>().text = "Time: " + FormattedTime(SaveSystem.currentPlayerData.time);
            stats.Find("Best Time").GetComponent<Text>().text = "Best Time: " + FormattedTime(SaveSystem.currentPlayerData.bestTime);
            float accuracy = (float)SaveSystem.currentPlayerData.kills / SaveSystem.currentPlayerData.shots;
            stats.Find("Accuracy").GetComponent<Text>().text = "Accuracy: " + (Mathf.Round(accuracy * 10000) / 100).ToString() + "%";
            stats.Find("Kills").GetComponent<Text>().text = "Kills: " + SaveSystem.currentPlayerData.kills;
            stats.Find("Deaths").GetComponent<Text>().text = "Deaths: " + SaveSystem.currentPlayerData.deaths;
            stats.Find("KD Ratio").GetComponent<Text>().text = "KD Ratio: " + ((float)SaveSystem.currentPlayerData.kills / SaveSystem.currentPlayerData.deaths).ToString();
        }
        else
        {
            SaveSystem.LoadSettings("settings.json");

            if (currentSceneIndex > 0)
            {
                Time.timeScale = 0;
                autoPlay = false;
                frozen = true;

                sceneLoader.loadingScreen.gameObject.SetActive(true);
                sceneLoader.progressBar.gameObject.SetActive(false);
                sceneLoader.startButton.gameObject.SetActive(true);

                sceneLoader.label.Find("Level").GetComponent<Text>().text = SceneManager.GetActiveScene().name;
                sceneLoader.label.Find("EnemyTanks").GetComponent<Text>().text = "Enemy tanks: " + GameObject.Find("Tanks").transform.childCount;
                sceneLoader.label.Find("Lives").GetComponent<Text>().text = "Lives: " + SaveSystem.currentPlayerData.lives;
            }
            else
            {
                SaveSystem.currentPlayerData.time = 0;
                SaveSystem.currentPlayerData.kills = 0;
                SaveSystem.currentPlayerData.shots = 0;
                SaveSystem.currentPlayerData.deaths = 0;
                SaveSystem.currentPlayerData.lives = 3;
                autoPlay = true;
                StartCoroutine(ReloadAutoPlay(2.5f));
            }
        }
    }

    public void LoadNextScene(float delay = 0)
    {
        int activeSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (activeSceneIndex == 10)
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
                SaveSystem.SavePlayerData("PlayerData.json");
            }

            yield return new WaitForSecondsRealtime(delay);
            yield return new WaitUntil(() => BaseUIHandler.PauseUIActive() == false);
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
        GameObject.Find("Player").transform.Find("UI").GetComponent<PlayerUIHandler>().Resume();
        yield return new WaitForSecondsRealtime(3);
        frozen = false;
        timing = true;
    }

    string FormattedTime(float time)
    {
        float minutes = Mathf.FloorToInt(time / 60);
        float seconds = Mathf.FloorToInt(time % 60);
        float milliSeconds = time % 1 * 1000;

        return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliSeconds);
    }
}
