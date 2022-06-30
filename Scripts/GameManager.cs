using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using CustomExtensions;

public class GameManager : MonoBehaviour
{
    public static GameManager gameManager;

    public static bool frozen;
    public static bool autoPlay;

    bool loadingScene = false;

    public Transform loadingScreen;
    [SerializeField] Transform progressBar;
    [SerializeField] Transform label;
    [SerializeField] Transform startButton;

    DataManager dataSystem;
    BaseUIHandler baseUIHandler;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        SaveSystem.Init();

        if (gameManager == null)
        {
            gameManager = this;
            DontDestroyOnLoad(transform);

            OnSceneLoad();
        }
        else if (gameManager != this)
        {
            gameManager.OnSceneLoad();

            Destroy(gameObject);
        }
    }

    public void OnSceneLoad()
    {
        baseUIHandler = FindObjectOfType<BaseUIHandler>();
        dataSystem = FindObjectOfType<DataManager>();

        StopAllCoroutines();
        loadingScene = false;
        string currentSceneName = SceneManager.GetActiveScene().name;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        PhotonNetwork.OfflineMode = currentSceneIndex < 11;

        switch (currentSceneName)
        {
            case "Main Menu":
                loadingScreen.gameObject.SetActive(false);
                SaveSystem.ResetPlayerData("PlayerData");                 // Resetting lives, kills, deaths, etc... but keeping bestTime
                autoPlay = true;
                StartCoroutine(ReloadAutoPlay(2.5f));
                break;
            case "End Scene":
                PlayerData playerData = SaveSystem.LoadPlayerData("PlayerData");

                Transform stats = baseUIHandler.UIElements["StatsMenu"].Find("Stats");
                gameManager.loadingScreen.gameObject.SetActive(false);

                stats.Find("Time").GetComponent<Text>().text = "Time: " + playerData.time.FormattedTime();
                stats.Find("Best Time").GetComponent<Text>().text = "Best Time: " + playerData.bestTime.FormattedTime();

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
                if (currentSceneIndex > 11)
                {
                    gameManager.loadingScreen.gameObject.SetActive(false);
                    Time.timeScale = 1;
                    autoPlay = false;
                    frozen = false;
                }
                else
                {
                    dataSystem.timing = false;

                    dataSystem.currentPlayerData = SaveSystem.LoadPlayerData("PlayerData");
                    
                    Time.timeScale = 0;
                    autoPlay = false;
                    frozen = true;

                    gameManager.loadingScreen.gameObject.SetActive(true);
                    gameManager.progressBar.gameObject.SetActive(false);
                    gameManager.startButton.gameObject.SetActive(true);

                    gameManager.label.Find("Level").GetComponent<Text>().text = SceneManager.GetActiveScene().name;
                    gameManager.label.Find("EnemyTanks").GetComponent<Text>().text = "Enemy tanks: " + GameObject.Find("Tanks").transform.childCount;
                    gameManager.label.Find("Lives").GetComponent<Text>().text = "Lives: " + dataSystem.currentPlayerData.lives;
                }
                break;
        }
    }

    public void LoadNextScene(float delay = 0, bool save = false)
    {
        int activeSceneIndex = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(LoadSceneRoutine(activeSceneIndex + 1, delay, save));
    }

    // Outside classes can't start coroutines in here for whatever reason
    public void LoadScene(int sceneIndex = -1, float delay = 0, bool save = false, bool waitWhilePaused = true)
    {
        StartCoroutine(LoadSceneRoutine(sceneIndex, delay, save, waitWhilePaused));
    }

    public void LoadScene(string sceneName = null, float delay = 0, bool save = false, bool waitWhilePaused = true)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, delay, save, waitWhilePaused));
    }

    private IEnumerator LoadSceneRoutine(int sceneIndex, float delay, bool save = false, bool waitWhilePaused = true)
    {
        if (!loadingScene)
        {
            loadingScene = true;
            
            if (sceneIndex < 0)
            {
                sceneIndex = SceneManager.GetActiveScene().buildIndex;
            }

            if (dataSystem != null && save)
            {
                dataSystem.currentPlayerData.SavePlayerData("PlayerData", sceneIndex == 11);
            }

            yield return new WaitForSecondsRealtime(delay);
            if (baseUIHandler != null && waitWhilePaused)
            {
                yield return new WaitWhile(() => baseUIHandler.PauseUIActive() == true);
            }
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

    private IEnumerator LoadSceneRoutine(string sceneName, float delay, bool save = false, bool waitWhilePaused = true)
    {
        if (!loadingScene)
        {
            loadingScene = true;

            if (sceneName == null)
            {
                sceneName = SceneManager.GetActiveScene().name;
            }

            if (dataSystem != null && save)
            {
                dataSystem.currentPlayerData.SavePlayerData("PlayerData", sceneName == "End Scene");
            }

            yield return new WaitForSecondsRealtime(delay);
            if (baseUIHandler != null && waitWhilePaused)
            {
                yield return new WaitWhile(() => baseUIHandler.PauseUIActive() == true);
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
        FindObjectOfType<LevelGenerator>().GenerateLevel();
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
        yield return new WaitForEndOfFrame();
        FindObjectOfType<PlayerUIHandler>().Resume();
        yield return new WaitForSecondsRealtime(3);
        frozen = false;
        dataSystem.timing = true;
    }

    public void RestartGame()
    {
        LoadScene(0, 0, false, false);
    }
}
