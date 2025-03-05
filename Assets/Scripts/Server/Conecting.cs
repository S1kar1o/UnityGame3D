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

    
    public async void SendMessageToServer()
    {
        await tcpClient.SendMessage("READYTOPLAY\n");
    }

    void Update()
    {
        if (tcpClient != null && tcpClient.IsConnected())
        {
            // You can add UI updates here to reflect the connection state
        }
    }
}
