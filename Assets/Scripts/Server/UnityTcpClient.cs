using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class UnityTcpClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private bool isReconnecting = false;
    private bool isFirstConnectionLog = true;
    private readonly byte[] buffer = new byte[1024];
    private static UnityTcpClient _instance;
    private static readonly object _lock = new object();
    public ButtonControler buttonControler;
    private bool isConnected = false;
    // Stores movement information for objects in 3D space
    public int IDclient;
    public int goldAmount = 1000, woodAmount = 500, rockAmount = 500;

    public bool enemyReady = false;
    public event Action<string> OnSceneChangeRequested;  // Подія для зміни сцени
    private string _sceneToMove;
    public TokenManager tokenManager;
    public int idUnitGeneratedAtServer = 0;


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

    public string[] tagOwner = new string[] { "Unit", "Enemy" };
    public UIresourceControll uIresource;
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
    // Публічний доступ до екземпляра
    public static UnityTcpClient Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    // Пошук існуючого екземпляра на сцені
                    _instance = FindObjectOfType<UnityTcpClient>();

                    if (_instance == null)
                    {
                        // Створення нового об'єкта, якщо не знайдено
                        GameObject singletonObject = new GameObject(typeof(UnityTcpClient).Name);
                        _instance = singletonObject.AddComponent<UnityTcpClient>();
                        DontDestroyOnLoad(singletonObject);
                    }
                }
                return _instance;
            }
        }
    }

    void Awake()
    {
        lock (_lock)
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;

            Initialize();
        }
    }

    private void Initialize()
    {
        // Тут ініціалізація підключення
        ConnectToServer();
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
            */
            OnConnectedToServer();

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
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
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
                            string message2 = message.Replace(',', '.');
                            string[] parts = message2.Split(' ');
                            int unitId = int.Parse(parts[1]);
                            List<Vector3> pathPoints = new List<Vector3>();
                            for (int i = 2; i < parts.Length; i += 3)
                            {
                                float x = float.Parse(parts[i], CultureInfo.InvariantCulture);
                                float y = float.Parse(parts[i + 1], CultureInfo.InvariantCulture);
                                float z = float.Parse(parts[i + 2], CultureInfo.InvariantCulture);
                                pathPoints.Add(new Vector3(x, y, z));
                            }

                            GameObject unit = FindObjectByServerID(unitId);
                            StartCoroutine(MoveStepByStep(unit, pathPoints));


                        }
                        else if (message.StartsWith("ATTACK"))
                        {
                            ProcessAttackMessage(message);
                        }
                        else if (message.StartsWith("EXTRACT"))
                        {
                            ProcessExtractMessage(message);
                        }
                        else if (message.StartsWith("LOADED"))
                        {
                            ProcessLoadedMessage(message);
                        }
                        else if (message.StartsWith("ID_GENERATED"))
                        {
                            ProcessLoadedIDfromServer(message);
                        }
                        else if (message.StartsWith("DIE"))
                        {
                            ProcessDieMessage(message);
                        }
                        else if (message.StartsWith("GOEXTRACT"))
                        {
                            Debug.Log(1);
                            ProcessExtractByUnitMessage(message);

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
                        else if (message.StartsWith("WON") || message.StartsWith("LOSE") || message.StartsWith("Opponent disconnected"))
                        {
                            Debug.Log(102);

                            buttonControler.endGamePanelIsActive = true;
                            buttonControler.PanelEndGameFromServerButton();
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
    private void ProcessLoadedMessage(string message)
    {
        string[] parts = message.Split(' ');
        if (parts.Length < 1)
        {
            Debug.LogWarning($"Неправильний формат Load: {message}");
            return;
        }
        enemyReady = true;

    }

    private void ProcessLoadedIDfromServer(string message)
    {
        string[] parts = message.Split(' ');
        if (parts.Length < 2)
        {
            Debug.LogWarning($"Неправильний формат Load: {message}");
            return;
        }
        int objectID = int.Parse(parts[1]); // ID об'єкта

        idUnitGeneratedAtServer = objectID;
    }
    private void InitializeID(string message)
    {
        Debug.Log(message);
        char id = message[7];
        Debug.Log(id);

        IDclient = id - '0';
        Debug.Log(id + ":::::::");
    }
    IEnumerator MoveStepByStep(GameObject unit, List<Vector3> pathPoints, float threshold = 0.5f)
    {
        if (unit == null || !unit.activeInHierarchy)
        {
            Debug.LogWarning("Об'єкт не активний або не існує.");
            yield break;
        }

        NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            Debug.LogWarning("NavMeshAgent не готовий або не на NavMesh.");
            yield break;
        }

        agent.ResetPath();

        foreach (var target in pathPoints)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(target, out hit, 1.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                Debug.Log($"[MOVE] Встановлена ціль: {hit.position}");

                yield return new WaitUntil(() => !agent.pathPending);

                if (agent.pathStatus != NavMeshPathStatus.PathComplete)
                {
                    Debug.LogWarning($"[MOVE] Шлях до {hit.position} неможливий: {agent.pathStatus}");
                    continue;
                }
            }
            else
            {
                Debug.LogWarning($"[MOVE] Точка {target} поза NavMesh.");
                continue;
            }
        }
            Debug.Log("Маршрут завершено.");
    }

    private void ProcessSpawnMessage(string message)
    {
        string[] parts = message.Split(' ');

        if (parts.Length == 9 && parts[0] == "SPAWN")
        {
            int id = int.Parse(parts[1]);
            string unitName = parts[2];
            string objX = parts[3].Replace(',', '.');
            string objY = parts[4].Replace(',', '.');
            string objZ = parts[5].Replace(',', '.');
            string rotX = parts[6];
            string rotY = parts[7].Replace(',', '.');
            string rotZ = parts[8];

            Debug.Log(id + " " + unitName + " " + objX + " " + objY + " " + objZ + " " + rotX + " " + rotY + " " + rotZ);

            if (float.TryParse(objX, NumberStyles.Float, CultureInfo.InvariantCulture, out float BuildX) &&
                float.TryParse(objY, NumberStyles.Float, CultureInfo.InvariantCulture, out float BuildY) &&
                float.TryParse(objZ, NumberStyles.Float, CultureInfo.InvariantCulture, out float BuildZ) &&
                float.TryParse(rotX, NumberStyles.Float, CultureInfo.InvariantCulture, out float RotX) &&
                float.TryParse(rotY, NumberStyles.Float, CultureInfo.InvariantCulture, out float RotY) &&
                float.TryParse(rotZ, NumberStyles.Float, CultureInfo.InvariantCulture, out float RotZ))
            {
                Vector3 spawnPosition = new Vector3(BuildX, BuildY, BuildZ);
                Quaternion spawnRotation = Quaternion.Euler(0, RotY, 0);

                // Перевіряємо, чи NavMesh готовий
                if (!IsNavMeshReady(spawnPosition))
                {
                    Debug.LogWarning($"NavMesh не готовий для позиції {spawnPosition}. Відкладений спавн...");
                    StartCoroutine(SpawnWhenNavMeshReady(id, unitName, spawnPosition, spawnRotation));
                    return;
                }

                SpawnUnit(id, unitName, spawnPosition, spawnRotation);
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

    private bool IsNavMeshReady(Vector3 position)
    {
        return NavMesh.SamplePosition(position, out NavMeshHit hit, 10f, NavMesh.AllAreas);
    }

    private IEnumerator SpawnWhenNavMeshReady(int id, string unitName, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        int maxAttempts = 50; // Максимальна кількість спроб (5 секунд при 10 спробах/сек)
        float delayBetweenAttempts = 0.1f;

        for (int i = 0; i < maxAttempts; i++)
        {
            if (IsNavMeshReady(spawnPosition))
            {
                SpawnUnit(id, unitName, spawnPosition, spawnRotation);
                yield break;
            }
            yield return new WaitForSeconds(delayBetweenAttempts);
        }

        Debug.LogError($"Не вдалося знайти NavMesh для {unitName} на позиції {spawnPosition} після {maxAttempts} спроб!");
        // Спавнимо без NavMesh як резервний варіант
        SpawnUnit(id, unitName, spawnPosition, spawnRotation, forceSpawn: true);
    }

    private void SpawnUnit(int id, string unitName, Vector3 spawnPosition, Quaternion spawnRotation, bool forceSpawn = false)
    {
        GameObject unitPrefab = Resources.Load<GameObject>("Prefabs/Units/" + unitName);
        if (unitPrefab != null)
        {
            CameraMoving cameraMoving = FindObjectOfType<CameraMoving>();
            GameObject unit = Instantiate(unitPrefab, spawnPosition, spawnRotation);
            ServerId ID = unit.GetComponent<ServerId>();
            ID.serverId = id;
            if (!forceSpawn)
            {
                NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
                if (agent != null && !agent.isOnNavMesh)
                {
                    Debug.LogWarning($"Юніт {unitName} створений, але не розміщений на NavMesh!");
                }
            }
            cameraMoving.enemys.Add(unitPrefab);

            Debug.Log($"Unit spawned: {unitName} at position {spawnPosition}");
        }
        else
        {
            Debug.LogError($"Unit prefab {unitName} not found.");
        }
    }
    private void ProcessExtractByUnitMessage(string message)
    {
        string[] parts = message.Split(' ');
        if (parts.Length < 3)
        {
            Debug.LogWarning($"Неправильний формат GOEXTRACT: {message}");
            return;
        }

        if (!int.TryParse(parts[1], out int unitId) || !int.TryParse(parts[2], out int targetId))
        {
            Debug.LogError($"Помилка парсингу ID у GOEXTRACT: {message}");
            return;
        }
        Debug.Log(2);

        GameObject unit = FindObjectByServerID(unitId);
        GameObject resource = FindObjectByServerID(targetId);
        Debug.Log(4);

        if (unit == null)
        {
            Debug.LogError($"Юніт з ID {unitId} не знайдено!");
            return;
        }
        if (resource == null)
        {
            Debug.LogError($"Ресурс з ID {targetId} не знайдено!");
            return;
        }
        Debug.Log(5);

        VillagerParametrs unitObj = unit.GetComponent<VillagerParametrs>();
        if (unitObj == null)
        {
            Debug.LogError($"Компонент VillagerParametrs не знайдено на юніті з ID {unitId}!");
            return;
        }
        Debug.Log(3);

        unitObj.IsRunningToResource(true);
        unitObj.MoveToResource(resource); // Рух до ресурсу
        Debug.Log($"Юніт {unitId} почав видобуток ресурсу {targetId}");
    }
    private void ProcessExtractMessage(string message)
    {
        try
        {
            // Розбиваємо повідомлення на частини
            string[] parts = message.Split(' ');
            if (parts.Length < 3)
            {
                Debug.LogError("Невірний формат повідомлення EXTRACT: " + message);
                return;
            }

            int resourceID = int.Parse(parts[1]); // ID ресурсу
            int amount = int.Parse(parts[2]);    // Кількість ресурсу

            // Знаходимо об'єкт ресурсу за ID (припустимо, що ID зберігається в компоненті AmountResource)
            GameObject resource = FindObjectByServerID(resourceID);
            AmountResource amountResource = resource.GetComponent<AmountResource>();
            if (resource != null)
            {
                amountResource.Extraction(amount);
                Debug.Log($"Видобуто {amount} одиниць ресурсу з об'єкта ID {resourceID}.");
            }
            else
            {
                Debug.LogError($"Ресурс з ID {resourceID} не знайдено.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Помилка при обробці повідомлення EXTRACT: {ex.Message}");
        }
    }
    private void ProcessDieMessage(string message)
    {
        try
        {
            // Розбиваємо повідомлення на частини
            string[] parts = message.Split(' ');
            if (parts.Length < 2)
            {
                Debug.LogError("Невірний формат повідомлення DIE: " + message);
                return;
            }

            int objectID = int.Parse(parts[1]); // ID об'єкта

            // Знаходимо об'єкт за ID
            GameObject obj = FindObjectByServerID(objectID);
            if (obj != null)
            {
                Destroy(obj);
                Debug.Log($"Об'єкт з ID {objectID} був знищений.");
            }
            else
            {
                Debug.LogError($"Об'єкт з ID {objectID} не знайдено.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Помилка при обробці повідомлення DIE: {ex.Message}");
        }
    }

    // Допоміжна функція для пошуку об'єкта за ID
    private GameObject FindObjectByID(int objectID)
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.GetInstanceID() == objectID)
            {
                return obj;
            }
        }
        return null;
    }
    private GameObject FindObjectByServerID(int objectID)
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            ServerId sr = obj.GetComponent<ServerId>();
            if (sr != null && sr.serverId == objectID)
            {
                return obj;
            }
        }
        return null;
    }
    private void ProcessAttackMessage(string message)
    {
        string[] parts = message.Split(' ');
        if (parts.Length < 4)
        {
            Debug.LogWarning($"Неправильний формат ATTACK_RESULT: {message}");
            return;
        }
        int attackerId = int.Parse(parts[1]);
        int targetId = int.Parse(parts[2]);
        int damage = int.Parse(parts[3]);

        GameObject attacker = FindObjectByServerID(attackerId);
        WarriorParametrs attackerObj = attacker.GetComponent<WarriorParametrs>();
        GameObject target = FindObjectByServerID(targetId);
        attackerObj.AttackEnemy(target);
    }
    // Допоміжна функція для пошуку ресурсу за ID
    private void ProcessBuiltMessage(string message)
    {
        string[] parts = message.Split(' ');
        if (parts.Length == 8 && parts[0] == "BUILT" && parts[1] == "Bridge3")
        {
            GameObject bridgePlacer = GameObject.Find("BridgePlacer");
            BridgePlacer bp = bridgePlacer.GetComponent<BridgePlacer>();
            string prefabName = parts[1];
            string buildXStr = parts[2];
            string buildYStr = parts[3];
            string buildZStr = parts[4];
            string buildXEnd = parts[5];
            string buildYEnd = parts[6];
            string buildZEnd = parts[7];
            Debug.Log(buildXStr + " " + buildYStr + " " + buildZStr + " " + buildXEnd + " " + buildYEnd + " " + buildZEnd);
            buildXStr = buildXStr.Replace(',', '.');
            buildYStr = buildYStr.Replace(',', '.');
            buildZStr = buildZStr.Replace(',', '.');
            buildXEnd = buildXEnd.Replace(',', '.');
            buildYEnd = buildYEnd.Replace(',', '.');
            buildZEnd = buildZEnd.Replace(',', '.');
            Debug.Log(buildXStr + " " + buildYStr + " " + buildZStr + " " + buildXEnd + " " + buildYEnd + " " + buildZEnd);

            bp.messageFromServer = true;
            bp.firstPoint = new Vector3(float.Parse(buildXStr, CultureInfo.InvariantCulture), float.Parse(buildYStr, CultureInfo.InvariantCulture), float.Parse(buildZStr, CultureInfo.InvariantCulture));
            bp.secondPoint = new Vector3(float.Parse(buildXEnd, CultureInfo.InvariantCulture), float.Parse(buildYEnd, CultureInfo.InvariantCulture), float.Parse(buildZEnd, CultureInfo.InvariantCulture));
            bp.PlaceBridge();
            bp.messageFromServer = false;
            bp.firstPoint = Vector3.zero;
            bp.secondPoint = Vector3.zero;

        }
        else if (parts.Length == 9 && parts[0] == "BUILT")
        {
            string prefabName = parts[1];
            string prefabId = parts[2];

            string buildXStr = parts[3];
            string buildYStr = parts[4];
            string buildZStr = parts[5];
            string rotXStr = parts[6];
            string rotYStr = parts[7];
            string rotZStr = parts[8];

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
                LoadAndInstantiateBuilding(prefabName, prefabId, buildXStr, buildYStr, buildZStr, rotXStr, rotYStr, rotZStr);
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

    private void LoadAndInstantiateBuilding(string prefabName, string idStr, string buildXStr, string buildYStr, string buildZStr, string rotXStr, string rotYStr, string rotZStr)
    {
        // Парсинг рядків у числа
        if (float.TryParse(buildXStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float buildX) &&
            int.TryParse(idStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) &&
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
                ServerId serverId = newBuilding.GetComponent<ServerId>();
                serverId.serverId = id;
                TowerAttack ta = newBuilding.GetComponent<TowerAttack>();
                ta.enabled = true;


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

    public void ReloadRscClient()
    {
        goldAmount = 0;
        woodAmount = 0;
        rockAmount = 0;
    }
}
