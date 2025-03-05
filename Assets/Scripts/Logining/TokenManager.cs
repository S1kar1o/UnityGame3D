using UnityEngine;
using System.IO;
using System;

public class TokenManager : MonoBehaviour
{
    private string tokenFilePath;

    void Start()
    {
        // ���� �� ����� ������ � persistentDataPath
        tokenFilePath = Path.Combine(Application.persistentDataPath, "user_token.txt");
        Debug.Log("���� �� ����� ������: " + tokenFilePath);
    }
    public void SaveTokenToFile(string token, string userId)
    {
        try
        {
            string data = token + " " + userId;
            File.WriteAllText(tokenFilePath, data);
            Debug.Log("����� ������ ��������� � ����.");
        }
        catch (Exception ex)
        {
            Debug.LogError("�� ������� �������� ����� � ����: " + ex.Message);
        }
    }

    public string GetTokenFromFile()
    {
        if (File.Exists(tokenFilePath))
        {
            string content = File.ReadAllText(tokenFilePath).Trim(); // ��������� ���� ������

            if (string.IsNullOrEmpty(content))
            {
                Debug.LogWarning("���� � ������� ����, ��� �� �������.");
                return null;
            }

            return content;
        }
        else
        {
            Debug.LogWarning("���� � ������� �� ���������.");
            return null;
        }
    }


    // ����� ��� ��������� ������ (��������� �����)
    public void DeleteTokenFile()
    {
        if (File.Exists(tokenFilePath))
        {
            File.Delete(tokenFilePath); // ��������� ����
        }
    }
}
