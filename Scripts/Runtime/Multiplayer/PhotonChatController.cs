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
    string replyRecipient = null;

    [SerializeField] GameObject inputParent;
    [SerializeField] GameObject chatParent;
    [SerializeField] CanvasGroup chatCanvasGroup;
    [SerializeField] TMP_InputField chatInput;
    [SerializeField] RectTransform chatContent;
    [SerializeField] GameObject chatMessageTemplate;

    string roomChannel = null;

    Dictionary<string, string> IDUsernamePair = new Dictionary<string, string>();

    bool isConnected = false;
    float timeSinceLastMessage = 0;
    public float chatVanishTime = 5;

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
        if(!inputParent.activeSelf)
        {
            if(Input.GetKeyDown(DataManager.playerSettings.keyBinds["Chat"]))
            {
                chatParent.SetActive(true);
                inputParent.SetActive(true);
                chatCanvasGroup.enabled = false;
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
            else if(Input.GetKeyDown(KeyCode.Escape))
            {
                timeSinceLastMessage = chatVanishTime;
                inputParent.SetActive(false);
                chatParent.SetActive(false);
                chatCanvasGroup.enabled = true;
            }
        }
    }

    string GetUsername(string userID)
    {
        if(IDUsernamePair.ContainsKey(userID))
        {
            return IDUsernamePair[userID];
        }
        else
        {
            Player player = CustomNetworkHandling.FindPlayerWithUserID(userID);
            if(player != null)
            {
                string username = player.NickName;
                IDUsernamePair.Add(userID, username);
                return username;
            }
            else
            {
                Debug.LogWarning("Could not find username for user " + userID);
                return userID;
            }
        }
    }

    string GetUserID(string username)
    {
        Player player = CustomNetworkHandling.FindPlayerWithUsername(username);
        if(player != null)
        {
            return player.UserId;
        }
        else
        {
            foreach((string userID, string name) in IDUsernamePair)
            {
                if(name == username)
                {
                    return userID;
                }
            }
            Debug.LogWarning("Could not find userID for user " + username);
            return username;
        }
    }

    public void ConnectToPhotonChat()
    {
        IDUsernamePair.AddOrReplace(PhotonNetwork.LocalPlayer.UserId, DataManager.chatSettings.username);
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
        Match match = Regex.Match(inputText, "[/][^ ]*([^ ]*)(.*)");
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
            CreateMessage("Could not find" + recipientUsername, Color.red);
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
                            Match match = Regex.Match(inputText, "[/][^ ]*([^ ]*)(.*)");
                            string mode = match.Groups[1].ToString();
                            if(mode == "clear")
                            {
                                DataManager.chatSettings.whitelist.Clear();
                                SaveSystem.SaveChatSettings(DataManager.chatSettings, "ChatSettings");
                                CreateMessage("Cleared the whitelist", Color.white);
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
                                string target = match.Groups[2].ToString();
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
                    default:
                        CreateMessage($"Unknown command \"{command}\"", Color.red);
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

    public void SubscribeToRoomChannel(string channel)
    {
        StartCoroutine(WaitToSubscribe(channel, 99, true));
        roomChannel = channel;
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
        isConnected = false;
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for(int i = 0; i < senders.Length; i++)
        {
            if(!senders[i].Equals(chatClient.UserId) && !DataManager.chatSettings.muteList.Contains(senders[i]))
            {
                CreateMessage($"{GetUsername(senders[i])}: {messages[i]}", Color.white);
            }
        }
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {
        if(!string.IsNullOrEmpty(message.ToString()))
        {
            string[] splitNames = channelName.Split(new char[] { ':' });
            string senderName = splitNames[0];
            if(!sender.Equals(senderName, StringComparison.OrdinalIgnoreCase) && !DataManager.chatSettings.muteList.Contains(sender))
            {
                CreateMessage($"{GetUsername(sender)} -> me: {message}", Color.grey);
                replyRecipient = sender;
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
            CreateMessage("Joined " + channels[i], Color.yellow);
        }
    }

    public void OnUnsubscribed(string[] channels)
    {
        for(int i = 0; i < channels.Length; i++)
        {
            CreateMessage("Left " + channels[i], Color.yellow);
        }
    }

    public void OnUserSubscribed(string channel, string user)
    {
        if(DataManager.chatSettings.whitelistActive && !DataManager.chatSettings.whitelist.Contains(user))
        {
            return;
        }
        if(DataManager.chatSettings.blacklist.Contains(user))
        {
            return;
        }
        CreateMessage($"{GetUsername(user)} joined", Color.yellow);
    }

    public void OnUserUnsubscribed(string channel, string user)
    {
        if(DataManager.chatSettings.whitelistActive && !DataManager.chatSettings.whitelist.Contains(user))
        {
            return;
        }
        if(DataManager.chatSettings.blacklist.Contains(user))
        {
            return;
        }
        CreateMessage($"{GetUsername(user)} left", Color.yellow);
    }
}
