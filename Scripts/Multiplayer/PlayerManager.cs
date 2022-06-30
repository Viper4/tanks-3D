using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using CustomExtensions;

public class PlayerManager : MonoBehaviour
{
    // Prefabs must be in Resources folder
    public Transform playerPrefab;

    public Transform spectatorPrefab;

    [SerializeField] Collider boundingBox;
    [SerializeField] LayerMask ignoreLayers;

    private void Start()
    {
        PhotonHashtable playerProperties = PhotonNetwork.LocalPlayer.CustomProperties;
        playerProperties.Add("Kills", 0);
        playerProperties.Add("Deaths", 0);

        PhotonNetwork.NickName = GetUniqueUsername(PhotonNetwork.NickName);

        if (PhotonExtensions.InGamePlayerList.Length < ((RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"]).playerLimit)
        {
            GameObject newPlayer = PhotonNetwork.Instantiate(playerPrefab.name, RandomExtensions.GetSpawnPointInCollider(boundingBox, Vector3.down, ignoreLayers), Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up));
            newPlayer.GetComponent<PhotonView>().RPC("RandomizeMaterialColors", RpcTarget.All);
            PhotonNetwork.LocalPlayer.TagObject = newPlayer;

            playerProperties.Add("Spectator", false);
            playerProperties.Add("ViewID", newPlayer.GetComponent<PhotonView>().ViewID);
        }
        else
        {
            GameObject newSpectator = PhotonNetwork.Instantiate(spectatorPrefab.name, Vector3.zero, Quaternion.identity);
            newSpectator.GetComponent<SpectatorControl>().playerManager = this;
            PhotonNetwork.LocalPlayer.TagObject = newSpectator;

            playerProperties.Add("Spectator", true);
            playerProperties.Add("ViewID", newSpectator.GetComponent<PhotonView>().ViewID);
        }
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
    }

    public void RespawnPlayer(Transform tankOrigin)
    {
        tankOrigin.SetPositionAndRotation(RandomExtensions.GetSpawnPointInCollider(boundingBox, Vector3.down, ignoreLayers), Quaternion.AngleAxis(Random.Range(-180, 180), Vector3.up));
    }

    string GetUniqueUsername(string username)
    {
        List<string> usernames = new List<string>();
        foreach (Player player in PhotonNetwork.PlayerListOthers)
        {
            usernames.Add(player.NickName);
        }

        if (usernames.Contains(username))
        {
            for (int i = 0; i < 20; i++)
            {
                string newUsername = username + " (" + (i + 1) + ")";
                if (!usernames.Contains(newUsername))
                {
                    return newUsername;
                }
            }
        }
        return username;
    }
}