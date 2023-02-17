using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Chat;
using Photon.Pun;
using ExitGames.Client.Photon;
using System.Text.RegularExpressions;
using TMPro;
using System;
using MyUnityAddons.CustomPhoton;
using Photon.Realtime;
using MyUnityAddons.Calculations;

public class PhotonChatController : MonoBehaviour, IChatClientListener
{
    public static PhotonChatController Instance;

    ChatClient chatClient;
    bool isConnected = false;
    string replyRecipient = null;

    [SerializeField] GameObject inputParent;
    [SerializeField] GameObject chatParent;
    [SerializeField] CanvasGroup chatCanvasGroup;
    [SerializeField] TMP_InputField chatInput;
    [SerializeField] RectTransform chatContent;
    [SerializeField] GameObject chatMessageTemplate;

    string roomChannel = null;

    Dictionary<string, string> IDUsernamePair = new Dictionary<string, string>();

    float timeSinceLastMessage = 0;
    public float chatVanishTime = 5;

    public bool chatBoxActive = false;

    int resetVote = 0;

    // Start is called before the first frame update
    void Start()
    {
        if(Instance == null)
        {
            Instance = this;
            chatClient = new ChatClient(this);
        }
        else
        {
            Destroy(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        chatClient.Service();

        if (isConnected)
        {
            if(!inputParent.activeSelf)
            {
                if(Input.GetKeyDown(DataManager.playerSettings.keyBinds["Chat"]))
                {
                    Pause();
                }
                else
                {
                    if(timeSinceLastMessage < chatVanishTime)
                    {
                        timeSinceLastMessage += Time.unscaledDeltaTime;
                        if(!chatParent.activeSelf)
                        {
                            chatParent.SetActive(true);
                        }
                    }
                    else if(chatParent.activeSelf)
                    {
                        chatParent.SetActive(false);
                    }
                }
            }
            else
            {
                if(Input.GetKeyDown(KeyCode.Return))
                {
                    SendMessage(chatInput);
                }
                else if(GameManager.Instance.inLobby && Input.GetKeyDown(KeyCode.Escape))
                {
                    Resume();
                }
            }
        }
    }

    public void Resume(bool changeCursor = true)
    {
        chatBoxActive = false;
        GameManager.Instance.paused = false;
        if(changeCursor && !GameManager.Instance.inLobby)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        timeSinceLastMessage = chatVanishTime;
        inputParent.SetActive(false);
        chatParent.SetActive(false);
        chatCanvasGroup.enabled = true;
    }

    public void Pause()
    {
        chatBoxActive = true;
        GameManager.Instance.paused = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        chatParent.SetActive(true);
        inputParent.SetActive(true);
        chatCanvasGroup.enabled = false;
    }

    string GetUsername(string userID)
    {
        if(IDUsernamePair.TryGetValue(userID, out string username))
        {
            return username;
        }
        else
        {
            Player player = CustomNetworkHandling.FindPlayerWithUsername(username);
            if(player != null)
            {
                IDUsernamePair.AddOrReplace(userID, player.NickName);
                return player.NickName;
            }
            else
            {
                Debug.LogWarning("Could not find username associated with \"" + userID + "\"");
                return userID;
            }
        }
    }

    string GetUserID(string username)
    {
        foreach(string userID in IDUsernamePair.Keys)
        {
            if (IDUsernamePair[userID] == username)
            {
                return userID;
            }
        }
        Player player = CustomNetworkHandling.FindPlayerWithUsername(username);
        if(player != null)
        {
            return player.UserId;
        }
        else
        {
            Debug.LogWarning("Could not find UserID associated with \"" + username + "\"");
            return username;
        }
    }

    public void ConnectToPhotonChat()
    {
        if(string.IsNullOrEmpty(DataManager.chatSettings.username))
        {
            IDUsernamePair.AddOrReplace(PhotonNetwork.LocalPlayer.UserId, PhotonNetwork.LocalPlayer.UserId);
        }
        else
        {
            IDUsernamePair.AddOrReplace(PhotonNetwork.LocalPlayer.UserId, DataManager.chatSettings.username);
        } 
        chatClient.AuthValues = new Photon.Chat.AuthenticationValues(PhotonNetwork.LocalPlayer.UserId);
        chatClient.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, PhotonNetwork.AppVersion, new Photon.Chat.AuthenticationValues(PhotonNetwork.LocalPlayer.UserId));
    }

    private void CreateMessage(string message, Color color)
    {
        timeSinceLastMessage = 0;
        TextMeshProUGUI chatMessage = Instantiate(chatMessageTemplate, chatContent).GetComponent<TextMeshProUGUI>();
        chatMessage.color = color;
        chatMessage.text = message;
    }

    public void SendPublicUserUpdate(string channel, bool queryReply = false)
    {
        chatClient.PublishMessage(channel, new object[] { 0, PhotonNetwork.LocalPlayer.UserId, DataManager.chatSettings.username, queryReply });
    }

    public void SendPublicUserUpdate(bool queryReply = false)
    {
        if (chatClient.PublicChannels.Count > 0)
        {
            foreach (string channel in chatClient.PublicChannels.Keys)
            {
                chatClient.PublishMessage(channel, new object[] { 0, PhotonNetwork.LocalPlayer.UserId, DataManager.chatSettings.username, queryReply });
            }
        }
    }

    private void SendPrivateUserUpdate(string target, bool queryReply = false)
    {
        chatClient.SendPrivateMessage(target, new object[] { 0, PhotonNetwork.LocalPlayer.UserId, DataManager.chatSettings.username, queryReply });
    }

    private void SendUserDelete()
    {
        if (chatClient.PublicChannels.Count > 0)
        {
            foreach (string channel in chatClient.PublicChannels.Keys)
            {
                chatClient.PublishMessage(channel, new object[] { 1, PhotonNetwork.LocalPlayer.UserId });
            }
        }
    }

    private void SendMapReset()
    {
        if(chatClient.PublicChannels.Count > 0)
        {
            foreach(string channel in chatClient.PublicChannels.Keys)
            {
                chatClient.PublishMessage(channel, new object[] { 2 });
            }
        }
    }

    private void SendPublicMessage(string inputText)
    {
        CreateMessage($"{DataManager.chatSettings.username}: {inputText}", Color.white);
        if(chatClient.PublicChannels.Count > 0)
        {
            foreach(string channel in chatClient.PublicChannels.Keys)
            {
                chatClient.PublishMessage(channel, inputText);
            }
        }
    }

    private void SendDirectMessage(string inputText)
    {
        Match match = Regex.Match(inputText, "[/][^ ]* ([^ ]*) (.*)");
        string recipientUsername = match.Groups[1].ToString();
        string message = match.Groups[2].ToString();
        string userID = GetUserID(recipientUsername);

        if(userID != recipientUsername)
        {
            CreateMessage($"me -> {recipientUsername}: {message}", Color.grey);
            chatClient.SendPrivateMessage(userID, message);
        }
        else
        {
            CreateMessage("Could not find " + recipientUsername, Color.red);
        }
    }

    private void SendReplyMessage(string inputText)
    {
        if(string.IsNullOrEmpty(replyRecipient))
        {
            CreateMessage("No one to reply to", Color.red);
        }
        else
        {
            string message = Regex.Replace(inputText, "[/][^ ]* ", "");

            CreateMessage($"me -> {GetUsername(replyRecipient)}: {message}", Color.grey);
            chatClient.SendPrivateMessage(replyRecipient, message);
        }
    }

    public void SendMessage(TMP_InputField inputField)
    {
        string inputText = inputField.text;
        if(!string.IsNullOrEmpty(inputText))
        {
            if(inputText[0] == '/')
            {
                string command = Regex.Match(inputText, "[/][^ ]*").ToString();
                switch(command)
                {
                    case "/help":
                        CreateMessage("List of commands: /msg {username}\n /w {username}\n /tell {username}\n /r\n /reply\n /mute {username}\n /unmute {username}\n /promote {username}\n /kick {username}\n /ban {username}\n /unban {username}\n /whitelist list|on|off|add|remove\n /reset", Color.yellow);
                        break;
                    case "/msg":
                        SendDirectMessage(inputText);
                        break;
                    case "/w":
                        SendDirectMessage(inputText);
                        break;
                    case "/tell":
                        SendDirectMessage(inputText);
                        break;
                    case "/r":
                        SendReplyMessage(inputText);
                        break;
                    case "/reply":
                        SendReplyMessage(inputText);
                        break;
                    case "/mute":
                        string username = Regex.Replace(inputText, "[/][^ ]* ", "");
                        string userID = GetUserID(username);
                        if(userID != username)
                        {
                            DataManager.chatSettings.muteList.TryAdd(userID);
                            CreateMessage("Muted " + username, Color.white);
                        }
                        else
                        {
                            CreateMessage("Could not find " + username, Color.red);
                        }
                        break;
                    case "/unmute":
                        username = Regex.Replace(inputText, "[/][^ ]* ", "");
                        userID = GetUserID(username);
                        if(userID != username)
                        {
                            DataManager.chatSettings.muteList.Remove(userID);
                            CreateMessage("Unmuted " + username, Color.white);
                        }
                        else
                        {
                            CreateMessage("Could not find " + username, Color.red);
                        }
                        break;
                    case "/promote":
                        if(PhotonNetwork.IsMasterClient)
                        {
                            username = Regex.Replace(inputText, "[/][^ ]* ", "");
                            Player player = CustomNetworkHandling.FindPlayerWithUsername(username);
                            if(player != null)
                            {
                                PhotonNetwork.SetMasterClient(player);
                                CreateMessage("Promoted " + username, Color.white);
                            }
                            else
                            {
                                CreateMessage("Could not find " + username, Color.red);
                            }
                        }
                        else
                        {
                            CreateMessage("You are not the owner", Color.red);
                        }
                        break;
                    case "/kick":
                        if(PhotonNetwork.IsMasterClient)
                        {
                            username = Regex.Replace(inputText, "[/][^ ]* ", "");
                            Player player = CustomNetworkHandling.FindPlayerWithUsername(username);
                            if(player != null)
                            {
                                PhotonNetwork.CloseConnection(player);
                                CreateMessage("Kicked " + username, Color.white);
                            }
                            else
                            {
                                CreateMessage("Could not find " + username, Color.red);
                            }
                        }
                        else
                        {
                            CreateMessage("You are not the owner", Color.red);
                        }
                        break;
                    case "/ban":
                        if(PhotonNetwork.IsMasterClient)
                        {
                            username = Regex.Replace(inputText, "[/][^ ]* ", "");
                            userID = GetUserID(username);
                            if(userID != username)
                            {
                                if(DataManager.chatSettings.blacklist.TryAdd(userID))
                                {
                                    SaveSystem.SaveChatSettings(DataManager.chatSettings, "ChatSettings");
                                    Player player = CustomNetworkHandling.FindPlayerWithUsername(username);
                                    if(player != null)
                                    {
                                        PhotonNetwork.CloseConnection(player);
                                    }
                                    CreateMessage("Banned " + username, Color.white);
                                }
                                else
                                {
                                    CreateMessage(username + " is already banned", Color.white);
                                }
                            }
                            else
                            {
                                CreateMessage("Could not find " + username, Color.red);
                            }
                        }
                        else
                        {
                            CreateMessage("You are not the owner", Color.red);
                        }
                        break;
                    case "/unban":
                        if(PhotonNetwork.IsMasterClient)
                        {
                            username = Regex.Replace(inputText, "[/][^ ]* ", "");
                            userID = GetUserID(username);
                            if(userID != username)
                            {
                                if(DataManager.chatSettings.blacklist.Remove(userID))
                                {
                                    SaveSystem.SaveChatSettings(DataManager.chatSettings, "ChatSettings");
                                    CreateMessage("Unbanned " + username, Color.white);
                                }
                                else
                                {
                                    CreateMessage(username + " is already unbanned", Color.white);
                                }
                            }
                            else
                            {
                                CreateMessage("Could not find " + username, Color.red);
                            }
                        }
                        else
                        {
                            CreateMessage("You are not the owner", Color.red);
                        }
                        break;
                    case "/whitelist":
                        if(PhotonNetwork.IsMasterClient)
                        {
                            Match match = Regex.Match(inputText, "[/][^ ]* ([^ ]*)");
                            string mode = match.Groups[1].ToString();
                            if(mode == "clear")
                            {
                                DataManager.chatSettings.whitelist.Clear();
                                SaveSystem.SaveChatSettings(DataManager.chatSettings, "ChatSettings");
                                CreateMessage("Cleared the whitelist", Color.white);
                            }
                            else if(mode == "list")
                            {
                                int whitelistCount = DataManager.chatSettings.whitelist.Count;
                                if(whitelistCount > 0)
                                {
                                    string usernameList = "Whitelist: ";
                                    for(int i = 0; i < whitelistCount; i++)
                                    {
                                        string user = DataManager.chatSettings.whitelist[i];
                                        if(i < whitelistCount - 1)
                                        {
                                            usernameList += GetUsername(user) + ", ";
                                        }
                                        else
                                        {
                                            usernameList += GetUsername(user);
                                        }
                                    }
                                    CreateMessage(usernameList, Color.white);
                                }
                                else
                                {
                                    CreateMessage("Whitelist is empty", Color.white);
                                }
                            }
                            else if(mode == "on")
                            {
                                DataManager.chatSettings.whitelistActive = true;
                                SaveSystem.SaveChatSettings(DataManager.chatSettings, "ChatSettings");
                                CreateMessage("Whitelist on", Color.white);
                            }
                            else if(mode == "off")
                            {
                                DataManager.chatSettings.whitelistActive = false;
                                SaveSystem.SaveChatSettings(DataManager.chatSettings, "ChatSettings");
                                CreateMessage("Whitelist off", Color.white);
                            }
                            else
                            {
                                string target = Regex.Replace(inputText, "[/][^ ]* [^ ]* ", "");
                                userID = GetUserID(target);
                                if(userID != target)
                                {
                                    if(mode == "add")
                                    {
                                        if(DataManager.chatSettings.whitelist.TryAdd(userID))
                                        {
                                            SaveSystem.SaveChatSettings(DataManager.chatSettings, "ChatSettings");
                                            CreateMessage("Added " + target + " to the whitelist", Color.white);
                                        }
                                        else
                                        {
                                            CreateMessage(target + " is already on the whitelist", Color.white);
                                        }
                                    }
                                    else if(mode == "remove")
                                    {
                                        if(DataManager.chatSettings.whitelist.Remove(userID))
                                        {
                                            SaveSystem.SaveChatSettings(DataManager.chatSettings, "ChatSettings");
                                            CreateMessage("Removed " + target + " from the whitelist", Color.white);
                                        }
                                        else
                                        {
                                            CreateMessage(target + " is already removed from the whitelist", Color.white);
                                        }
                                    }
                                    else
                                    {
                                        CreateMessage("Unknown mode \"" + mode + "\", use on/off/add/remove/clear", Color.red);
                                    }
                                }
                                else
                                {
                                    CreateMessage("Could not find " + target, Color.red);
                                }
                            }
                        }
                        else
                        {
                            CreateMessage("You are not the owner", Color.red);
                        }
                        break;
                    case "/reset":
                        if(!GameManager.Instance.inLobby && GameManager.Instance.currentScene.buildIndex <= GameManager.Instance.multiplayerSceneIndexEnd)
                        {
                            Player thisPlayer = PhotonNetwork.LocalPlayer;
                            if(!thisPlayer.CustomProperties.ContainsKey("spectator") || !(bool)thisPlayer.CustomProperties["spectator"])
                            {
                                SendMapReset();
                                resetVote++;
                                int totalCount = CustomNetworkHandling.NonSpectatorList.Length;
                                if (resetVote == totalCount)
                                {
                                    CreateMessage(resetVote + "/" + totalCount + " votes to reset the map. Resetting...", Color.white);
                                    if (PhotonNetwork.IsMasterClient)
                                    {
                                        GameManager.Instance.PhotonLoadScene(-1);
                                    }
                                    resetVote = 0;
                                }
                                else
                                {
                                    CreateMessage(resetVote + "/" + totalCount + " votes to reset the map.", Color.white);
                                }
                            }
                        }
                        break;
                    default:
                        CreateMessage($"Unknown command \"{command}\". Type \"/help\" for a list of commands.", Color.red);
                        break;
                }
            }
            else
            {
                SendPublicMessage(inputText);
            }
            inputField.text = "";
        }
    }

    public void SubscribeToChannel(string channel, int maxSubscribers, bool publishSubscribers, bool room = false)
    {
        StartCoroutine(WaitToSubscribe(channel, maxSubscribers, publishSubscribers));
        if (room)
        {
            chatClient.Unsubscribe(new string[] {"RegionLobby"});
            roomChannel = channel;
        }
    }

    IEnumerator WaitToSubscribe(string channel, int maxSubcribers, bool publishSubscribers)
    {
        ChannelCreationOptions channelOptions = new ChannelCreationOptions()
        {
            MaxSubscribers = maxSubcribers,
            PublishSubscribers = publishSubscribers,
        };
        yield return new WaitUntil(() => isConnected);
        chatClient.Subscribe(channel, 0, 0, channelOptions);
    }

    public void UnsubscribeFromRoomChannel()
    {
        chatClient.Unsubscribe(new string[] { roomChannel });
        StartCoroutine(WaitToSubscribe("RegionLobby", 99, true));
    }

    public void Disconnect()
    {
        SendUserDelete();
        chatClient.Disconnect();
    }

    public void DebugReturn(DebugLevel level, string message)
    {
        
    }

    public void OnChatStateChange(ChatState state)
    {
        
    }

    public void OnConnected()
    {
        isConnected = true;
    }

    public void OnDisconnected()
    {
        timeSinceLastMessage = chatVanishTime;
        isConnected = false;
        inputParent.SetActive(false);
        chatParent.SetActive(false);
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            if (!senders[i].Equals(chatClient.UserId))
            {
                if (messages[i].GetType() == typeof(string))
                {
                    if (!DataManager.chatSettings.muteList.Contains(senders[i]))
                    {
                        CreateMessage($"{GetUsername(senders[i])}: {messages[i]}", Color.white);
                    }
                }
                else
                {
                    object[] messageData = (object[])messages[i];
                    byte messageType = (byte)messageData[0];
                    if (messageType == 0)
                    {
                        IDUsernamePair.AddOrReplace((string)messageData[1], (string)messageData[2]);
                        if ((bool)messageData[3])
                        {
                            SendPrivateUserUpdate(senders[i]);
                        }
                    }
                    else if (messageType == 1)
                    {
                        IDUsernamePair.Remove(senders[i]);
                    }
                    else if (messageType == 2)
                    {
                        resetVote++;
                        int totalCount = CustomNetworkHandling.NonSpectatorList.Length;
                        if (resetVote == totalCount)
                        {
                            CreateMessage(resetVote + "/" + totalCount + " votes to reset the map. Resetting...", Color.white);
                            if (PhotonNetwork.IsMasterClient)
                            {
                                GameManager.Instance.PhotonLoadScene(-1);
                            }
                            resetVote = 0;
                        }
                        else
                        {
                            CreateMessage(resetVote + "/" + totalCount + " votes to reset the map.", Color.white);
                        }
                    }
                }
            }
        }
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {
        if(!string.IsNullOrEmpty(message.ToString()))
        {
            string[] splitNames = channelName.Split(new char[] { ':' });
            string senderName = splitNames[0];
            if(!sender.Equals(senderName, StringComparison.OrdinalIgnoreCase))
            {
                if(message.GetType() == typeof(string))
                {
                    if(!DataManager.chatSettings.muteList.Contains(sender))
                    {
                        CreateMessage($"{GetUsername(sender)} -> me: {message}", Color.grey);
                        replyRecipient = sender;
                    }
                }
                else
                {
                    object[] messageData = (object[])message;
                    int messageType = (int)messageData[0];
                    if(messageType == 0)
                    {
                        IDUsernamePair.AddOrReplace((string)messageData[1], (string)messageData[2]);
                        if((bool)messageData[3])
                        {
                            SendPrivateUserUpdate(sender);
                        }
                    }
                    else if(messageType == 1)
                    {
                        IDUsernamePair.Remove(sender);
                    }
                }
            }
        }
    }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
    {
        
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        for(int i = 0; i < channels.Length; i++)
        {
            SendPublicUserUpdate(channels[i], true);
            if(channels[i] != "RegionLobby")
            {
                CreateMessage("Joined " + channels[i], Color.yellow);
            }
        }
    }

    public void OnUnsubscribed(string[] channels)
    {
        for(int i = 0; i < channels.Length; i++)
        {
            if(channels[i] != "RegionLobby")
            {
                CreateMessage("Left " + channels[i], Color.yellow);
            }
        }
    }

    public void OnUserSubscribed(string channel, string user)
    {
        if (channel == "RegionLobby")
        {
            return;
        }
        if (PhotonNetwork.InRoom)
        {
            if (DataManager.chatSettings.whitelistActive && !DataManager.chatSettings.whitelist.Contains(user))
            {
                return;
            }
            if (DataManager.chatSettings.blacklist.Contains(user))
            {
                return;
            }
        }

        CreateMessage($"{GetUsername(user)} joined", Color.yellow);
    }

    public void OnUserUnsubscribed(string channel, string user)
    {
        if (channel == "RegionLobby")
        {
            return;
        }
        if (PhotonNetwork.InRoom)
        {
            if (DataManager.chatSettings.whitelistActive && !DataManager.chatSettings.whitelist.Contains(user))
            {
                return;
            }
            if (DataManager.chatSettings.blacklist.Contains(user))
            {
                return;
            }
        }

        CreateMessage($"{GetUsername(user)} left", Color.yellow);
    }
}
