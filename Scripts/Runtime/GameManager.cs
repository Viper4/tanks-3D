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
using System.Collections.Generic;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    public bool canceledConnect = false;
    public bool frozen;
    public bool autoPlay;
    public bool inLobby;
    public bool reachedLastLevel = false;
    public bool editing = false;
    public bool playMode = false;

    public bool paused = false;

    bool loadingScene = false;

    Coroutine autoPlayRoutine;

    public Transform loadingScreen;
    public List<string> editorNames = new List<string>();
    public List<GameObject> editorPrefabs = new List<GameObject>();
    [SerializeField] Transform progressBar;
    [SerializeField] Transform label;
    [SerializeField] Transform extraLifePopup;
    [SerializeField] Transform startButton;
    [SerializeField] Transform readyButton;
    [SerializeField] TextMeshProUGUI readyPlayersCounter;

    [SerializeField] GameObject[] customPhotonPrefabPool;

    PhotonView playerPV;
    BaseUI baseUI;
    public bool canSpawn = true; 

    public Scene currentScene;

    public readonly int multiplayerSceneIndexEnd = 7;

    public int totalLives = -1;
    int readyPlayers = 0;
    bool ready = false;

    public List<int> destroyedTanks = new List<int>();

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        SaveSystem.Init();
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 10;
        if(Instance == null)
        {
            PhotonNetwork.EnableCloseConnection = true;
            PhotonNetwork.OfflineMode = true;
            Instance = this;
            DontDestroyOnLoad(transform);

            OnSceneLoad();
        }
        else if(Instance != this)
        {
            Instance.autoPlay = autoPlay;
            Instance.inLobby = inLobby;
            Instance.editing = editing;
            Instance.frozen = frozen;
            Instance.OnSceneLoad();

            Destroy(gameObject);
        }
    }

    public void UpdatePlayerVariables(PhotonView PV)
    {
        playerPV = PV;
        baseUI = PV.transform.Find("Player UI").GetComponent<BaseUI>();
    }

    public void OnSceneLoad()
    {
        StopAllCoroutines();
        if(PhotonNetwork.PrefabPool is DefaultPool pool && customPhotonPrefabPool != null)
        {
            foreach(GameObject prefab in customPhotonPrefabPool)
            {
                if(!pool.ResourceCache.ContainsKey(prefab.name))
                {
                    pool.ResourceCache.Add(prefab.name, prefab);
                }
            }
        }

        loadingScene = false;
        currentScene = SceneManager.GetActiveScene();
        Time.timeScale = 1;
        canSpawn = true;

        if (!editing)
        {
            if (PhotonNetwork.OfflineMode)
            {
                baseUI = FindObjectOfType<BaseUI>();
            }

            if (inLobby)
            {
                UpdatePlayerWithSettings(null);
                loadingScreen.gameObject.SetActive(false);
                if(autoPlay)
                    ResetAutoPlay(2.5f);
            }
            else
            {
                switch (currentScene.name)
                {
                    case "End Scene":
                        baseUI = GameObject.Find("End UI").GetComponent<BaseUI>();
                        Text labelText = baseUI.UIElements["EndMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>();
                        Transform stats = baseUI.UIElements["StatsMenu"].Find("Stats");
                        loadingScreen.gameObject.SetActive(false);

                        if (!PhotonNetwork.OfflineMode && !PhotonNetwork.IsMasterClient)
                        {
                            baseUI.UIElements["EndMenu"].Find("Restart").gameObject.SetActive(false);
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
                            else if ((int)PhotonNetwork.CurrentRoom.CustomProperties["totalLives"] > 0)
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
                        if (currentScene.buildIndex <= multiplayerSceneIndexEnd)
                        {
                            loadingScreen.gameObject.SetActive(false);
                            frozen = false;
                        }
                        else
                        {
                            int.TryParse(Regex.Match(currentScene.name, @"\d+").Value, out int levelIndex);
                            levelIndex--;
                            Time.timeScale = 0;
                            frozen = true;
                            Cursor.visible = true;
                            Cursor.lockState = CursorLockMode.None;

                            loadingScreen.gameObject.SetActive(true);
                            progressBar.gameObject.SetActive(false);
                            if (PhotonNetwork.OfflineMode)
                            {
                                startButton.gameObject.SetActive(true);
                                readyButton.gameObject.SetActive(false);
                                readyPlayersCounter.gameObject.SetActive(false);
                                if (DataManager.playerData.previousSceneIndex != currentScene.buildIndex && levelIndex != 0 && levelIndex % 5 == 0)
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
                                    { "started", false }
                                };
                                startButton.gameObject.SetActive(false);
                                readyButton.gameObject.SetActive(true);
                                readyButton.GetComponent<Image>().color = Color.red;
                                ready = false;
                                readyPlayers = 0;
                                readyPlayersCounter.text = "0 / " + CustomNetworkHandling.NonSpectatorList.Length;

                                totalLives = (int)PhotonNetwork.CurrentRoom.CustomProperties["totalLives"];
                                if (DataManager.playerData.previousSceneIndex != currentScene.buildIndex && levelIndex != 0 && levelIndex % 5 == 0)
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

                            if (TankManager.Instance == null || !TankManager.Instance.lastCampaignScene)
                            {
                                label.Find("Level").GetComponent<Text>().text = currentScene.name;
                            }
                            else
                            {
                                reachedLastLevel = true;
                                label.Find("Level").GetComponent<Text>().text = "Final " + Regex.Match(currentScene.name, @"(.*?)[ ][0-9]+$").Groups[1] + " Mission";
                            }
                            label.Find("EnemyTanks").GetComponent<Text>().text = "Enemy tanks: " + (GameObject.Find("Tanks").transform.childCount - destroyedTanks.Count);
                            DataManager.playerData.previousSceneIndex = currentScene.buildIndex;
                        }
                        break;
                }
            }
        }
    }

    public void UpdatePlayerWithSettings(Transform player)
    {
        if(player != null)
        {
            if(player.TryGetComponent<Camera>(out var camera))
            {
                camera.fieldOfView = DataManager.playerSettings.fieldOfView;
            }

            if(player.CompareTag("Player"))
            {
                string crosshairFilePath = SaveSystem.CROSSHAIR_FOLDER + DataManager.playerSettings.crosshairFileName + ".png";
                Sprite crosshair = CustomMath.ImageToSprite(crosshairFilePath);
                Transform playerUI = player.Find("Player UI");
                if(playerUI != null)
                {
                    BaseUI baseUI = playerUI.GetComponent<BaseUI>();
                    if(baseUI != null && baseUI.UIElements.ContainsKey("InGame"))
                    {
                        Transform reticle = baseUI.UIElements["InGame"].Find("Reticle");
                        reticle.GetComponent<CrosshairManager>().UpdateReticleSprite(crosshair, DataManager.playerSettings.crosshairColorIndex, DataManager.playerSettings.crosshairScale);
                    }
                }
            }
        }
        else
        {
            Camera.main.fieldOfView = DataManager.playerSettings.fieldOfView;
        }

        AudioListener.volume = DataManager.playerSettings.masterVolume / 300;
        Application.targetFrameRate = DataManager.playerSettings.targetFramerate;
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

    public void LoadScene(int sceneIndex = -1, float delay = 0, bool save = false, bool waitWhilePaused = true)
    {
        StartCoroutine(LoadSceneRoutine(sceneIndex, delay, false, save, waitWhilePaused));
    }

    public void LoadScene(string sceneName = null, float delay = 0, bool save = false, bool waitWhilePaused = true)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, delay, false, save, waitWhilePaused));
    }

    public void PhotonLoadNextScene(float delay = 0, bool save = false)
    {
        if(PhotonNetwork.IsMasterClient)
        {
            PhotonHashtable eventParameters = new PhotonHashtable()
            {
                { "sceneIndex", currentScene.buildIndex + 1 },
                { "delay", delay },
                { "save", save }
            };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions()
            {
                CachingOption = EventCaching.DoNotCache,
                InterestGroup = 0,
                TargetActors = null,
                Receivers = ReceiverGroup.All,
            };
            PhotonNetwork.RaiseEvent(EventCodes.LoadScene, eventParameters, raiseEventOptions, SendOptions.SendUnreliable);
        }
    }

    public void PhotonLoadScene(int sceneIndex = -1, float delay = 0, bool save = false)
    {
        if(PhotonNetwork.IsMasterClient)
        {
            PhotonHashtable eventParameters = new PhotonHashtable()
            {
                { "sceneIndex", sceneIndex },
                { "delay", delay },
                { "save", save }
            };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions()
            {
                CachingOption = EventCaching.DoNotCache,
                InterestGroup = 0,
                TargetActors = null,
                Receivers = ReceiverGroup.All,
            };
            PhotonNetwork.RaiseEvent(EventCodes.LoadScene, eventParameters, raiseEventOptions, SendOptions.SendUnreliable);
        }
    }

    public void PhotonLoadScene(string sceneName = null, float delay = 0, bool save = false)
    {
        if(PhotonNetwork.IsMasterClient)
        {
            PhotonHashtable eventParameters = new PhotonHashtable()
            {
                { "sceneName", sceneName },
                { "delay", delay },
                { "save", save }
            };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions()
            {
                CachingOption = EventCaching.DoNotCache,
                InterestGroup = 0,
                TargetActors = null,
                Receivers = ReceiverGroup.All,
            };
            PhotonNetwork.RaiseEvent(EventCodes.LoadScene, eventParameters, raiseEventOptions, SendOptions.SendUnreliable);
        }
    }

    private IEnumerator LoadSceneRoutine(int sceneIndex, float delay, bool photon, bool save, bool waitWhilePaused)
    {
        if(!loadingScene)
        {
            loadingScene = true;
            
            if(sceneIndex < 0)
            {
                sceneIndex = currentScene.buildIndex;
            }

            if(save)
            {
                string campaign = Regex.Match(currentScene.name, @"(.*?)[ ][0-9]+$").Groups[1].ToString();
                DataManager.playerData.sceneIndex = sceneIndex;
                DataManager.playerData.SavePlayerData(campaign + "PlayerData", sceneIndex == SceneManager.sceneCountInBuildSettings - 1 && reachedLastLevel);
            }

            yield return new WaitForSecondsRealtime(delay);
            if(baseUI != null && waitWhilePaused)
            {
                yield return new WaitWhile(() => baseUI.PauseUIActive());
            }

            if(photon)
            {
                PhotonNetwork.LoadLevel(sceneIndex);

                if(!inLobby)
                {
                    loadingScreen.gameObject.SetActive(true);
                    startButton.gameObject.SetActive(false);
                    readyButton.gameObject.SetActive(false);
                    progressBar.gameObject.SetActive(true);

                    float progress = Mathf.Clamp01(PhotonNetwork.LevelLoadingProgress / .9f);
                    while(progress < 1)
                    {
                        progress = Mathf.Clamp01(PhotonNetwork.LevelLoadingProgress / .9f);

                        progressBar.GetComponent<Slider>().value = progress;
                        progressBar.Find("Text").GetComponent<TextMeshProUGUI>().text = (progress * 100) + "%";
                        yield return null;
                    }
                }
            }
            else
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);

                if(!inLobby)
                {
                    loadingScreen.gameObject.SetActive(true);
                    startButton.gameObject.SetActive(false);
                    readyButton.gameObject.SetActive(false);
                    progressBar.gameObject.SetActive(true);

                    while(!asyncLoad.isDone)
                    {
                        float progress = Mathf.Clamp01(asyncLoad.progress / .9f);

                        progressBar.GetComponent<Slider>().value = progress;
                        progressBar.Find("Text").GetComponent<TextMeshProUGUI>().text = (progress * 100) + "%";
                        yield return null;
                    }
                }
            }
        }
    }

    private IEnumerator LoadSceneRoutine(string sceneName, float delay, bool photon, bool save, bool waitWhilePaused)
    {
        if(!loadingScene)
        {
            loadingScene = true;

            if(sceneName == null)
            {
                sceneName = currentScene.name;
            }

            if(save)
            {
                string campaign = Regex.Match(currentScene.name, @"(.*?)[ ][0-9]+$").Groups[1].ToString();
                DataManager.playerData.sceneIndex = SceneManager.GetSceneByName(sceneName).buildIndex;
                DataManager.playerData.SavePlayerData(campaign + "PlayerData", sceneName == "End Scene" && reachedLastLevel);
            }

            yield return new WaitForSecondsRealtime(delay);
            if(baseUI != null && waitWhilePaused)
            {
                yield return new WaitWhile(() => baseUI.PauseUIActive());
            }

            if(photon)
            {
                PhotonNetwork.LoadLevel(sceneName);

                if(!inLobby)
                {
                    loadingScreen.gameObject.SetActive(true);
                    startButton.gameObject.SetActive(false);
                    readyButton.gameObject.SetActive(false);
                    progressBar.gameObject.SetActive(true);

                    float progress = Mathf.Clamp01(PhotonNetwork.LevelLoadingProgress / .9f);
                    while(progress < 1)
                    {
                        progress = Mathf.Clamp01(PhotonNetwork.LevelLoadingProgress / .9f);

                        progressBar.GetComponent<Slider>().value = progress;
                        progressBar.Find("Text").GetComponent<TextMeshProUGUI>().text = progress * 100 + "%";
                        yield return null;
                    }
                }
            }
            else
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

                if(!inLobby)
                {
                    loadingScreen.gameObject.SetActive(true);
                    startButton.gameObject.SetActive(false);
                    readyButton.gameObject.SetActive(false);
                    progressBar.gameObject.SetActive(true);

                    while(!asyncLoad.isDone)
                    {
                        float progress = Mathf.Clamp01(asyncLoad.progress / .9f);

                        progressBar.GetComponent<Slider>().value = progress;
                        progressBar.Find("Text").GetComponent<TextMeshProUGUI>().text = progress * 100 + "%";
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

    public void ResetAutoPlay(float startDelay = 0)
    {
        if(autoPlayRoutine != null)
        {
            StopCoroutine(autoPlayRoutine);
        }
        autoPlayRoutine = StartCoroutine(AutoPlay(startDelay));
    }

    private IEnumerator AutoPlay(float startDelay = 0)
    {
        yield return new WaitForEndOfFrame(); // Waiting for scripts and scene to fully load

        Time.timeScale = 1;
        frozen = true;
        LevelGenerator levelGenerator = FindObjectOfType<LevelGenerator>();
        if(levelGenerator != null)
        {
            levelGenerator.Generate();
        }
        TankManager.Instance.GenerateTanks();
        yield return new WaitForSecondsRealtime(startDelay);
        frozen = false;

        yield return new WaitForSecondsRealtime(60);
        ResetAutoPlay();
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
        PhotonTeam team = PhotonNetwork.LocalPlayer.GetPhotonTeam();

        if(PhotonNetwork.OfflineMode)
        {
            baseUI.GetComponent<PlayerUI>().Resume();
        }
        else if((team == null || team.Name != "Spectators") && playerPV.IsMine)
        {
            baseUI.GetComponent<PlayerUI>().Resume();
        }

        yield return new WaitForSecondsRealtime(3);
        if(PhotonNetwork.OfflineMode)
        {
            yield return new WaitWhile(() => baseUI.PauseUIActive());
        }
        frozen = false;
    }

    public void ToggleReady()
    {
        if(playerPV.IsMine)
        {
            Image readyImage = readyButton.GetComponent<Image>();

            if(ready)
            {
                readyPlayers--;
                PhotonNetwork.RaiseEvent(EventCodes.RemoveReadyPlayer, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
                readyImage.color = Color.red;
                ready = false;
            }
            else
            {
                readyPlayers++;
                PhotonNetwork.RaiseEvent(EventCodes.AddReadyPlayer, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
                readyImage.color = Color.green;
                ready = true;
            }

            int nonSpectatorsLength = CustomNetworkHandling.NonSpectatorList.Length;
            if(readyPlayers >= nonSpectatorsLength)
            {
                PhotonNetwork.RaiseEvent(EventCodes.StartGame, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
                StartGame();
                PhotonHashtable roomProperties = new PhotonHashtable()
                {
                    { "started", true }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
            }

            readyPlayersCounter.text = readyPlayers + " / " + nonSpectatorsLength;
        }
    }

    public void MainMenu()
    {
        StopAllLoadRoutines();
        if(PhotonNetwork.OfflineMode)
        {
            LoadScene("Main Menu", 0, false, false);
        }
        else
        {
            PhotonNetwork.Disconnect();
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
        if(eventData.Code == EventCodes.StartGame)
        {
            readyButton.GetComponent<Image>().color = Color.red;
            readyPlayers = 0;
            StartGame();
        }
        else if(eventData.Code == EventCodes.AddReadyPlayer)
        {
            readyPlayers++;
            readyPlayersCounter.text = readyPlayers + " / " + CustomNetworkHandling.NonSpectatorList.Length;
        }
        else if(eventData.Code == EventCodes.RemoveReadyPlayer)
        {
            readyPlayers--;
            readyPlayersCounter.text = readyPlayers + " / " + CustomNetworkHandling.NonSpectatorList.Length;
        }
        else if(eventData.Code == EventCodes.ResetData)
        {
            PhotonHashtable parameters = (PhotonHashtable)eventData.Parameters[ParameterCode.Data];
            DataManager.playerData = SaveSystem.ResetPlayerData((string)parameters["fileName"]);
        }
        else if(eventData.Code == EventCodes.LoadScene)
        {
            PhotonHashtable parameters = (PhotonHashtable)eventData.Parameters[ParameterCode.Data];
            if(parameters.ContainsKey("sceneIndex"))
            {
                StartCoroutine(LoadSceneRoutine((int)parameters["sceneIndex"], (float)parameters["delay"], true, (bool)parameters["save"], false));
            }
            else if(parameters.ContainsKey("sceneName"))
            {
                StartCoroutine(LoadSceneRoutine((string)parameters["sceneName"], (float)parameters["delay"], true, (bool)parameters["save"], false));
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        if(canceledConnect)
        {
            PhotonNetwork.Disconnect();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if(currentScene.name == "End Scene" && PhotonNetwork.IsMasterClient)
        {
            baseUI.UIElements["EndMenu"].Find("Restart").gameObject.SetActive(true);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if(PhotonNetwork.IsMasterClient)
        {
            if(DataManager.chatSettings.whitelistActive)
            {
                if(!DataManager.chatSettings.whitelist.Contains(newPlayer.UserId))
                {
                    PhotonNetwork.CloseConnection(newPlayer);
                    return;
                }
            }
            if(DataManager.chatSettings.blacklist.Contains(newPlayer.UserId))
            {
                PhotonNetwork.CloseConnection(newPlayer);
                return;
            }
        }
        if(currentScene.name != "Waiting Room")
        {
            if(PhotonNetwork.IsMasterClient)
            {
                newPlayer.AllocatePlayerToTeam();
            }
            readyPlayersCounter.text = readyPlayers + " / " + CustomNetworkHandling.NonSpectatorList.Length;
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if(currentScene.buildIndex > multiplayerSceneIndexEnd)
        {
            readyPlayers = 0;
            if(ready)
            {
                readyPlayers++;
                PhotonNetwork.RaiseEvent(EventCodes.AddReadyPlayer, null, RaiseEventOptions.Default, SendOptions.SendUnreliable);
            }
            readyPlayersCounter.text = readyPlayers + " / " + CustomNetworkHandling.NonSpectatorList.Length;
        }
    }

    public override void OnLeftRoom()
    {
        PhotonChatController.Instance.UnsubscribeFromRoomChannel();
        SceneManager.LoadScene("Lobby");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        PhotonChatController.Instance.Disconnect();
        canceledConnect = false;
        PhotonNetwork.OfflineMode = true;
        SceneManager.LoadScene("Main Menu");
    }
}
