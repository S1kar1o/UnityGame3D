using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    public int IDclient;
    public int goldAmount = 50, woodAmount = 50, rockAmount = 50;

    public event Action<string> OnSceneChangeRequested;  // Подія для зміни сцени
    private string _sceneToMove;
    public TokenManager tokenManager;
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
    public void OnConnectedToServer()
    {
        isConnected = true;
        TryLoginWithToken(); // Після підключення намагаємося здійснити вхід за токеном
    }

    // Метод для спроби авторизації через токен
    private async void TryLoginWithToken()
    {
        if (isConnected)
        {
            string key = tokenManager.GetTokenFromFile(); // Отримуємо токен
            if (key != null)
            {
                Debug.Log(101);
                await SendMessage("LOGINBYTOKEN " + key); // Відправляємо повідомлення з токеном
            }
        }
    }
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            ConnectToServer(); // Підключення до сервера
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
                Debug.Log($"Attempting to connect to server at {ipAddress}:{port}\n");
                isFirstConnectionLog = false;
            }

            client = new TcpClient();
            await client.ConnectAsync(ipAddress, port);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log("Connected to server.");
/*            await SendMessage("Hello from Unity client!\n");
*/            OnConnectedToServer();

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
                byte[] data = Encoding.UTF8.GetBytes(message+"\n");
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

                    // Розділяємо отриманий рядок на окремі повідомлення
                    string[] messages = receivedMessage.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string message in messages)
                    {
                        // Оброюляємо кожне окреме повідомлення
                        if (message.Contains("YourID:"))
                        {
                            InitializeID(message);
                        }
                        else if (message.Contains("Другий гравець підключився"))
                        {
                            Debug.Log("Гра починається...");
                            StartCoroutine(LoadSceneAsync("LoadingScene"));
                        }
                        else if (message.StartsWith("SPAWN"))
                        {
                            ProcessSpawnMessage(message);
                        }
                        else if (message.StartsWith("BUILT"))
                        {
                            ProcessBuiltMessage(message);
                        }
                        else if (message.StartsWith("MOVE"))
                        {
                            /*                            ProcessMoveMessage(message);
                            */
                        } 
                        else if (message.StartsWith("LOGIN_SUCCESS_WITHOUT_TOKEN"))
                        {
                            Debug.Log(120);
                            // Розбиваємо повідомлення на частини
                            string[] parts = message.Split(' ');

                            if (parts.Length == 3) // Перевіряємо, чи є токен і userId
                            {
                                string token = parts[1];
                                string userId = parts[2];
                                Debug.Log(120);

                                // Зберігаємо токен і userId в файл
                                tokenManager.SaveTokenToFile(token, userId);
                            }
                            else
                            {
                                Debug.LogError("Невірний формат повідомлення LOGIN_SUCCESS");
                            }
                            StartCoroutine(LoadSceneAsync("SampleScene"));
                        }
                        else if (message.StartsWith("LOGIN_SUCCESS"))
                        {
                            StartCoroutine(LoadSceneAsync("SampleScene"));
                        }
                        else
                        {
                            Debug.LogWarning($"Unknown message type: {message}");
                        }
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
    private void InitializeID(string message)
    {
        Debug.Log(message);
        char id = message[7];
        Debug.Log(id);

        IDclient = id - '0';
        Debug.Log(id + ":::::::");
    }
    private void ProcessSpawnMessage(string message)
    {
        string[] parts = message.Split(' ');

        if (parts.Length == 8 && parts[0] == "SPAWN")
        {
            string unitName = parts[1];
            string objX = parts[2];
            string objY = parts[3];
            string objZ = parts[4];
            string rotX = parts[5];
            string rotY = parts[6];
            string rotZ = parts[7];

            objX = objX.Replace(',', '.');
            objY = objY.Replace(',', '.');
            objZ = objZ.Replace(',', '.');
            rotY = rotY.Replace(',', '.');
            Debug.Log(unitName + " " + objX + " " + objY + " " + objZ + " " + rotX + " " + rotY + " " + rotZ);

            if (float.TryParse(objX, NumberStyles.Float, CultureInfo.InvariantCulture, out float BuildX) &&
                 float.TryParse(objY, NumberStyles.Float, CultureInfo.InvariantCulture, out float BuildY) &&
                 float.TryParse(objZ, NumberStyles.Float, CultureInfo.InvariantCulture, out float BuildZ) &&
                 float.TryParse(rotX, NumberStyles.Float, CultureInfo.InvariantCulture, out float RotX) &&
                 float.TryParse(rotY, NumberStyles.Float, CultureInfo.InvariantCulture, out float RotY) &&
                 float.TryParse(rotZ, NumberStyles.Float, CultureInfo.InvariantCulture, out float RotZ))
            {

                
                GameObject unitPrefab = Resources.Load<GameObject>("Prefabs/Units/" + unitName);
                if (unitPrefab != null)
                {
                    Vector3 spawnPosition = new Vector3(BuildX, BuildY, BuildZ);
                    Quaternion spawnRotation = Quaternion.Euler(0, RotY, 0);
                    Debug.Log("124: " + spawnPosition + " " + spawnRotation);

                    Instantiate(unitPrefab, spawnPosition, spawnRotation);
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
            Debug.Log(message);
            Debug.LogError("Invalid spawn message format. Expected format: SPAWN <unitName> <x> <y> <z>");
        }

    }

   /* private void ProcessMoveMessage(string message)
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
    }*/

    void Update()
    {
       
    }
    private void ProcessBuiltMessage(string message)
    {
        string[] parts = message.Split(' ');

        if (parts.Length == 8 && parts[0] == "BUILT")
        {
            string prefabName = parts[1];
            string buildXStr = parts[2];
            string buildYStr = parts[3];
            string buildZStr = parts[4];
            string rotXStr = parts[5];
            string rotYStr = parts[6];
            string rotZStr = parts[7];

            // Заміна коми на крапку
            buildXStr = buildXStr.Replace(',', '.');
            buildYStr = buildYStr.Replace(',', '.');
            buildZStr = buildZStr.Replace(',', '.');
            rotXStr = rotXStr.Replace(',', '.');
            rotYStr = rotYStr.Replace(',', '.');
            rotZStr = rotZStr.Replace(',', '.');

            // Парсинг рядків у числа
            if (float.TryParse(buildXStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float buildX) &&
                float.TryParse(buildYStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float buildY) &&
                float.TryParse(buildZStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float buildZ) &&
                float.TryParse(rotXStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float rotX) &&
                float.TryParse(rotYStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float rotY) &&
                float.TryParse(rotZStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float rotZ))
            {
                LoadAndInstantiateBuilding(prefabName, buildXStr, buildYStr, buildZStr, rotXStr, rotYStr, rotZStr); // Рядок 302, який потрібно змінити
            }
            else
            {
                Debug.LogError("Invalid build message format. Coordinates and rotation must be numbers.");
            }
        }
        else
        {
            Debug.LogError("Invalid build message format.");
        }
    }

    private void LoadAndInstantiateBuilding(string prefabName, string buildXStr, string buildYStr, string buildZStr, string rotXStr, string rotYStr, string rotZStr)
    {
        // Парсинг рядків у числа
        if (float.TryParse(buildXStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float buildX) &&
            float.TryParse(buildYStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float buildY) &&
            float.TryParse(buildZStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float buildZ) &&
            float.TryParse(rotXStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float rotX) &&
            float.TryParse(rotYStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float rotY) &&
            float.TryParse(rotZStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float rotZ))
        {
            GameObject buildingPrefab = Resources.Load<GameObject>("Prefabs/Buildings/" + prefabName);
            if (buildingPrefab != null)
            {
                Vector3 buildPosition = new Vector3(buildX, buildY, buildZ);
                Quaternion buildRotation = Quaternion.Euler(rotX, rotY, rotZ);
                GameObject newBuilding = Instantiate(buildingPrefab, buildPosition, buildRotation);
                newBuilding.tag = "Enemy"; 
                Debug.Log($"Building constructed: {prefabName} at position {buildPosition} with rotation {buildRotation.eulerAngles}");
            }
            else
            {
                Debug.LogError($"Building prefab {prefabName} not found.");
            }
        }
        else
        {
            Debug.LogError("Invalid number format received from server.");
        }
    }

    // ... (в коді, де ви викликаєте LoadAndInstantiateBuilding)
    // Замість float передавайте рядки:
    // LoadAndInstantiateBuilding(prefabName, buildX.ToString(), buildY.ToString(), buildZ.ToString(), rotX.ToString(), rotY.ToString(), rotZ.ToString());
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
