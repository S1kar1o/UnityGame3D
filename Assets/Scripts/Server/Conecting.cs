using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class Conecting : MonoBehaviour
{
    public TMP_Text points, nickName, id;
    public TMP_InputField textOfLetter;
    private UnityTcpClient tcpClient;
    public GameObject panelWithInformation, panelWithFriends, panelForWrittingLetter;
    public string telegramURL = "https://t.me/+WVqCryqlsII5ZmM6";
    public GameObject userItemPrefab, friendItemPrefab,messageItemPref;
    public Transform usersTopPanel, friendsPanel, panelWithLetterslTrans;

    public GameObject PanelWithRaiting, PanelWithLetters;
    private int alreadyLoad = 0, alreadyLoadFriend = 0,alreadyLoadedMessages = 0;
    private bool hasMoreUsers = true, hasMoreFriend = true, hasMoreMessage = false;
    private bool isLoading = false, isLoadingFriends = false, isLoadingFriendsMessages = false;
    public float loadThreshold = 0.05f; // 5% до низу

    public String tmpUserNick;
    private GameObject prefabSent;

    public Coroutine currentAnimationCoroutine;


    [SerializeField] private RectTransform rectComponent;
    private float rotateSpeed = 400f;
    public Image bacgroundDurringLoading;
    [System.Serializable]
    public class UserMessage
    {
        public string id ;

        public string sender_id;

        public string receiver_id;

        public string content;

        public string timestamp; 

        public bool isRead;
    }
    [System.Serializable]
    public class MessageList
    {
        public UserMessage[] messages;
    }

    public enum FriendshipStatus
    {
        Pending,
        Accepted,
        Declined,
        Himself,
        Procecing,
        CanApply,
        None
    }
    [System.Serializable]
    public class User
    {
        public string username;
        public int rating;
        public string id;
        public int status;
        [NonSerialized] public FriendshipStatus statusEnum;

        public void MapEnumStatus()
        {
            statusEnum = (FriendshipStatus)status;
        }
    }

    [System.Serializable]
    public class UserList
    {
        public User[] users;
    }

    public void CloseGame()
    {
        Debug.Log("Game is exiting...");
        Application.Quit();

#if UNITY_EDITOR
        // Якщо ви запускаєте гру в редакторі Unity, закриття гри не працюватиме, тому ми зупинимо редактор.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    void Awake()
    {
        tcpClient = UnityTcpClient.Instance;
        points.text = UnityTcpClient.Instance.UserRaiting;
        nickName.text = UnityTcpClient.Instance.UserNickName;
        id.text = UnityTcpClient.Instance.UserId;
        UnityTcpClient.Instance.SendMessage("USER_INFORMATION");
        currentAnimationCoroutine = StartCoroutine(waitingAnimation());

    }
    public IEnumerator waitingAnimation()
    {
        bacgroundDurringLoading.gameObject.SetActive(true);
        rectComponent.gameObject.SetActive(true);
        while (true)
        {
            if (rectComponent != null)
                rectComponent.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime);
            else
                yield break; // закінчити корутину, якщо компонент видалено
            yield return null;
        }
    }

    public void stopWaitingAnimation()
    {
        bacgroundDurringLoading.gameObject.SetActive(false);
        rectComponent.gameObject.SetActive(false);
        if (currentAnimationCoroutine != null)
            StopCoroutine(currentAnimationCoroutine);
    }
    public void UpdateInformation(string message)
    {
        string[] parts = message.Split(' ');

        points.text = parts[1];
        nickName.text = parts[2];
        id.text = parts[3];
        UnityTcpClient.Instance.UserId = id.text;
        UnityTcpClient.Instance.UserRaiting = points.text;
        UnityTcpClient.Instance.UserNickName = nickName.text;
    }
    public async void SendMessageToServer()
    {
        await tcpClient.SendMessage("READYTOPLAY");
    }
    public void CheckScrollPosition(Vector2 scrollPos)
    {
        if (!isLoading && hasMoreUsers && scrollPos.y <= loadThreshold)
        {
            isLoading = true;
            LeaderBoardStatus();
        }
    }

    // Функція, що приймає JSON рядок і створює UI-елементи для користувачів
    public void LoadFriendsMessage(string json)
    {
        panelWithFriends.SetActive(true);
        Debug.Log("Loading users from JSON: " + json);

        if (json != " Немає користувачів для відображення")
        {
            UserList userList = JsonUtility.FromJson<UserList>(json);

            if (userList == null || userList.users == null || userList.users.Length == 0)
            {
                Debug.Log("No more users to load.");
                hasMoreMessage = false;
                isLoadingFriendsMessages = false;
                return;
            }

            foreach (User user in userList.users)
            {
                GameObject item = Instantiate(friendItemPrefab, friendsPanel);

                TMP_Text usernameText = item.transform.Find("PanelUserInformation/UsernameText")?.GetComponent<TMP_Text>();
                TMP_Text ratingText = item.transform.Find("PanelUserInformation/PointsImg/RatingText")?.GetComponent<TMP_Text>();
                TMP_Text userId = item.transform.Find("PanelUserInformation/CopyIdButton/UserId")?.GetComponent<TMP_Text>();

                Button sendMessageButton = item.transform.Find("PanelUserInformation/SendMessage")?.GetComponent<Button>();
                if (sendMessageButton != null)
                {
                    sendMessageButton.onClick.RemoveAllListeners();
                    sendMessageButton.onClick.AddListener(() => GetFriendsLetters(item));
                }

                usernameText.text = user.username;
                ratingText.text = user.rating.ToString();
                userId.text = user.id;

                user.MapEnumStatus();
                alreadyLoadFriend++;
            }
        }
    }
    public void LoadFriendListFromMessages(string json)
    {
        Debug.Log("Loading messages from JSON: " + json);

        if (string.IsNullOrEmpty(json) || json.Contains("Немає користувачів"))
        {
            Debug.Log("No messages to load.");
            return;
        }

        MessageList messageList = JsonUtility.FromJson<MessageList>(json);

        if (messageList == null || messageList.messages == null || messageList.messages.Length == 0)
        {
            Debug.Log("No messages found in the response.");
            return;
        }

        foreach (UserMessage message in messageList.messages)
        {
            GameObject messageObj = Instantiate(messageItemPref, panelWithLetterslTrans);
            TMP_Text usernameText = messageObj.transform.Find("Text")?.GetComponent<TMP_Text>();

            if (message.sender_id == UnityTcpClient.Instance.UserId)
            {
                string content = UnityTcpClient.Instance.UserNickName + "\n" + "\t" + message.content;
                usernameText.text = content;
                Image imageComponent = messageObj.transform.GetComponent<Image>();
                if (imageComponent != null)
                    imageComponent.color = new Color(83f / 255f, 49f / 255f, 88f / 255f); // #533158
            }
            else
            {
                string content = tmpUserNick + "\n" + "\t" + message.content;
                usernameText.text = content;
            }

            alreadyLoadedMessages++;
        }

        panelWithFriends.SetActive(false);
        PanelWithLetters.SetActive(true);
    }
    public void activateFriendList()
    {
        panelWithFriends.SetActive(true);

    }
    public void activateLettersList()
    {
        PanelWithLetters.SetActive(true);

    }
    public async void GetFriendsLetters(GameObject item)
    {
        prefabSent = item;
        GameObject txt = prefabSent.transform.Find("PanelUserInformation/UsernameText").gameObject;
        tmpUserNick = txt.GetComponent<TMP_Text>().text;

        PanelWithLetters.gameObject.SetActive(false);
        Transform idTransform = prefabSent.transform.Find("PanelUserInformation/CopyIdButton/UserId");
        TMP_Text idSendTo = idTransform.GetComponent<TMP_Text>();

        // ⚡ Передай кількість уже завантажених повідомлень
        await UnityTcpClient.Instance.SendMessage($"LISTING_STATUS {idSendTo.text} {alreadyLoadedMessages}");
        currentAnimationCoroutine = StartCoroutine(waitingAnimation());
    }
    public void LoadUsersFromJson(string json)
    {
        Debug.Log("Loading users from JSON: " + json);
        if (json != " Немає користувачів для відображення")
        {
            UserList userList = JsonUtility.FromJson<UserList>(json);

            if (userList == null || userList.users == null || userList.users.Length == 0)
            {
                Debug.Log("No more users to load.");
                hasMoreUsers = false;  // ⛔ Ставимо прапор, що більше нема
                isLoading = false;
                return;
            }

            foreach (User user in userList.users)
            {
                GameObject item = Instantiate(userItemPrefab, usersTopPanel);

                TMP_Text usernameText = item.transform.Find("PanelUserInformation/UsernameText")?.GetComponent<TMP_Text>();
                TMP_Text ratingText = item.transform.Find("PanelUserInformation/PointsImg/RatingText")?.GetComponent<TMP_Text>();
                TMP_Text userId = item.transform.Find("PanelUserInformation/CopyIdButton/UserId")?.GetComponent<TMP_Text>();
                GameObject userIdButton = item.transform.Find("PanelUserInformation/CopyIdButton")?.gameObject;
                GameObject areFriends = item.transform.Find("PanelUserInformation/AreFriends").gameObject;
                GameObject himselfBorder = item.transform.Find("himselfBorder").gameObject;
                GameObject declineFriendsRequest = item.transform.Find("PanelUserInformation/DeclineButton").gameObject;
                GameObject deleteFrend = item.transform.Find("PanelUserInformation/RemoveFriendButton").gameObject;

                GameObject sentFriendsRequest = item.transform.Find("PanelUserInformation/SentRequest").gameObject;
                GameObject sendToUserFriendRequest = item.transform.Find("PanelUserInformation/SendFriendRequest").gameObject;
                GameObject applyRequestButton = item.transform.Find("PanelUserInformation/ApplyRequestButton").gameObject;
                if (usernameText == null || ratingText == null)
                {
                    Debug.LogError("TMP_Text components not found in the instantiated prefab!");
                    continue;
                }
                Button sendFriendButton = sendToUserFriendRequest.GetComponent<Button>();
                if (sendFriendButton != null)
                {
                    sendFriendButton.onClick.RemoveAllListeners(); // щоб не дублювались
                    sendFriendButton.onClick.AddListener(() => SendFriendRequest(item));
                }
                Button deleteFrendButton = deleteFrend.GetComponent<Button>();
                if (deleteFrendButton != null)
                {
                    deleteFrendButton.onClick.RemoveAllListeners(); // щоб не дублювались
                    deleteFrendButton.onClick.AddListener(() => DeleteFriend(item));
                }
                Button accept = applyRequestButton.GetComponent<Button>();
                if (accept != null)
                {
                    accept.onClick.RemoveAllListeners(); // щоб не дублювались
                    accept.onClick.AddListener(() => AcceptFriendRequest(item));
                }
                Button declineFriendButton = declineFriendsRequest.GetComponent<Button>();
                if (declineFriendButton != null)
                {
                    declineFriendButton.onClick.RemoveAllListeners(); // щоб не дублювались
                    declineFriendButton.onClick.AddListener(() => DeclineFriendRequest(item));
                }
                Button copyUserIdButton = userIdButton.GetComponent<Button>();
                if (copyUserIdButton != null)
                {
                    copyUserIdButton.onClick.RemoveAllListeners(); // щоб не дублювались
                    copyUserIdButton.onClick.AddListener(() => CopyIdPlayer(userId));
                }
                usernameText.text = user.username;
                ratingText.text = user.rating.ToString();
                user.MapEnumStatus();
                switch (user.statusEnum)
                {
                    case FriendshipStatus.Procecing:
                        areFriends.SetActive(false);
                        sentFriendsRequest.SetActive(true);
                        sendToUserFriendRequest.SetActive(false);
                        himselfBorder.SetActive(false);
                        applyRequestButton.SetActive(false);
                        declineFriendsRequest.SetActive(false);
                        deleteFrend.SetActive(false);
                        break;
                    case FriendshipStatus.CanApply:
                        areFriends.SetActive(false);
                        sentFriendsRequest.SetActive(false);
                        sendToUserFriendRequest.SetActive(false);
                        himselfBorder.SetActive(false);
                        applyRequestButton.SetActive(true);
                        declineFriendsRequest.SetActive(true);
                        deleteFrend.SetActive(false);
                        break;
                    case FriendshipStatus.Pending:
                        areFriends.SetActive(false);
                        sentFriendsRequest.SetActive(true);
                        sendToUserFriendRequest.SetActive(false);
                        himselfBorder.SetActive(false);
                        applyRequestButton.SetActive(false);
                        declineFriendsRequest.SetActive(false);
                        deleteFrend.SetActive(false);

                        break;
                    case FriendshipStatus.Accepted:
                        areFriends.SetActive(true);
                        sentFriendsRequest.SetActive(false);
                        sendToUserFriendRequest.SetActive(false);
                        himselfBorder.SetActive(false);
                        applyRequestButton.SetActive(false);
                        declineFriendsRequest.SetActive(false);
                        deleteFrend.SetActive(true);

                        break;
                    case FriendshipStatus.Declined:
                        areFriends.SetActive(false);
                        sentFriendsRequest.SetActive(false);
                        sendToUserFriendRequest.SetActive(true);
                        himselfBorder.SetActive(false);
                        applyRequestButton.SetActive(false);

                        break;
                    case FriendshipStatus.None:
                        areFriends.SetActive(false);
                        sentFriendsRequest.SetActive(false);
                        sendToUserFriendRequest.SetActive(true);
                        himselfBorder.SetActive(false);
                        applyRequestButton.SetActive(false);
                        declineFriendsRequest.SetActive(false);
                        deleteFrend.SetActive(false);
                        break;
                    case FriendshipStatus.Himself:
                        areFriends.SetActive(false);
                        sentFriendsRequest.SetActive(false);
                        sendToUserFriendRequest.SetActive(false);
                        himselfBorder.SetActive(true);
                        applyRequestButton.SetActive(false);
                        declineFriendsRequest.SetActive(false);
                        deleteFrend.SetActive(false);
                        break;
                }
                alreadyLoad++;
                userId.text = user.id;
            }

            isLoading = false;
        }
    }
    public async void SendFriendRequest(GameObject prefabSent)
    {
        Transform idTransform = prefabSent.transform.Find("PanelUserInformation/CopyIdButton/UserId");
        TMP_Text idSendTo = idTransform.GetComponent<TMP_Text>();
        GameObject sentFriendsRequest = prefabSent.transform.Find("PanelUserInformation/SentRequest").gameObject;
        GameObject beforeSenFriendsRequest = prefabSent.transform.Find("PanelUserInformation/SendFriendRequest").gameObject;
        sentFriendsRequest.SetActive(true);
        beforeSenFriendsRequest.SetActive(false);
        await tcpClient.SendMessage($"SEND_FRIEND_REQUEST {idSendTo.text}");

    }
    public void ActivateSendMessage()
    {
        panelForWrittingLetter.SetActive(true);
        panelWithFriends.SetActive(false);

    }
    public void closeMenu(GameObject item)
    {
        item.SetActive(false);
        TMP_InputField inputField = item.transform.Find("Viewport/Content/InputField (TMP)").GetComponent<TMP_InputField>();
        if (inputField != null)
        {
            inputField.text = "";
        }

    }

    public async void SendMessageToFriend()
    {
        Transform idTransform = prefabSent.transform.Find("PanelUserInformation/CopyIdButton/UserId");
        TMP_Text idSendTo = idTransform.GetComponent<TMP_Text>();

        panelForWrittingLetter.SetActive(false);
        bacgroundDurringLoading.gameObject.SetActive(false);

        await UnityTcpClient.Instance.SendMessage($"SEND_MESSAGE {idSendTo.text} {textOfLetter.text}");

        GameObject messageObj = Instantiate(messageItemPref, panelWithLetterslTrans);
        TMP_Text usernameText = messageObj.transform.Find("Text")?.GetComponent<TMP_Text>();
        String content = UnityTcpClient.Instance.UserNickName + "\n" + "\t" + textOfLetter.text;
        usernameText.text = content;
    }
    public async void AcceptFriendRequest(GameObject prefabSent)
    {
        Transform idTransform = prefabSent.transform.Find("PanelUserInformation/CopyIdButton/UserId");
        TMP_Text idSendTo = idTransform.GetComponent<TMP_Text>();
        GameObject applyFriendsRequest = prefabSent.transform.Find("PanelUserInformation/ApplyRequestButton").gameObject;
        GameObject declineFriendsRequest = prefabSent.transform.Find("PanelUserInformation/DeclineButton").gameObject;

        GameObject friends = prefabSent.transform.Find("PanelUserInformation/AreFriends").gameObject;
        applyFriendsRequest.SetActive(false);
        declineFriendsRequest.SetActive(false);
        GameObject deleteFrendButton = prefabSent.transform.Find("PanelUserInformation/RemoveFriendButton").gameObject;
        deleteFrendButton.SetActive(true);
        friends.SetActive(true);
        await tcpClient.SendMessage($"ACCEPT_FRIEND_REQUEST {idSendTo.text}");

    }
    public async void DeleteFriend(GameObject prefabSent)
    {
        Transform idTransform = prefabSent.transform.Find("PanelUserInformation/CopyIdButton/UserId");
        TMP_Text idSendTo = idTransform.GetComponent<TMP_Text>();
        GameObject applyFriendsRequest = prefabSent.transform.Find("PanelUserInformation/ApplyRequestButton").gameObject;
        GameObject declineFriendsRequest = prefabSent.transform.Find("PanelUserInformation/DeclineButton").gameObject;
        GameObject deleteFrendButton = prefabSent.transform.Find("PanelUserInformation/RemoveFriendButton").gameObject;
        GameObject sendToUserFriendRequest = prefabSent.transform.Find("PanelUserInformation/SendFriendRequest").gameObject;

        GameObject friends = prefabSent.transform.Find("PanelUserInformation/AreFriends").gameObject;
        applyFriendsRequest.SetActive(false);
        declineFriendsRequest.SetActive(false);
        deleteFrendButton.SetActive(false);
        friends.SetActive(false);
        sendToUserFriendRequest.SetActive(true);
        await tcpClient.SendMessage($"REMOVE_FRIEND {idSendTo.text}");

    }
    public async void DeclineFriendRequest(GameObject prefabSent)
    {
        Transform idTransform = prefabSent.transform.Find("PanelUserInformation/CopyIdButton/UserId");
        TMP_Text idSendTo = idTransform.GetComponent<TMP_Text>();
        GameObject declineFriendsRequest = prefabSent.transform.Find("PanelUserInformation/DeclineButton").gameObject;
        GameObject applyFriendsRequest = prefabSent.transform.Find("PanelUserInformation/ApplyRequestButton").gameObject;

        GameObject sendingButton = prefabSent.transform.Find("PanelUserInformation/SendFriendRequest").gameObject;
        declineFriendsRequest.SetActive(false);
        applyFriendsRequest.SetActive(false);

        sendingButton.SetActive(true);
        await tcpClient.SendMessage($"DECLINE_FRIEND_REQUEST {idSendTo.text}");

    }
    public async void SendMessageToLoadFriendList()
    {
        int offset = alreadyLoadFriend; // скільки вже завантажено
        await UnityTcpClient.Instance.SendMessage($"FRIENDLIST_STATUS {offset}");
        currentAnimationCoroutine = StartCoroutine(waitingAnimation());
    }

    public void CopyIdPlayer(TMP_Text userId)
    {
        GUIUtility.systemCopyBuffer = userId.text;
    }
    public void OnConnectedToServer(GameObject userInformPref)
    {
        GameObject areFriends = userInformPref.transform.Find("AreFriends").gameObject;
        GameObject sentFriendsRequest = userInformPref.transform.Find("SentRequest").gameObject;
        GameObject sendToUserFriendRequest = userInformPref.transform.Find("SendFriendRequest").gameObject;

        areFriends.SetActive(false);
        sentFriendsRequest.SetActive(true);
        sendToUserFriendRequest.SetActive(false);

    }
    public void LeaderBoardStatus()
    {
        UnityTcpClient.Instance.SendMessage("LEADER_BOARD" + " " + alreadyLoad);
        currentAnimationCoroutine = StartCoroutine(waitingAnimation());


    }

    public void ChangeUser()
    {
        try
        {
            string tokenFilePath = Path.Combine(Application.persistentDataPath, "user_token.txt");
            string data = "";
            File.WriteAllText(tokenFilePath, data);
            Debug.Log("Токен успішно збережено у файл.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Не вдалося зберегти токен у файл: " + ex.Message);
        }
        UnityTcpClient.Instance.SendMessage("EXIT");
        StartCoroutine(LoadSceneAsync("LogingScene"));

    }
    public void OpenTelegramGroup()
    {
        Application.OpenURL(telegramURL);
    }
    public void additionalInformation()
    {
        panelWithInformation.SetActive(true);
    }
    public void PanelRaitingChangeActive()
    {
        PanelWithRaiting.SetActive(!PanelWithRaiting.activeSelf);
        if (PanelWithRaiting.activeSelf)
        {
            LeaderBoardStatus();
        }
    }
    public void closeAdditionalInformation()
    {
        panelWithInformation.SetActive(false);
    }
    IEnumerator LoadSceneAsync(string nameScene)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(nameScene);

        // Уникаємо автоматичного переходу на нову сцену, чекаючи завершення завантаження
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            // Прогрес завантаження
            float progress = Mathf.Clamp01(operation.progress / 0.9f); // Progress max is 0.9
                                                                       // Відображення прогресу завантаження (наприклад, через slider)
            Debug.Log("Завантаження: " + progress * 100 + "%");

            // Якщо завантажено 90%, переходимо до сцени
            if (operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;  // Активуємо сцену

            }

            yield return null; // дає можливість оновлювати UI, виконувати інші завдання

        }
    }
}
