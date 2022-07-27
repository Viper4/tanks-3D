using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using Photon.Realtime;
using MyUnityAddons.CustomPhoton;
using Photon.Pun.UtilityScripts;

public class LeaderboardHandler : MonoBehaviour
{
    [SerializeField] Transform leaderboardCanvas;
    [SerializeField] Transform playerList;
    [SerializeField] GameObject playerSlot;
    [SerializeField] GameObject[] teamPlayerSlots;

    [SerializeField] ClientManager ClientManager;
    [SerializeField] DataManager clientDataSystem;

    // Update is called once per frame
    void LateUpdate()
    {
        if (ClientManager.PV.IsMine)
        {
            if (Input.GetKeyDown(clientDataSystem.currentPlayerSettings.keyBinds["Leaderboard"]))
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
        foreach (Transform child in playerList)
        {
            Destroy(child.gameObject);
        }

        Dictionary<string, LeaderboardData> leaderboard = new Dictionary<string, LeaderboardData>();

        foreach (Player player in CustomNetworkHandling.NonSpectatorList)
        {
            PlayerData playerData = player.PhotonViewInScene().GetComponent<DataManager>().currentPlayerData;
            LeaderboardData leaderboardData = new LeaderboardData()
            {
                kills = playerData.kills,
                deaths = playerData.deaths,
                teamName = player.GetPhotonTeam().Name,
            };

            leaderboardData.KD = playerData.deaths == 0 ? playerData.kills : (Mathf.Round((float)playerData.kills / playerData.deaths * 100) / 100);

            leaderboard[player.NickName] = leaderboardData;
        }

        List<KeyValuePair<string, LeaderboardData>> orderedDictionary = leaderboard.ToList();

        orderedDictionary.Sort((pair1, pair2) => pair1.Value.kills.CompareTo(pair2.Value.kills));

        for (int i = 0; i < orderedDictionary.Count; i++)
        {
            string username = orderedDictionary[i].Key;
            LeaderboardData leaderboardData = orderedDictionary[i].Value;

            GameObject newPlayerSlot;

            switch (leaderboardData.teamName)
            {
                case "Team 1":
                    newPlayerSlot = Instantiate(teamPlayerSlots[0], playerList);
                    break;
                case "Team 2":
                    newPlayerSlot = Instantiate(teamPlayerSlots[1], playerList);
                    break;
                case "Team 3":
                    newPlayerSlot = Instantiate(teamPlayerSlots[2], playerList);
                    break;
                case "Team 4":
                    newPlayerSlot = Instantiate(teamPlayerSlots[3], playerList);
                    break;
                default:
                    newPlayerSlot = Instantiate(playerSlot, playerList);
                    break;
            }
            newPlayerSlot.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = username;
            newPlayerSlot.transform.Find("Kills").GetComponent<TextMeshProUGUI>().text = leaderboardData.kills.ToString();
            newPlayerSlot.transform.Find("Deaths").GetComponent<TextMeshProUGUI>().text = leaderboardData.deaths.ToString();
            newPlayerSlot.transform.Find("KD").GetComponent<TextMeshProUGUI>().text = leaderboardData.KD.ToString();
        }
    }

    private struct LeaderboardData
    {
        public int kills;
        public int deaths;
        public float KD;
        public string teamName;
    }
}
