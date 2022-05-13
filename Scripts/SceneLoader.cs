using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Runtime.Serialization.Formatters.Binary;

public class SceneLoader : MonoBehaviour
{
    public static Transform sceneLoader;

    Transform loadingScreen;
    Transform progressBar;
    Transform label;
    Transform startButton;

    void Awake()
    {
        Debug.Log("Got here");

        SaveSystem.Init();

        if (sceneLoader == null)
        {
            sceneLoader = transform;
            DontDestroyOnLoad(transform);

            loadingScreen = transform.Find("LoadingScreen");
            progressBar = loadingScreen.Find("Progress Bar");
            label = loadingScreen.Find("LabelBackground");
            startButton = loadingScreen.Find("Start");

            OnSceneLoad();
        }
        else if (sceneLoader != transform)
        {
            sceneLoader.GetComponent<SceneLoader>().OnSceneLoad();
            Destroy(gameObject);
        }
    }

    public void OnSceneLoad()
    {
        Time.timeScale = 0;

        progressBar.gameObject.SetActive(false);
        startButton.gameObject.SetActive(true);

        label.Find("Level").GetComponent<Text>().text = "Level " + (SceneManager.GetActiveScene().buildIndex + 1).ToString();
        label.Find("Tanks").GetComponent<Text>().text = "Enemy tanks: " + GameObject.Find("Enemies").transform.childCount;
        label.Find("Lives").GetComponent<Text>().text = "Lives: " + GameObject.Find("Player").GetComponent<PlayerControl>().lives;
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
        GameObject.Find("Player").transform.Find("UI").GetComponent<UIHandler>().Resume();
    }

    public bool CurrentSceneLoaded()
    {
        return SceneManager.GetActiveScene().isLoaded;
    }
}
