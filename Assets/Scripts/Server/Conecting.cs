using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Conecting : MonoBehaviour
{
    private UnityTcpClient tcpClient;
    public void CloseGame()
    {
        Debug.Log("Game is exiting...");
        Application.Quit();

#if UNITY_EDITOR
        // Якщо ви запускаєте гру в редакторі Unity, закриття гри не працюватиме, тому ми зупинимо редактор.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    void Start()
    {
        tcpClient = UnityTcpClient.Instance;
    }


    public async void SendMessageToServer()
    {
        await tcpClient.SendMessage("READYTOPLAY");
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
