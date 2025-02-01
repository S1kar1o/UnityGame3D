using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnityTcpClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private bool isReconnecting = false;
    private bool isFirstConnectionLog = true;
    private readonly byte[] buffer = new byte[1024];
    private static UnityTcpClient instance;
    private bool isConnected = false;
    // Stores movement information for objects in 3D space
    private Dictionary<string, MoveInfo> moveInfos = new Dictionary<string, MoveInfo>();

    public int goldAmount=50, woodAmount=50, rockAmount = 50;

    public event Action<string> OnSceneChangeRequested;  // Подія для зміни сцени
    private string _sceneToMove;

    public string SceneToMove
    {
        get => _sceneToMove;
        set
        {
            if (_sceneToMove != value)
            {
                _sceneToMove = value;
                OnSceneChangeRequested?.Invoke(_sceneToMove);  // Сповіщаємо про зміну
            }
        }
    }
    private class MoveInfo
    {
        public Vector3 startPosition;
        public Vector3 targetPosition;
        public float startTime;
        public float journeyTime;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async Task ConnectToServer(string ipAddress = "127.0.0.1", int port = 8080)
    {
        try
        {
            if (isFirstConnectionLog)
            {
                Debug.Log($"Attempting to connect to server at {ipAddress}:{port}");
                isFirstConnectionLog = false;
            }

            client = new TcpClient();
            await client.ConnectAsync(ipAddress, port);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log("Connected to server.");
            await SendMessage("Hello from Unity client!");
            ReceiveMessages();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error connecting to server: {ex.Message}");
            if (!isReconnecting)
            {
                isReconnecting = true;
                await Reconnect(ipAddress, port);
            }
        }
    }

    public async new Task SendMessage(string message)
    {
        if (isConnected && client.Connected && stream != null)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                Debug.Log("Message sent to server: " + message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending message: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("Cannot send message, no active connection.");
            if (!isReconnecting)
            {
                isReconnecting = true;
                await Reconnect("127.0.0.1", 8080);
            }
        }
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
            SceneToMove = "Playble";

        }
    }
