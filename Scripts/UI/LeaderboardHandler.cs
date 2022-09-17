using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using Photon.Realtime;
using MyUnityAddons.CustomPhoton;
using Photon.Pun.UtilityScripts;
using Photon.Pun;

public class LeaderboardHandler : MonoBehaviour
{
    [SerializeField] Transform leaderboardCanvas;
    [SerializeField] Transform playerList;
    [SerializeField] GameObject playerSlot;
    [SerializeField] GameObject[] teamPlayerSlots;

    [SerializeField] ClientManager clientManager;

    // Update is called once per frame
    void LateUpdate()
    {
        if (clientManager == null || clientManager.photonView.IsMine)
        {
            if (Input.GetKeyDown(DataManager.playerSettings.keyBinds["Leaderboard"]))
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

        Player[] leaderboardPlayers = ((RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"]).primaryMode == "Co-Op" ? PhotonNetwork.PlayerList : CustomNetworkHandling.NonSpectatorList;

        foreach (Player player in leaderboardPlayers)
        {
            LeaderboardData leaderboardData = new LeaderboardData()
            {
                kills = (int)player.CustomProperties["Kills"],
                deaths = (int)player.CustomProperties["Deaths"],
                teamName = player.GetPhotonTeam().Name,
            };

            leaderboardData.KD = leaderboardData.deaths == 0 ? leaderboardData.kills : (Mathf.Round((float)leaderboardData.kills / leaderboardData.deaths * 100) / 100);

            leaderboard[player.NickName] = leaderboardData;
        }

        foreach (KeyValuePair<string, LeaderboardData> slot in leaderboard.OrderByDescending((x) => x.Value.kills))
        {
            string username = slot.Key;
            LeaderboardData leaderboardData = slot.Value;
            GameObject newPlayerSlot = leaderboardData.teamName switch
            {
                "Team 1" => Instantiate(teamPlayerSlots[0], playerList),
                "Team 2" => Instantiate(teamPlayerSlots[1], playerList),
                "Team 3" => Instantiate(teamPlayerSlots[2], playerList),
                "Team 4" => Instantiate(teamPlayerSlots[3], playerList),
                _ => Instantiate(playerSlot, playerList),
            };
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
