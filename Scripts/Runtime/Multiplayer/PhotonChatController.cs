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

public class PhotonChatController : MonoBehaviour, IChatClientListener
{
    public static PhotonChatController Instance;

    ChatClient chatClient;
    string replyRecipient = null;

    [SerializeField] GameObject chatBox;
    [SerializeField] TMP_InputField chatInput;

    [SerializeField] RectTransform chatContent;
    [SerializeField] GameObject chatMessageTemplate;

    string roomChannel = null;

    // Start is called before the first frame update
    void Start()
    {
        if (Instance == null)
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
        if (!chatBox.activeSelf)
        {
            if (Input.GetKeyDown(DataManager.playerSettings.keyBinds["Chat"]))
            {
                chatBox.SetActive(true);
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SendMessage(chatInput);
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                chatBox.SetActive(false);
            }
        }
    }

    public void ConnectToPhotonChat()
    {
        chatClient.AuthValues = new AuthenticationValues(PhotonNetwork.LocalPlayer.UserId);
        chatClient.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, PhotonNetwork.AppVersion, new AuthenticationValues(PhotonNetwork.LocalPlayer.UserId));
    }

    private void CreateMessage(string message, Color color)
    {
        TextMeshProUGUI chatMessage = Instantiate(chatMessageTemplate, chatContent).GetComponent<TextMeshProUGUI>();
        chatMessage.color = color;
        chatMessage.text = message;
    }

    private void SendPublicMessage(string inputText)
    {
        CreateMessage($"{DataManager.playerSettings.username}: {inputText}", Color.white);
        if (chatClient.PublicChannels.Count > 0)
        {
            foreach (string channel in chatClient.PublicChannels.Keys)
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
        CreateMessage($"me -> {recipientUsername}: {message}", Color.grey);

        chatClient.SendPrivateMessage(CustomNetworkHandling.FindPlayerWithUsername(recipientUsername).UserId, message);
    }

    private void SendReplyMessage(string inputText)
    {
        if (string.IsNullOrEmpty(replyRecipient))
        {
            CreateMessage("No one to reply to", Color.red);
        }
        else
        {
            string message = Regex.Replace(inputText, "[/][^ ]* ", "");
            CreateMessage($"me -> {CustomNetworkHandling.FindPlayerWithUserID(replyRecipient).NickName}: {message}", Color.grey);
            chatClient.SendPrivateMessage(replyRecipient, message);
        }
    }

    public void SendMessage(TMP_InputField inputField)
    {
        string inputText = inputField.text;
        if (!string.IsNullOrEmpty(inputText))
        {
            if (inputText[0] == '/')
            {
                string command = Regex.Match(inputText, "[/][^ ]*").ToString();
                switch (command)
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
        StartCoroutine(WaitToSubscribe(channel));
        roomChannel = channel;
    }

    IEnumerator WaitToSubscribe(string channel)
    {
        while (!chatClient.Subscribe(new string[] { channel }))
        {
            yield return new WaitForSecondsRealtime(0.05f);
        }
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
        
    }

    public void OnDisconnected()
    {
        
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            if (!senders[i].Equals(chatClient.UserId))
            {
                CreateMessage($"{CustomNetworkHandling.FindPlayerWithUserID(senders[i]).NickName}: {messages[i]}", Color.white);
            }
        }
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {
        if (!string.IsNullOrEmpty(message.ToString()))
        {
            string[] splitNames = channelName.Split(new char[] { ':' });
            string senderName = splitNames[0];
            if (!sender.Equals(senderName, StringComparison.OrdinalIgnoreCase))
            {
                CreateMessage($"{CustomNetworkHandling.FindPlayerWithUserID(sender).NickName} -> me: {message}", Color.grey);
                replyRecipient = sender;
            }
        }
    }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
    {
        
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        for (int i = 0; i < channels.Length; i++)
        {
            CreateMessage("Joined " + channels[i], Color.yellow);
        }
    }

    public void OnUnsubscribed(string[] channels)
    {
        for (int i = 0; i < channels.Length; i++)
        {
            CreateMessage("Left " + channels[i], Color.yellow);
        }
    }

    public void OnUserSubscribed(string channel, string user)
    {
        CreateMessage($"{CustomNetworkHandling.FindPlayerWithUserID(user).NickName} joined", Color.yellow);
    }

    public void OnUserUnsubscribed(string channel, string user)
    {
        CreateMessage($"{CustomNetworkHandling.FindPlayerWithUserID(user).NickName} left", Color.yellow);
    }
}
