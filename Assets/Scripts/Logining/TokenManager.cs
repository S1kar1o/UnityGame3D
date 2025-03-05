using UnityEngine;
using System.IO;
using System;

public class TokenManager : MonoBehaviour
{
    private string tokenFilePath;

    void Start()
    {
        // Шлях до файлу токена в persistentDataPath
        tokenFilePath = Path.Combine(Application.persistentDataPath, "user_token.txt");
        Debug.Log("Шлях до файлу токена: " + tokenFilePath);
    }
    public void SaveTokenToFile(string token, string userId)
    {
        try
        {
            string data = token + " " + userId;
            File.WriteAllText(tokenFilePath, data);
            Debug.Log("Токен успішно збережено у файл.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Не вдалося зберегти токен у файл: " + ex.Message);
        }
    }

    public string GetTokenFromFile()
    {
        if (File.Exists(tokenFilePath))
        {
            string content = File.ReadAllText(tokenFilePath).Trim(); // Видаляємо зайві пробіли

            if (string.IsNullOrEmpty(content))
            {
                Debug.LogWarning("Файл з токеном існує, але він порожній.");
                return null;
            }

            return content;
        }
        else
        {
            Debug.LogWarning("Файл з токеном не знайдений.");
            return null;
        }
    }


    // Метод для видалення токену (видалення файлу)
    public void DeleteTokenFile()
    {
        if (File.Exists(tokenFilePath))
        {
            File.Delete(tokenFilePath); // Видаляємо файл
        }
    }
}
