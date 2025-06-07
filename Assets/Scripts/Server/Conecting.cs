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
    public TMP_Text points, nickName;
    private UnityTcpClient tcpClient;
    public GameObject panelWithInformation;
    public string telegramURL = "https://t.me/+WVqCryqlsII5ZmM6";

    public GameObject userItemPrefab;
    public Transform contentPanel;

    public GameObject PanelWithRaiting;
    private int alreadyLoad = 0;
    private bool hasMoreUsers = true;
    private bool isLoading = false;
    public float loadThreshold = 0.05f; // 5% до низу

    [System.Serializable]
    public class User
    {
        public string username;
        public int rating;
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

        UnityTcpClient.Instance.SendMessage("USER_INFORMATION");
    }

    public void UpdateInformation(string message)
    {
        string[] parts = message.Split(' ');

        points.text = parts[1];
        nickName.text = parts[2];
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
                GameObject item = Instantiate(userItemPrefab, contentPanel);

                TMP_Text usernameText = item.transform.Find("PanelUserInformation/UsernameText")?.GetComponent<TMP_Text>();
                TMP_Text ratingText = item.transform.Find("PanelUserInformation/PointsImg/RatingText")?.GetComponent<TMP_Text>();

                if (usernameText == null || ratingText == null)
                {
                    Debug.LogError("TMP_Text components not found in the instantiated prefab!");
                    continue;
                }

                usernameText.text = user.username;
                ratingText.text = user.rating.ToString();
                alreadyLoad++;
            }

            isLoading = false;
        }
    }
    public void LeaderBoardStatus()
    {
        UnityTcpClient.Instance.SendMessage("LEADER_BOARD"+" "+alreadyLoad);

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
        if(PanelWithRaiting.activeSelf)
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
