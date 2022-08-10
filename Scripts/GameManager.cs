using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using MyUnityAddons.CustomPhoton;
using MyUnityAddons.Math;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using ExitGames.Client.Photon;
using Photon.Realtime;
using System.Text.RegularExpressions;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager gameManager;

    public bool offlineMode = true;
    public static bool frozen;
    public static bool autoPlay;

    bool loadingScene = false;

    readonly byte StartEventCode = 2;
    public readonly byte LoadSceneEventCode = 3;

    public Transform loadingScreen;
    [SerializeField] Transform progressBar;
    [SerializeField] Transform label;
    [SerializeField] Transform extraLifePopup;
    [SerializeField] Transform startButton;
    [SerializeField] Transform readyButton;

    PhotonView playerPV;
    DataManager dataManager;
    BaseUIHandler baseUIHandler;

    public readonly int multiplayerSceneIndexEnd = 4;

    int lastSceneIndex = -1;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        SaveSystem.Init();

        if (gameManager == null)
        {
            PhotonNetwork.OfflineMode = offlineMode;
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

    public void UpdatePlayerVariables(PhotonView PV)
    {
        playerPV = PV;
        dataManager = PV.GetComponent<DataManager>();
        baseUIHandler = PV.transform.Find("Player UI").GetComponent<BaseUIHandler>();
    }

    public void OnSceneLoad()
    {
        StopAllCoroutines();
        loadingScene = false;
        string currentSceneName = SceneManager.GetActiveScene().name;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (PhotonNetwork.OfflineMode)
        {
            dataManager = FindObjectOfType<DataManager>();
            baseUIHandler = FindObjectOfType<BaseUIHandler>();
        }

        switch (currentSceneName)
        {
            case "Main Menu":
                lastSceneIndex = -1;
                PhotonNetwork.OfflineMode = true;
                offlineMode = true;
                SaveSystem.ResetPlayerData("PlayerData");                 // Resetting lives, kills, deaths, etc... but keeping bestTime

                loadingScreen.gameObject.SetActive(false);
                autoPlay = true;
                StartCoroutine(ResetAutoPlay(2.5f));
                break;
            case "Waiting Room":
                PhotonNetwork.OfflineMode = false;
                offlineMode = false;

                loadingScreen.gameObject.SetActive(false);
                autoPlay = true;
                StartCoroutine(ResetAutoPlay(2.5f));
                break;
            case "End Scene":
                if (PhotonNetwork.OfflineMode || playerPV.IsMine)
                {
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
                }
                break;
            default:
                if (currentSceneIndex <= multiplayerSceneIndexEnd)
                {
                    PhotonNetwork.OfflineMode = false;
                    offlineMode = false;

                    gameManager.loadingScreen.gameObject.SetActive(false);
                    Time.timeScale = 1;
                    autoPlay = false;
                    frozen = false;
                }
                else
                {
                    int.TryParse(Regex.Match(currentSceneName, @"\d+").Value, out int levelIndex);
                    levelIndex--;

                    dataManager.timing = false;
                    dataManager.currentPlayerData = SaveSystem.LoadPlayerData("PlayerData");

                    Time.timeScale = 0;
                    autoPlay = false;
                    frozen = true;

                    gameManager.loadingScreen.gameObject.SetActive(true);
                    gameManager.progressBar.gameObject.SetActive(false);
                    if (PhotonNetwork.OfflineMode)
                    {
                        gameManager.startButton.gameObject.SetActive(true);
                        readyButton.gameObject.SetActive(false);
                        if (lastSceneIndex != currentSceneIndex && levelIndex != 0 && levelIndex % 5 == 0)
                        {
                            dataManager.currentPlayerData.lives++;
                            StartCoroutine(PopupExtraLife(2.25f));
                        }

                        gameManager.label.Find("Lives").GetComponent<Text>().text = "Lives: " + dataManager.currentPlayerData.lives;
                    }
                    else
                    {
                        gameManager.readyButton.gameObject.SetActive(true);
                        startButton.gameObject.SetActive(false);
                        if (lastSceneIndex != currentSceneIndex && levelIndex != 0 && levelIndex % 5 == 0)
                        {
                            PhotonHashtable roomProperties = new PhotonHashtable
                            {
                                { "Total Lives", ((int)PhotonNetwork.CurrentRoom.CustomProperties["Total Lives"]) + 1 }
                            };
                            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
                            StartCoroutine(PopupExtraLife(2.25f));
                        }

                        gameManager.label.Find("Lives").GetComponent<Text>().text = "Lives: " + (int)PhotonNetwork.CurrentRoom.CustomProperties["Total Lives"];
                    }

                    if (!FindObjectOfType<TankManager>().lastCampaignScene)
                    {
                        gameManager.label.Find("Level").GetComponent<Text>().text = currentSceneName;
                    }
                    else
                    {
                        gameManager.label.Find("Level").GetComponent<Text>().text = "Final " + Regex.Match(currentSceneName, @"(.*?)[ ][0-9]+$").Groups[1] + " Mission";
                    }
                    gameManager.label.Find("EnemyTanks").GetComponent<Text>().text = "Enemy tanks: " + GameObject.Find("Tanks").transform.childCount;
                }
                lastSceneIndex = currentSceneIndex;
                break;
        }
    }

    // Outside classes can't start coroutines here
    public void LoadNextScene(float delay = 0, bool save = false)
    {
        int activeSceneIndex = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(LoadSceneRoutine(activeSceneIndex + 1, delay, false, save));
    }
    public void PhotonLoadNextScene(float delay = 0, bool save = false)
    {
        int activeSceneIndex = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(LoadSceneRoutine(activeSceneIndex + 1, delay, true, save));
    }

    public void LoadScene(int sceneIndex = -1, float delay = 0, bool save = false, bool waitWhilePaused = true)
    {
        StartCoroutine(LoadSceneRoutine(sceneIndex, delay, false, save, waitWhilePaused));
    }

    public void LoadScene(string sceneName = null, float delay = 0, bool save = false, bool waitWhilePaused = true)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, delay, false, save, waitWhilePaused));
    }

    public void PhotonLoadScene(int sceneIndex = -1, float delay = 0, bool save = false, bool waitWhilePaused = true)
    {
        StartCoroutine(LoadSceneRoutine(sceneIndex, delay, true, save, waitWhilePaused));
    }

    public void PhotonLoadScene(string sceneName = null, float delay = 0, bool save = false, bool waitWhilePaused = true)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, delay, true, save, waitWhilePaused));
    }

    private IEnumerator LoadSceneRoutine(int sceneIndex, float delay, bool photon, bool save = false, bool waitWhilePaused = true)
    {
        if (!loadingScene)
        {
            loadingScene = true;
            
            if (sceneIndex < 0)
            {
                sceneIndex = SceneManager.GetActiveScene().buildIndex;
            }

            if (dataManager != null && save)
            {
                dataManager.currentPlayerData.SavePlayerData("PlayerData", sceneIndex == SceneManager.sceneCountInBuildSettings - 1);
            }

            yield return new WaitForSecondsRealtime(delay);
            if (baseUIHandler != null && waitWhilePaused)
            {
                yield return new WaitWhile(() => baseUIHandler.PauseUIActive());
            }

            if (photon)
            {
                PhotonNetwork.LoadLevel(sceneIndex);

                if (!autoPlay)
                {
                    loadingScreen.gameObject.SetActive(true);
                    startButton.gameObject.SetActive(false);
                    readyButton.gameObject.SetActive(false);
                    progressBar.gameObject.SetActive(true);

                    float progress = Mathf.Clamp01(PhotonNetwork.LevelLoadingProgress / .9f);
                    while (progress < 1)
                    {
                        progress = Mathf.Clamp01(PhotonNetwork.LevelLoadingProgress / .9f);

                        progressBar.GetComponent<Slider>().value = progress;
                        progressBar.Find("Text").GetComponent<Text>().text = progress * 100 + "%";
                        yield return null;
                    }
                }
            }
            else
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);

                if (!autoPlay)
                {
                    loadingScreen.gameObject.SetActive(true);
                    startButton.gameObject.SetActive(false);
                    readyButton.gameObject.SetActive(false);
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
    }

    private IEnumerator LoadSceneRoutine(string sceneName, float delay, bool photon, bool save = false, bool waitWhilePaused = true)
    {
        if (!loadingScene)
        {
            loadingScene = true;

            if (sceneName == null)
            {
                sceneName = SceneManager.GetActiveScene().name;
            }

            if (dataManager != null && save)
            {
                dataManager.currentPlayerData.SavePlayerData("PlayerData", sceneName == "End Scene");
            }

            yield return new WaitForSecondsRealtime(delay);
            if (baseUIHandler != null && waitWhilePaused)
            {
                yield return new WaitWhile(() => baseUIHandler.PauseUIActive());
            }

            if (photon)
            {
                PhotonNetwork.LoadLevel(sceneName);

                if (!autoPlay)
                {
                    loadingScreen.gameObject.SetActive(true);
                    startButton.gameObject.SetActive(false);
                    readyButton.gameObject.SetActive(false);
                    progressBar.gameObject.SetActive(true);

                    float progress = Mathf.Clamp01(PhotonNetwork.LevelLoadingProgress / .9f);
                    while (progress < 1)
                    {
                        progress = Mathf.Clamp01(PhotonNetwork.LevelLoadingProgress / .9f);

                        progressBar.GetComponent<Slider>().value = progress;
                        progressBar.Find("Text").GetComponent<Text>().text = progress * 100 + "%";
                        yield return null;
                    }
                }
            }
            else
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

                if (!autoPlay)
                {
                    loadingScreen.gameObject.SetActive(true);
                    startButton.gameObject.SetActive(false);
                    readyButton.gameObject.SetActive(false);
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
    }

    IEnumerator PopupExtraLife(float delay)
    {
        extraLifePopup.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(delay);
        extraLifePopup.gameObject.SetActive(false);
    }

    public IEnumerator ResetAutoPlay(float startDelay = 0)
    {
        yield return new WaitForEndOfFrame(); // Waiting for scripts and scene to fully load

        Time.timeScale = 1;
        frozen = true;
        FindObjectOfType<LevelGenerator>().GenerateLevel();
        yield return new WaitForSecondsRealtime(startDelay);
        frozen = false;
    }

    public void StartGame()
    {
        Time.timeScale = 1;
        loadingScreen.gameObject.SetActive(false);
        StartCoroutine(DelayedStart());
    }

    public void ToggleReady()
    {
        if (playerPV.IsMine)
        {
            Image readyImage = readyButton.GetComponent<Image>();
            int readyPlayers = (int)PhotonNetwork.CurrentRoom.CustomProperties["Ready Players"];

            if (readyImage.color == Color.green)
            {
                Debug.Log("Removed players");
                readyPlayers--;
                readyImage.color = Color.red;
            }
            else
            {
                Debug.Log("Added players");
                readyPlayers++;
                readyImage.color = Color.green;
            }

            if (readyPlayers >= CustomNetworkHandling.NonSpectatorList.Length)
            {
                Debug.Log("Here");
                PhotonNetwork.RaiseEvent(StartEventCode, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
                StartGame();
                readyImage.color = Color.red;
            }
            PhotonHashtable roomProperties = new PhotonHashtable
            {
                { "Ready Players", readyPlayers }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(roomProperties);
        }
    }

    private void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
    }

    private void OnEvent(EventData eventData)
    {
        Debug.Log(eventData.Code);
        if (eventData.Code == StartEventCode)
        {
            Debug.Log("Raised StartEventCode");
            StartGame();
        }
        else if (eventData.Code == LoadSceneEventCode)
        {
            System.Collections.Hashtable parameters = (System.Collections.Hashtable)eventData.Parameters[ParameterCode.Data];
            Debug.Log("Raised LoadSceneEventCode");
            LoadScene((string)parameters["sceneName"], (int)parameters["delay"], (bool)parameters["save"], (bool)parameters["waitWhilePaused"]);
        }
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForEndOfFrame();

        if (PhotonNetwork.OfflineMode)
        {
            baseUIHandler.GetComponent<PlayerUIHandler>().Resume();
        }
        else if (playerPV.IsMine)
        {
            baseUIHandler.GetComponent<PlayerUIHandler>().Resume();
        }

        yield return new WaitForSecondsRealtime(3);
        if (PhotonNetwork.OfflineMode)
        {
            yield return new WaitWhile(() => baseUIHandler.PauseUIActive());
        }
        frozen = false;
        Time.timeScale = 1;
        dataManager.timing = true;
    }

    public void MainMenu()
    {
        if (PhotonNetwork.OfflineMode)
        {
            LoadScene("Main Menu", 0, false, false);
        }
        else
        {
            PhotonNetwork.Disconnect();
            LoadScene("Main Menu", 0, false, false);
        }
    }
}
