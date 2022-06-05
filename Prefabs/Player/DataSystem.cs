using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataSystem : MonoBehaviour
{
    public Settings currentSettings = new Settings();
    public PlayerData currentPlayerData = new PlayerData();

    public bool timing = false;

    private void Start()
    {
        SaveSystem.LoadSettings("Settings.json", this);
    }

    // Update is called once per frame
    void Update()
    {
        if (timing)
        {
            currentPlayerData.time += Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            DataSystem[] dataSystems = FindObjectsOfType<DataSystem>();
            foreach(DataSystem dataSystem in dataSystems)
            {
                Debug.Log(dataSystem.currentPlayerData.deaths);
            }
        }
    }
}
