using UnityEngine;

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
        tcpClient = FindObjectOfType<UnityTcpClient>();
    }

    public async void PlayGame()
    {
        await tcpClient.ConnectToServer();
    }

    public async void SendMessageToServer(string message)
    {
        await tcpClient.SendMessage(message);
    }

    void Update()
    {
        if (tcpClient != null && tcpClient.IsConnected())
        {
            // You can add UI updates here to reflect the connection state
        }
    }
}
