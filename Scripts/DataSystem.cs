using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataSystem : MonoBehaviour
{
    public Settings currentSettings = new Settings();
    public PlayerData currentPlayerData= new PlayerData();

    public bool timing = false;

    // Update is called once per frame
    void Update()
    {
        if (timing)
        {
            currentPlayerData.time += Time.deltaTime;
        }
    }
}
