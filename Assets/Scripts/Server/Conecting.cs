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
        // ���� �� ��������� ��� � �������� Unity, �������� ��� �� �����������, ���� �� �������� ��������.
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
            Debug.Log("����� ������ ��������� � ����.");
        }
        catch (Exception ex)
        {
            Debug.LogError("�� ������� �������� ����� � ����: " + ex.Message);
        }
        UnityTcpClient.Instance.SendMessage("EXIT");
        StartCoroutine(LoadSceneAsync("LogingScene"));

    }
    IEnumerator LoadSceneAsync(string nameScene)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(nameScene);

        // ������� ������������� �������� �� ���� �����, ������� ���������� ������������
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            // ������� ������������
            float progress = Mathf.Clamp01(operation.progress / 0.9f); // Progress max is 0.9
                                                                       // ³���������� �������� ������������ (���������, ����� slider)
            Debug.Log("������������: " + progress * 100 + "%");

            // ���� ����������� 90%, ���������� �� �����
            if (operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;  // �������� �����

            }

            yield return null; // �� ��������� ���������� UI, ���������� ���� ��������

        }
    }
}
