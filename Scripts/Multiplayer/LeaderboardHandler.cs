using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class LeaderboardHandler : MonoBehaviour
{
    [SerializeField] Transform leaderboardCanvas;
    [SerializeField] Transform playerList;
    [SerializeField] GameObject playerSlotPrefab;

    [SerializeField] ClientManager ClientManager;
    [SerializeField] DataSystem clientDataSystem;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (ClientManager.ViewIsMine())
        {
            if (Input.GetKeyDown(clientDataSystem.currentSettings.keyBinds["Leaderboard"]))
            {
                if (leaderboardCanvas.gameObject.activeSelf)
                {
                    leaderboardCanvas.gameObject.SetActive(false);
                }
                else
                {
                    leaderboardCanvas.gameObject.SetActive(true);
                    UpdateLeaderboard();
                }
            }
        }
    }

    void UpdateLeaderboard()
    {
        foreach(Transform child in playerList)
        {
            Destroy(child.gameObject);
        }

        DataSystem[] allDataSystems = FindObjectsOfType<DataSystem>();

        Dictionary<string, int> playerKillPair = new Dictionary<string, int>();
        Dictionary<string, DataSystem> playerDataPair = new Dictionary<string, DataSystem>();
        foreach (DataSystem dataSystem in allDataSystems)
        {
            string username = dataSystem.GetComponent<PhotonView>().Owner.NickName;
            if (playerKillPair.ContainsKey(username))
            {
                for (int i = 0; i < 10; i++)
                {
                    string newUsername = username + " (" + i + ")";
                    if (!playerKillPair.ContainsKey(newUsername))
                    {
                        playerKillPair[newUsername] = dataSystem.currentPlayerData.kills;
                        playerDataPair[newUsername] = dataSystem;
                        break;
                    }
                }
            }
            else
            {
                playerKillPair[username] = dataSystem.currentPlayerData.kills;
                playerDataPair[username] = dataSystem;
            }
        }

        List<KeyValuePair<string, int>> orderedDictionary = playerKillPair.ToList();

        orderedDictionary.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

        for (int i = 0; i < orderedDictionary.Count; i++)
        {
            GameObject newPlayerSlot = Instantiate(playerSlotPrefab, playerList);
            int kills = orderedDictionary[i].Value;
            int deaths = playerDataPair[orderedDictionary[i].Key].currentPlayerData.deaths;
            float KD = deaths == 0 ? kills : (Mathf.Round((float)kills / deaths * 100) / 100);

            newPlayerSlot.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = orderedDictionary[i].Key;
            newPlayerSlot.transform.Find("Kills").GetComponent<TextMeshProUGUI>().text = kills.ToString();
            newPlayerSlot.transform.Find("Deaths").GetComponent<TextMeshProUGUI>().text = deaths.ToString();
            newPlayerSlot.transform.Find("KD").GetComponent<TextMeshProUGUI>().text = KD.ToString();
        }
    }
}
