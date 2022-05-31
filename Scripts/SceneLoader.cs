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

    public Transform loadingScreen;
    Transform progressBar;
    Transform label;
    Transform startButton;

    DataSystem dataSystem;

    BaseUIHandler baseUIHandler;

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

    public void OnSceneLoad()
    {
        dataSystem = FindObjectOfType<DataSystem>();

        StopAllCoroutines();
        loadingScene = false;
        string currentSceneName = SceneManager.GetActiveScene().name;
        baseUIHandler = FindObjectOfType<BaseUIHandler>();
        if(dataSystem != null)
        {
            SaveSystem.LoadSettings("settings.json", dataSystem.currentSettings);
        }

        switch (currentSceneName)
        {
            case "Main Menu":
                SaveSystem.SavePlayerData("PlayerData.json", SaveSystem.defaultPlayerData);                 // Resetting lives, kills, deaths, etc...
                autoPlay = true;
                StartCoroutine(ReloadAutoPlay(2.5f));
                break;
            case "End Scene":
                PlayerData playerData = new PlayerData();
                SaveSystem.LoadPlayerData("PlayerData.json", playerData);

                Transform stats = baseUIHandler.UIElements["StatsMenu"].Find("Stats");
                sceneLoader.loadingScreen.gameObject.SetActive(false);

                stats.Find("Time").GetComponent<Text>().text = "Time: " + FormattedTime(playerData.time);
                stats.Find("Best Time").GetComponent<Text>().text = "Best Time: " + FormattedTime(playerData.bestTime);

                if (playerData.kills > 0)
                {
                    float accuracy = 1;
                    if (playerData.shots != 0)
                    {
                        accuracy = Mathf.Clamp((float)playerData.kills / playerData.shots, 0, 1);
                    }
                    stats.Find("Accuracy").GetComponent<Text>().text = "Accuracy: " + (Mathf.Round(accuracy * 10000) / 100).ToString() + "%";
                    stats.Find("Kills").GetComponent<Text>().text = "Kills: " + playerData.kills;
                    if (playerData.deaths == 0)
                    {
                        stats.Find("KD Ratio").GetComponent<Text>().text = "KD Ratio: " + playerData.kills.ToString();
                    }
                    else
                    {
                        stats.Find("KD Ratio").GetComponent<Text>().text = "KD Ratio: " + ((float)playerData.kills / playerData.deaths).ToString();
                    }
                }

                stats.Find("Deaths").GetComponent<Text>().text = "Deaths: " + playerData.deaths;
                break;
            default:
                int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
                
                if (currentSceneIndex > 11)
                {
                    sceneLoader.loadingScreen.gameObject.SetActive(false);
                    Time.timeScale = 1;
                    autoPlay = false;
                    frozen = false;
                }
                else
                {
                    dataSystem.timing = false;

                    SaveSystem.LoadPlayerData("PlayerData.json", dataSystem.currentPlayerData);

                    Time.timeScale = 0;
                    autoPlay = false;
                    frozen = true;

                    sceneLoader.loadingScreen.gameObject.SetActive(true);
                    sceneLoader.progressBar.gameObject.SetActive(false);
                    sceneLoader.startButton.gameObject.SetActive(true);

                    sceneLoader.label.Find("Level").GetComponent<Text>().text = SceneManager.GetActiveScene().name;
                    sceneLoader.label.Find("EnemyTanks").GetComponent<Text>().text = "Enemy tanks: " + GameObject.Find("Tanks").transform.childCount;
                    sceneLoader.label.Find("Lives").GetComponent<Text>().text = "Lives: " + dataSystem.currentPlayerData.lives;
                }
                break;
        }
    }

    public void LoadNextScene(float delay = 0)
    {
        int activeSceneIndex = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(LoadSceneRoutine(activeSceneIndex + 1, delay));
    }

    // Outside classes can't start coroutines in here for whatever reason
    public void LoadScene(int sceneIndex = -1, float delay = 0)
    {
        StartCoroutine(LoadSceneRoutine(sceneIndex, delay));
    }

    public void LoadScene(string sceneName = null, float delay = 0)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, delay));
    }

    private IEnumerator LoadSceneRoutine(int sceneIndex, float delay)
    {
        if (!loadingScene)
        {
            /*if (dataSystem != null)
            {
                SaveSystem.SavePlayerData("PlayerData.json", dataSystem.currentPlayerData);
            }*/

            loadingScene = true;
            
            if (sceneIndex < 0)
            {
                sceneIndex = SceneManager.GetActiveScene().buildIndex;
            }

            yield return new WaitForSecondsRealtime(delay);
            yield return new WaitUntil(() => baseUIHandler.PauseUIActive() == false);
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

    private IEnumerator LoadSceneRoutine(string sceneName, float delay)
    {
        if (!loadingScene)
        {
            loadingScene = true;

            if (sceneName == null)
            {
                sceneName = SceneManager.GetActiveScene().name;
            }

            yield return new WaitForSecondsRealtime(delay);
            if(baseUIHandler != null)
            {
                yield return new WaitUntil(() => baseUIHandler.PauseUIActive() == false);
            }
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

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
        GameObject.Find("Player").transform.Find("Player UI").GetComponent<PlayerUIHandler>().Resume();
        yield return new WaitForSecondsRealtime(3);
        frozen = false;
        dataSystem.timing = true;
    }

    public void RestartGame()
    {
        LoadScene(0);
    }

    string FormattedTime(float time)
    {
        float minutes = Mathf.FloorToInt(time / 60);
        float seconds = Mathf.FloorToInt(time % 60);
        float milliSeconds = time % 1 * 1000;

        return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliSeconds);
    }
}
