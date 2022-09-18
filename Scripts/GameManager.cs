using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using MyUnityAddons.CustomPhoton;
using MyUnityAddons.Calculations;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using ExitGames.Client.Photon;
using Photon.Realtime;
using System.Text.RegularExpressions;
using TMPro;
using Photon.Pun.UtilityScripts;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    public bool offlineMode = true;
    public bool frozen;
    public bool autoPlay;
    public bool inLobby;
    public bool reachedLastLevel = false;

    bool loadingScene = false;

    readonly byte StartEventCode = 3;
    public readonly byte LoadSceneEventCode = 4;
    readonly byte AddReadyPlayerCode = 5;
    readonly byte RemoveReadyPlayerCode = 6;
    public readonly byte DestroyCode = 7;
    public readonly byte ResetDataCode = 8;

    public Transform loadingScreen;
    [SerializeField] Transform progressBar;
    [SerializeField] Transform label;
    [SerializeField] Transform extraLifePopup;
    [SerializeField] Transform startButton;
    [SerializeField] Transform readyButton;
    [SerializeField] TextMeshProUGUI readyPlayersCounter;

    PhotonView playerPV;
    BaseUIHandler baseUIHandler;

    public Scene currentScene;

    public readonly int multiplayerSceneIndexEnd = 5;
    int previousLevelIndex = -1;

    public int totalLives = -1;
    int readyPlayers = 0;
    bool ready = false;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        SaveSystem.Init();
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 10;
        if (Instance == null)
        {
            PhotonNetwork.OfflineMode = offlineMode;
            Instance = this;
            DontDestroyOnLoad(transform);

            OnSceneLoad();
        }
        else if (Instance != this)
        {
            Instance.OnSceneLoad();

            Destroy(gameObject);
        }
    }

    public void UpdatePlayerVariables(PhotonView PV)
    {
        playerPV = PV;
        baseUIHandler = PV.transform.Find("Player UI").GetComponent<BaseUIHandler>();
    }

    public void OnSceneLoad()
    {
        StopAllCoroutines();
        loadingScene = false;
        currentScene = SceneManager.GetActiveScene();
        Time.timeScale = 1;

        if (PhotonNetwork.OfflineMode)
        {
            baseUIHandler = FindObjectOfType<BaseUIHandler>();
        }

        switch (currentScene.name)
        {
            case "Main Menu":
                PhotonNetwork.OfflineMode = true;
                offlineMode = true;
                loadingScreen.gameObject.SetActive(false);
                autoPlay = true;
                inLobby = true;
                StartCoroutine(ResetAutoPlay(2.5f));
                break;
            case "Waiting Room":
                PhotonNetwork.OfflineMode = false;
                offlineMode = false;
                loadingScreen.gameObject.SetActive(false);
                autoPlay = true;
                inLobby = true;
                StartCoroutine(ResetAutoPlay(2.5f));
                break;
            case "End Scene":
                baseUIHandler = GameObject.Find("End UI").GetComponent<BaseUIHandler>();
                Text labelText = baseUIHandler.UIElements["EndMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>();
                Transform stats = baseUIHandler.UIElements["StatsMenu"].Find("Stats");
                loadingScreen.gameObject.SetActive(false);

                if (!PhotonNetwork.OfflineMode && !PhotonNetwork.IsMasterClient)
                {
                    baseUIHandler.UIElements["EndMenu"].Find("Restart").gameObject.SetActive(false);
                }

                labelText.text = "Game over";
                if (reachedLastLevel)
                {
                    if (PhotonNetwork.OfflineMode)
                    {
                        if (DataManager.playerData.lives > 0)
                        {
                            labelText.text = "Campaign complete!";
                        }
                    }
                    else if ((int)PhotonNetwork.CurrentRoom.CustomProperties["Total Lives"] > 0)
                    {
                        labelText.text = "Campaign complete!";
                    }
                }

                stats.Find("Time").GetComponent<Text>().text = "Time: " + DataManager.playerData.time.FormattedTime();
                stats.Find("Best Time").GetComponent<Text>().text = "Best Time: " + DataManager.playerData.bestTime.FormattedTime();

                if (DataManager.playerData.kills > 0)
                {
                    float accuracy = 1;
                    if (DataManager.playerData.shots != 0)
                    {
                        accuracy = Mathf.Clamp((float)DataManager.playerData.kills / DataManager.playerData.shots, 0, 1);
                    }
                    stats.Find("Accuracy").GetComponent<Text>().text = "Accuracy: " + (Mathf.Round(accuracy * 10000) / 100).ToString() + "%";
                    stats.Find("Kills").GetComponent<Text>().text = "Kills: " + DataManager.playerData.kills;
                    if (DataManager.playerData.deaths == 0)
                    {
                        stats.Find("KD Ratio").GetComponent<Text>().text = "KD Ratio: " + DataManager.playerData.kills.ToString();
                    }
                    else
                    {
                        stats.Find("KD Ratio").GetComponent<Text>().text = "KD Ratio: " + ((float)DataManager.playerData.kills / DataManager.playerData.deaths).ToString();
                    }
                }

                stats.Find("Deaths").GetComponent<Text>().text = "Deaths: " + DataManager.playerData.deaths;
                break;
            default:
                autoPlay = false;
                inLobby = false;

                if (currentScene.buildIndex <= multiplayerSceneIndexEnd)
                {
                    PhotonNetwork.OfflineMode = false;
                    offlineMode = false;

                    loadingScreen.gameObject.SetActive(false);
                    frozen = false;
                }
                else
                {
                    int.TryParse(Regex.Match(currentScene.name, @"\d+").Value, out int levelIndex);
                    levelIndex--;
                    Time.timeScale = 0;
                    frozen = true;

                    loadingScreen.gameObject.SetActive(true);
                    progressBar.gameObject.SetActive(false);
                    if (PhotonNetwork.OfflineMode)
                    {
                        startButton.gameObject.SetActive(true);
                        readyButton.gameObject.SetActive(false);
                        readyPlayersCounter.gameObject.SetActive(false);
                        if (previousLevelIndex != currentScene.buildIndex && levelIndex != 0 && levelIndex % 5 == 0)
                        {
                            DataManager.playerData.lives++;
                            StartCoroutine(PopupExtraLife(2.25f));
                        }

                        label.Find("Lives").GetComponent<Text>().text = "Lives: " + DataManager.playerData.lives;
                    }
                    else
                    {
                        PhotonHashtable roomProperties = new PhotonHashtable()
                        {
                            { "Started", false }
                        };
                        startButton.gameObject.SetActive(false);
                        readyButton.gameObject.SetActive(true);
                        readyButton.GetComponent<Image>().color = Color.red;
                        ready = false;
                        readyPlayers = 0;
                        readyPlayersCounter.text = "0 / " + CustomNetworkHandling.NonSpectatorList.Length;

                        totalLives = (int)PhotonNetwork.CurrentRoom.CustomProperties["Total Lives"];
                        if (previousLevelIndex != currentScene.buildIndex && levelIndex != 0 && levelIndex % 5 == 0)
                        {
                            totalLives++;
                            StartCoroutine(PopupExtraLife(2.25f));

                            label.Find("Lives").GetComponent<Text>().text = "Lives: " + totalLives;
                            roomProperties.Add("Total Lives", totalLives);
                        }
                        else
                        {
                            label.Find("Lives").GetComponent<Text>().text = "Lives: " + totalLives;
                        }
                        if (PhotonNetwork.IsMasterClient)
                        {
                            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
                        }
                    }

                    if (!FindObjectOfType<TankManager>().lastCampaignScene)
                    {
                        label.Find("Level").GetComponent<Text>().text = currentScene.name;
                    }
                    else
                    {
                        reachedLastLevel = true;
                        label.Find("Level").GetComponent<Text>().text = "Final " + Regex.Match(currentScene.name, @"(.*?)[ ][0-9]+$").Groups[1] + " Mission";
                    }
                    label.Find("EnemyTanks").GetComponent<Text>().text = "Enemy tanks: " + GameObject.Find("Tanks").transform.childCount;
                    previousLevelIndex = currentScene.buildIndex;
                }
                break;
        }
    }

    public void StopAllLoadRoutines()
    {
        StopAllCoroutines();
        loadingScene = false;
        Time.timeScale = 1;
    }
    // Outside classes can't start coroutines here
    public void LoadNextScene(float delay = 0, bool save = false)
    {
        StartCoroutine(LoadSceneRoutine(currentScene.buildIndex + 1, delay, false, save, true));
    }
    public void PhotonLoadNextScene(float delay = 0, bool save = false)
    {
        StartCoroutine(LoadSceneRoutine(currentScene.buildIndex + 1, delay, true, save, false));
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

    private IEnumerator LoadSceneRoutine(int sceneIndex, float delay, bool photon, bool save, bool waitWhilePaused)
    {
        if (!loadingScene)
        {
            loadingScene = true;
            
            if (sceneIndex < 0)
            {
                sceneIndex = currentScene.buildIndex;
            }

            if (save)
            {
                DataManager.playerData.sceneIndex = sceneIndex;
                DataManager.playerData.SavePlayerData("PlayerData", sceneIndex == SceneManager.sceneCountInBuildSettings - 1 && reachedLastLevel);
            }

            yield return new WaitForSecondsRealtime(delay);
            if (baseUIHandler != null && waitWhilePaused)
            {
                yield return new WaitWhile(() => baseUIHandler.PauseUIActive());
            }

            if (photon)
            {
                PhotonNetwork.LoadLevel(sceneIndex);

                if (!inLobby)
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

                if (!inLobby)
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

    private IEnumerator LoadSceneRoutine(string sceneName, float delay, bool photon, bool save, bool waitWhilePaused)
    {
        if (!loadingScene)
        {
            loadingScene = true;

            if (sceneName == null)
            {
                sceneName = currentScene.name;
            }

            if (save)
            {
                DataManager.playerData.sceneIndex = SceneManager.GetSceneByName(sceneName).buildIndex;
                DataManager.playerData.SavePlayerData("PlayerData", sceneName == "End Scene" && reachedLastLevel);
            }

            yield return new WaitForSecondsRealtime(delay);
            if (baseUIHandler != null && waitWhilePaused)
            {
                yield return new WaitWhile(() => baseUIHandler.PauseUIActive());
            }

            if (photon)
            {
                PhotonNetwork.LoadLevel(sceneName);

                if (!inLobby)
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

                if (!inLobby)
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

    IEnumerator DelayedStart()
    {
        yield return new WaitForEndOfFrame();

        if (PhotonNetwork.OfflineMode)
        {
            baseUIHandler.GetComponent<PlayerUIHandler>().Resume();
        }
        else if (playerPV.IsMine)
        {
            DataManager.playerData = SaveSystem.LoadPlayerData("PlayerData");
            baseUIHandler.GetComponent<PlayerUIHandler>().Resume();
        }

        yield return new WaitForSecondsRealtime(3);
        if (PhotonNetwork.OfflineMode)
        {
            yield return new WaitWhile(() => baseUIHandler.PauseUIActive());
        }
        frozen = false;
    }

    public void ToggleReady()
    {
        if (playerPV.IsMine)
        {
            Image readyImage = readyButton.GetComponent<Image>();

            if (ready)
            {
                readyPlayers--;
                PhotonNetwork.RaiseEvent(RemoveReadyPlayerCode, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
                readyImage.color = Color.red;
                ready = false;
            }
            else
            {
                readyPlayers++;
                PhotonNetwork.RaiseEvent(AddReadyPlayerCode, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
                readyImage.color = Color.green;
                ready = true;
            }

            int nonSpectatorsLength = CustomNetworkHandling.NonSpectatorList.Length;
            if (readyPlayers >= nonSpectatorsLength)
            {
                PhotonNetwork.RaiseEvent(StartEventCode, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
                StartGame();
                PhotonHashtable roomProperties = new PhotonHashtable()
                {
                    { "Started", true }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
            }

            readyPlayersCounter.text = readyPlayers + " / " + nonSpectatorsLength;
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.MinimalTimeScaleToDispatchInFixedUpdate = 0;
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
    }

    void OnEvent(EventData eventData)
    {
        if (eventData.Code == StartEventCode)
        {
            PhotonTeam team = PhotonNetwork.LocalPlayer.GetPhotonTeam();
            if (team == null || team.Name != "Spectators")
            {
                readyButton.GetComponent<Image>().color = Color.red;
                readyPlayers = 0;
                StartGame();
            }
        }
        else if (eventData.Code == LoadSceneEventCode)
        {
            PhotonHashtable parameters = (PhotonHashtable)eventData.Parameters[ParameterCode.Data];
            if (parameters.ContainsKey("sceneName"))
            {
                PhotonLoadScene((string)parameters["sceneName"], (int)parameters["delay"], (bool)parameters["save"], (bool)parameters["waitWhilePaused"]);
            }
            else if (parameters.ContainsKey("sceneIndex"))
            {
                PhotonLoadScene((int)parameters["sceneIndex"], (int)parameters["delay"], (bool)parameters["save"], (bool)parameters["waitWhilePaused"]);
            }
        }
        else if (eventData.Code == AddReadyPlayerCode)
        {
            readyPlayers++;
            readyPlayersCounter.text = readyPlayers + " / " + CustomNetworkHandling.NonSpectatorList.Length;
        }
        else if (eventData.Code == RemoveReadyPlayerCode)
        {
            readyPlayers--;
            readyPlayersCounter.text = readyPlayers + " / " + CustomNetworkHandling.NonSpectatorList.Length;
        }
        else if (eventData.Code == ResetDataCode)
        {
            DataManager.playerData = SaveSystem.ResetPlayerData("PlayerData");
        }
    }

    public void MainMenu()
    {
        StopAllLoadRoutines();
        if (PhotonNetwork.OfflineMode)
        {
            LoadScene("Main Menu", 0, false, false);
        }
        else
        {
            PhotonNetwork.Disconnect();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (currentScene.name != "Waiting Room")
        {
            readyPlayersCounter.text = readyPlayers + " / " + CustomNetworkHandling.NonSpectatorList.Length;
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (currentScene.name == "End Scene" && PhotonNetwork.IsMasterClient)
        {
            baseUIHandler.UIElements["EndMenu"].Find("Restart").gameObject.SetActive(true);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (currentScene.buildIndex > multiplayerSceneIndexEnd)
        {
            readyPlayers = 0;
            if (ready)
            {
                readyPlayers++;
                PhotonNetwork.RaiseEvent(AddReadyPlayerCode, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
            }
            readyPlayersCounter.text = readyPlayers + " / " + CustomNetworkHandling.NonSpectatorList.Length;
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Lobby");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SceneManager.LoadScene("Main Menu");
        Debug.Log("Disconnected: " + cause.ToString());
    }
}