public async void ReceiveMessages()
    {
        while (isConnected)
        {
            try
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log("Received from server: " + receivedMessage);
                    if (receivedMessage.Contains("Другий гравець підключився"))
                    {
                        Debug.Log("Гра починається...");
                        StartCoroutine(LoadSceneAsync("LoadingScene"));
                    }
                    if (receivedMessage.StartsWith("SPAWN"))
                    {
                        ProcessSpawnMessage(receivedMessage);
                    }
                    else if (receivedMessage.StartsWith("BUILT"))
                    {
                        ProcessBuiltMessage(receivedMessage);
                    }
                    else if (receivedMessage.StartsWith("MOVE"))
                    {
                        ProcessMoveMessage(receivedMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error receiving message: {ex.Message}");
                break;
            }
        }
    }

    private void ProcessSpawnMessage(string message)
    {
        string[] spawns = message.Split(new[] { "SPAWN " }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var spawn in spawns)
        {
            string[] parts = spawn.Trim().Split(' ');
            if (parts.Length == 4)
            {
                string unitName = parts[0];
                if (float.TryParse(parts[1], out float x) &&
                    float.TryParse(parts[2], out float y) &&
                    float.TryParse(parts[3], out float z))
                {
                    GameObject unitPrefab = Resources.Load<GameObject>("Prefabs/" + unitName);
                    if (unitPrefab != null)
                    {
                        Vector3 spawnPosition = new Vector3(x, y, z);
                        Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
                        Debug.Log($"Unit spawned: {unitName} at position {spawnPosition}");
                    }
                    else
                    {
                        Debug.LogError($"Unit prefab {unitName} not found.");
                    }
                }
                else
                {
                    Debug.LogError("Invalid spawn message format. Coordinates must be numbers.");
                }
            }
            else
            {
                Debug.LogError("Invalid spawn message format. Expected format: SPAWN <unitName> <x> <y> <z>");
            }
        }
    }

    private void ProcessMoveMessage(string message)
    {
        string[] moves = message.Split(new[] { "MOVE " }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var move in moves)
        {
            string[] parts = move.Trim().Split(' ');
            if (parts.Length == 7)
            {
                string objectName = parts[0];
                if (float.TryParse(parts[1], out float startX) &&
                    float.TryParse(parts[2], out float startY) &&
                    float.TryParse(parts[3], out float startZ) &&
                    float.TryParse(parts[4], out float endX) &&
                    float.TryParse(parts[5], out float endY) &&
                    float.TryParse(parts[6], out float endZ))
                {
                    GameObject obj = GameObject.Find(objectName);
                    if (obj != null)
                    {
                        moveInfos[objectName] = new MoveInfo
                        {
                            startPosition = new Vector3(startX, startY, startZ),
                            targetPosition = new Vector3(endX, endY, endZ),
                            startTime = Time.time,
                            journeyTime = 1f // Customize duration if required
                        };
                    }
                    else
                    {
                        Debug.LogError($"Object {objectName} not found");
                    }
                }
                else
                {
                    Debug.LogError("Invalid move message format. Coordinates must be numbers.");
                }
            }
            else
            {
                Debug.LogError("Invalid move message format. Expected format: MOVE <objectName> <startX> <startY> <startZ> <endX> <endY> <endZ>");
            }
        }
    }

    void Update()
    {
        foreach (var pair in moveInfos)
        {
            string objectName = pair.Key;
            MoveInfo moveInfo = pair.Value;

            GameObject obj = GameObject.Find(objectName);
            if (obj != null)
            {
                float elapsed = (Time.time - moveInfo.startTime) / moveInfo.journeyTime;
                obj.transform.position = Vector3.Lerp(moveInfo.startPosition, moveInfo.targetPosition, elapsed);
            }
        }
    }
    private void ProcessBuiltMessage(string message)
    {
        string[] builds = message.Split(new[] { "BUILT " }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var build in builds)
        {
            string[] parts = build.Trim().Split(' ');
            if (parts.Length == 7)
            {
                string prefabName = parts[0];
                if (float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float buildX) &&
                    float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float buildY) &&
                    float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float buildZ) &&
                    float.TryParse(parts[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float rotX) &&
                    float.TryParse(parts[5], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float rotY) &&
                    float.TryParse(parts[6], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float rotZ))
                {
                    GameObject buildingPrefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
                    if (buildingPrefab != null)
                    {
                        Vector3 buildPosition = new Vector3(buildX, buildY, buildZ);
                        Quaternion buildRotation = Quaternion.Euler(rotX, rotY, rotZ);
                        GameObject newBuilding = Instantiate(buildingPrefab, buildPosition, buildRotation);
                        Debug.Log($"Building constructed: {prefabName} at position {buildPosition} with rotation {buildRotation.eulerAngles}");
                    }
                    else
                    {
                        Debug.LogError($"Building prefab {prefabName} not found.");
                    }
                }
                else
                {
                    Debug.LogError("Invalid build message format. Coordinates and rotation must be numbers.");
                }
            }
            else
            {
                Debug.LogError("Invalid build message format. Expected format: BUILT <prefabName> <buildX> <buildY> <buildZ> <rotX> <rotY> <rotZ>");
            }
        }
    }
    private async Task Reconnect(string ipAddress, int port)
    {
        Debug.Log("Reconnecting...");
        await Task.Delay(5000);
        await ConnectToServer(ipAddress, port);
        isReconnecting = false;
    }

    public bool IsConnected()
    {
        return client != null && client.Connected && isConnected;
    }

    public void Close()
    {
        try
        {
            stream?.Close();
            client?.Close();
            isConnected = false;
            Debug.Log("Connection closed.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error closing connection: {ex.Message}");
        }
    }
}
