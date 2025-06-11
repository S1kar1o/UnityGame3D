using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using NativeWebSocket;

public class UnityTcpClient : MonoBehaviour
{
    public LoginingToDB loginController;
    public ButtonControler buttonControler;
    public TokenManager tokenManager;
    public UIresourceControll uIresource;
    public CameraMoving cameraMoving;
    private WebSocket ws;
    private bool isReconnecting = false;
    private bool isFirstConnectionLog = true;
    private StringBuilder messageBuffer = new StringBuilder();
    private static UnityTcpClient _instance;
    private static readonly object _lock = new object();
    private bool isConnected = false;
    public int IDclient;
    public int goldAmount = 500, woodAmount = 500, rockAmount = 500;
    public bool enemyReady = false;
    public event Action<string> OnSceneChangeRequested;
    private string _sceneToMove;
    public int idUnitGeneratedAtServer = 0;
    [SerializeField] private string serverIp = "c-server-d27j.onrender.com";
    public string[] tagOwner = new string[] { "Unit", "Enemy" };
    private static int loginTokenAttempts = 0;
    private const int MAX_LOGIN_TOKEN_ATTEMPTS = 3;

    public bool statusOponent = false;
    public string UserRaiting = "100";
    public string UserId;
    public string UserNickName = "David";
    public string SceneToMove
    {
        get => _sceneToMove;
        set
        {
            if (_sceneToMove != value)
            {
                _sceneToMove = value;
                OnSceneChangeRequested?.Invoke(_sceneToMove);
            }
        }
    }

    public static UnityTcpClient Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UnityTcpClient>();
                    if (_instance == null)
                    {
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
        }
        Initialize();
    }

    private void Initialize()
    {
        ConnectToServer();
    }

    public async void ConnectToServer()
    {
        try
        {
            if (isFirstConnectionLog)
            {
                Debug.Log($"Attempting to connect to WebSocket server at wss://{serverIp}/ws");
                isFirstConnectionLog = false;
            }

            ws = new WebSocket($"wss://{serverIp}/ws");
            ws.OnOpen += () =>
            {
                isConnected = true;
                Debug.Log("Connected to server.");
                OnConnectedToServer();
            };
            ws.OnMessage += (bytes) =>
            {
                string receivedMessage = Encoding.UTF8.GetString(bytes).Trim();
                messageBuffer.Append(receivedMessage + "\n");
                ProcessReceivedMessages();
            };
            ws.OnError += (e) =>
            {
                Debug.LogError($"WebSocket error: {e}");
                isConnected = false;
                if (!isReconnecting) StartCoroutine(Reconnect());
            };
            ws.OnClose += (e) =>
            {
                Debug.Log($"WebSocket closed: {e}");
                isConnected = false;
                if (!isReconnecting) StartCoroutine(Reconnect());
            };

            await ws.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error connecting to server: {ex.Message}");
            if (!isReconnecting)
            {
                StartCoroutine(Reconnect());
            }
        }
    }

    public void OnConnectedToServer()
    {
        TryLoginWithToken();
    }

    private async void TryLoginWithToken()
    {

        if (!isConnected) return;

        string key = tokenManager.GetTokenFromFile();
        if (!string.IsNullOrEmpty(key) && loginTokenAttempts < MAX_LOGIN_TOKEN_ATTEMPTS)
        {
            Debug.Log($"Attempting login with token (attempt {loginTokenAttempts + 1}/{MAX_LOGIN_TOKEN_ATTEMPTS}).");
            loginTokenAttempts++;
            await SendMessage($"LOGINBYTOKEN {key}");
            loginController.currentAnimationCoroutine = StartCoroutine(loginController.waitingAnimation());
        }
        else
        {
            Debug.Log("No token found or max token attempts reached, attempting login with email.");
            loginTokenAttempts = 0;
        }
    }

    public async Task SendMessage(string message)
    {
        if (isConnected && ws != null && ws.State == WebSocketState.Open)
        {
            try
            {
                Debug.Log($"Sending to server: {message.Trim()}");
                await ws.SendText(message + "\n");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending message: {ex.Message}");
                isConnected = false;
                if (!isReconnecting)
                {
                    StartCoroutine(Reconnect());
                }
            }
        }
        else
        {
            Debug.LogError("Cannot send message, no active connection.");
            if (!isReconnecting)
            {
                StartCoroutine(Reconnect());
            }
        }
    }

    private void ProcessReceivedMessages()
    {
        string currentBuffer = messageBuffer.ToString();
        int newlineIndex;
        while ((newlineIndex = currentBuffer.IndexOf('\n')) >= 0)
        {
            string message = currentBuffer.Substring(0, newlineIndex).Trim();
            currentBuffer = currentBuffer.Substring(newlineIndex + 1);
            messageBuffer.Clear();
            messageBuffer.Append(currentBuffer);

            if (string.IsNullOrEmpty(message)) continue;

            Debug.Log($"Received from server: {message}");

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
            else if (message.StartsWith("USER_INFORMATION"))
            {
                Conecting conecting = FindAnyObjectByType<Conecting>();
                if (conecting != null)
                {
                    conecting.UpdateInformation(message);
                    conecting.stopWaitingAnimation();

                }
            }
            else if (message.StartsWith("WALL"))
            {
                WallGenerator wallGenerator = FindAnyObjectByType<WallGenerator>();
                if (wallGenerator != null)
                {
                    wallGenerator.HandleWallConstructionMessage(message);
                }
                else
                {
                    Debug.LogError("WallGenerator not found.");
                }
            }
            else if (message.StartsWith("BUILT"))
            {
                ProcessBuiltMessage(message);
            }
            else if (message.StartsWith("Oponent_Loaded"))
            {
                statusOponent = true;
            }
            else if (message.StartsWith("MOVE"))
            {
                string message2 = message.Replace(',', '.');
                string[] parts = message2.Split(' ');
                int unitId = int.Parse(parts[1]);
                Vector3 postionObject = new Vector3(
                    float.Parse(parts[2], CultureInfo.InvariantCulture),
                    float.Parse(parts[3], CultureInfo.InvariantCulture),
                    float.Parse(parts[4], CultureInfo.InvariantCulture)
                );
                Vector3 rotateObject = new Vector3(
                    float.Parse(parts[5], CultureInfo.InvariantCulture),
                    float.Parse(parts[6], CultureInfo.InvariantCulture),
                    float.Parse(parts[7], CultureInfo.InvariantCulture)
                );
                Vector3 destinationFromServer = new Vector3(
                    float.Parse(parts[8], CultureInfo.InvariantCulture),
                    float.Parse(parts[9], CultureInfo.InvariantCulture),
                    float.Parse(parts[10], CultureInfo.InvariantCulture)
                );

                GameObject unit = FindObjectByServerID(unitId);
                var extractor = unit?.GetComponent<VillagerParametrs>();
                if (extractor != null)
                {
                    if (extractor is WarriorParametrs warrior)
                    {
                        warrior.targetEnemy = null;
                    }
                    else if (extractor is VillagerParametrs villager)
                    {
                        villager.StopExtracting();
                        villager.StopBuilding();
                    }
                    MoveObject(unit, postionObject, rotateObject, destinationFromServer);
                }
                else
                {
                    extractor?.StopExtracting();
                }
            }
            else if (message.StartsWith("ATTACK"))
            {
                ProcessAttackMessage(message);
            }
            else if (message.StartsWith("FRIENDLIST_JSON"))
            {
                string json = message.Substring("FRIENDLIST_JSON ".Length); // відрізаємо префікс

                Conecting conecting = FindAnyObjectByType<Conecting>();
                if (conecting != null)
                {
                    conecting.LoadFriendsMessage(json);
                    conecting.stopWaitingAnimation();

                }
            }
            else if (message.StartsWith("LISTING_STATUS_OK"))
            {
                string json = message.Substring("LISTING_STATUS_OK ".Length); // відрізаємо префікс

                Conecting conecting = FindAnyObjectByType<Conecting>();
                if (conecting != null)
                {
                    conecting.LoadFriendListFromMessages(json);
                    conecting.stopWaitingAnimation();

                }
            }
            else if (message.StartsWith("LISTING_STATUS_EMPTY"))
            {
                Conecting conecting = FindAnyObjectByType<Conecting>();
                if (conecting != null)
                {
                    conecting.activateLettersList();
                    conecting.stopWaitingAnimation();

                }
            }
            else if (message.StartsWith("FRIENDLIST_EMPTY"))
            {
                Conecting conecting = FindAnyObjectByType<Conecting>();
                if (conecting != null)
                {
                    conecting.stopWaitingAnimation();
                    conecting.activateFriendList();

                }
            }
            else if (message.StartsWith("EXTRACT"))
            {
                ProcessExtractMessage(message);
            }
            else if (message.StartsWith("START_BUILDING"))
            {
                ProcessStartBuildingMessage(message);
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
                ProcessExtractByUnitMessage(message);
            }
            else if (message.StartsWith("LOGIN_SUCCESS_WITHOUT_TOKEN"))
            {
                string[] parts = message.Split(' ');
                if (parts.Length == 3)
                {
                    string token = parts[1];
                    string userId = parts[2];
                    tokenManager.SaveTokenToFile(token, userId);
                    Debug.Log($"Login successful! Token: {token}, UserId: {userId}");
                    loginTokenAttempts = 0;
                }
                StartCoroutine(LoadSceneAsync("SampleScene"));
            }
            else if (message.StartsWith("REGISTRATION_SUCCESS"))
            {
                loginController.toApplyMenu();
                loginController.stopWaitingAnimation();
            }
            else if (message.StartsWith("REGISTRATION_FAILED"))
            {
                string errorMessage = message.Substring("REGISTRATION_FAILED".Length).Trim();
                HandleRegistrationError(errorMessage);
                loginController.stopWaitingAnimation();
            }
            else if (message.StartsWith("LOGIN_FAILED"))
            {
                string errorMessage = message.Substring("LOGIN_FAILED".Length).Trim();
                HandleRegistrationError(errorMessage);
                loginController.stopWaitingAnimation();
            }
            else if (message.StartsWith("NEED_APPLY"))
            {
                loginController.ProblemAcceptGmail();
                loginController.stopWaitingAnimation();

            }
            else if (message.StartsWith("LEADER_BOARD"))
            {
                string json = message.Substring("LEADER_BOARD_JSON ".Length); // відрізаємо префікс

                Conecting conecting = FindAnyObjectByType<Conecting>();
                if (conecting != null)
                {
                    conecting.LoadUsersFromJson(json);
                }
                conecting.stopWaitingAnimation();

            }
            else if (message.StartsWith("LOGIN_SUCCESS"))
            {
                StartCoroutine(LoadSceneAsync("SampleScene"));
            }
            else if (message.StartsWith("LOSE"))
            {
                buttonControler.endGamePanelIsActive = true;
                buttonControler.PanelEndGameLoseFromServerButton();
            }
            else if (message.StartsWith("WON") || message.StartsWith("Opponent disconnected"))
            {
                cameraMoving.waiting = true;
                buttonControler.endGamePanelIsActive = true;
                buttonControler.PanelEndGameButton();
            }
            else
            {
                Debug.LogWarning($"Unknown message type: {message}");
            }
        }
    }
    private IEnumerator Reconnect()
    {
        isReconnecting = true;
        Debug.Log("Reconnecting...");
        yield return new WaitForSeconds(10f);
        Close();
        ConnectToServer();
        isReconnecting = false;
    }

    public void MoveObject(GameObject unit, Vector3 position, Vector3 rotating, Vector3 endDpoint)
    {
        if (unit == null)
        {
            Debug.LogWarning("Unit is null in MoveObject!");
            return;
        }

        NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("No NavMeshAgent found on unit!");
            return;
        }

        // Встановити стартову позицію (тільки якщо потрібно «телепортувати»)
        agent.Warp(position); // Warp краще за transform.position для агента

        // Встановити обертання
        unit.transform.rotation = Quaternion.Euler(rotating);

        // Задати цільову точку для навігації
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(endDpoint);
        }
        else
        {
            Debug.LogWarning("Agent is not on NavMesh — cannot set destination.");
        }
    }

    void OnDestroy()
    {
        Close();
        Debug.Log("UnityWebSocketClient is being destroyed!");
    }

    public bool IsConnected()
    {
        return ws != null && ws.State == WebSocketState.Open && isConnected;
    }

    public async void Close()
    {
        try
        {
            isConnected = false;
            if (ws != null && ws.State == WebSocketState.Open)
            {
                await ws.Close();
            }
            Debug.Log("Connection closed.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error closing connection: {ex.Message}");
        }
    }

    public void ReloadRscClient()
    {
        goldAmount = 500;
        woodAmount = 500;
        rockAmount = 500;
        statusOponent = false;
    }

    private void InitializeID(string message)
    {
        char id = message[7];
        IDclient = id - '0';
        Debug.Log($"Client ID set to: {IDclient}");
    }

    private void ProcessLoadedMessage(string message)
    {
        string[] parts = message.Split(' ');
        if (parts.Length < 1)
        {
            Debug.LogWarning($"Invalid LOADED message format: {message}");
            return;
        }
        enemyReady = true;
    }

    private void HandleRegistrationError(string errorMessage)
    {
        string errorCode = "";
        string description = errorMessage;
        if (errorMessage.Contains(":"))
        {
            var parts = errorMessage.Split(new[] { ':' }, 2);
            errorCode = parts[0].Trim();
            description = parts.Length > 1 ? parts[1].Trim() : "Unknown error";
        }

        if (loginController != null)
        {
            loginController.ShowRegistrationError(description);
        }
        else
        {
            Debug.LogError($"Registration error (no controller): {description}");
        }
        Debug.Log($"Registration failed: Code={errorCode}, Message={description}");
    }

    private void ProcessLoadedIDfromServer(string message)
    {
        string[] parts = message.Split(' ');
        if (parts.Length < 2)
        {
            Debug.LogWarning($"Invalid ID_GENERATED message format: {message}");
            return;
        }
        idUnitGeneratedAtServer = int.Parse(parts[1]);
    }

    private IEnumerator LoadSceneAsync(string nameScene)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(nameScene);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            Debug.Log($"Loading: {progress * 100}%");
            if (operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }
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

            if (float.TryParse(objX, NumberStyles.Float, CultureInfo.InvariantCulture, out float BuildX) &&
                float.TryParse(objY, NumberStyles.Float, CultureInfo.InvariantCulture, out float BuildY) &&
                float.TryParse(objZ, NumberStyles.Float, CultureInfo.InvariantCulture, out float BuildZ) &&
                float.TryParse(rotX, NumberStyles.Float, CultureInfo.InvariantCulture, out float RotX) &&
                float.TryParse(rotY, NumberStyles.Float, CultureInfo.InvariantCulture, out float RotY) &&
                float.TryParse(rotZ, NumberStyles.Float, CultureInfo.InvariantCulture, out float RotZ))
            {
                Vector3 spawnPosition = new Vector3(BuildX, BuildY, BuildZ);
                Quaternion spawnRotation = Quaternion.Euler(0, RotY, 0);

                if (!IsNavMeshReady(spawnPosition))
                {
                    Debug.LogWarning($"NavMesh not ready at position {spawnPosition}. Delaying spawn...");
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
            Debug.LogError($"Invalid spawn message format: {message}");
        }
    }

    private bool IsNavMeshReady(Vector3 position)
    {
        return NavMesh.SamplePosition(position, out NavMeshHit hit, 10f, NavMesh.AllAreas);
    }

    private IEnumerator SpawnWhenNavMeshReady(int id, string unitName, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        int maxAttempts = 50;
        float delayBetweenAttempts = 0.1f;

        for (int i = 0; i < maxAttempts; i++)
        {
            if (IsNavMeshReady(spawnPosition))
            {
                SpawnUnit(id, unitName, spawnPosition, spawnRotation);
                statusOponent = true;
                yield break;
            }
            yield return new WaitForSeconds(delayBetweenAttempts);
        }

        Debug.LogError($"Failed to find NavMesh for {unitName} at {spawnPosition} after {maxAttempts} attempts!");
        SpawnUnit(id, unitName, spawnPosition, spawnRotation, forceSpawn: true);
        statusOponent = true;

    }

    private void SpawnUnit(int id, string unitName, Vector3 spawnPosition, Quaternion spawnRotation, bool forceSpawn = false)
    {
        GameObject unitPrefab = Resources.Load<GameObject>("Prefabs/Units/" + unitName);
        if (unitPrefab != null)
        {
            GameObject unit = Instantiate(unitPrefab, spawnPosition, spawnRotation);
            ServerId ID = unit.GetComponent<ServerId>();
            ID.serverId = id;
            if (!forceSpawn)
            {
                NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
                if (agent != null && !agent.isOnNavMesh)
                {
                    Debug.LogWarning($"Unit {unitName} spawned but not placed on NavMesh!");
                }
            }
            cameraMoving.enemys.Add(unit);
            statusOponent = true;

            Debug.Log($"Unit spawned: {unitName} at position {spawnPosition}");
        }
        else
        {
            Debug.LogError($"Unit prefab {unitName} not found.");
        }
    }

    private void ProcessStartBuildingMessage(string message)
    {
        string[] parts = message.Split(' ');
        if (parts.Length < 3)
        {
            Debug.LogWarning($"Invalid GOEXTRACT message format: {message}");
            return;
        }

        if (!int.TryParse(parts[1], out int unitId) || !int.TryParse(parts[2], out int targetId))
        {
            Debug.LogError($"Error parsing IDs in GOEXTRACT: {message}");
            return;
        }

        GameObject unit = FindObjectByServerID(unitId);
        GameObject building = FindObjectByServerID(targetId);
        if (unit == null)
        {
            Debug.LogError($"Unit with ID {unitId} not found!");
            return;
        }
        if (building == null)
        {
            Debug.LogError($"Resource with ID {targetId} not found!");
            return;
        }

        VillagerParametrs unitObj = unit.GetComponent<VillagerParametrs>();
        if (unitObj == null)
        {

            unitObj.StopExtracting();
            unitObj.StopBuilding();

            Debug.LogError($"VillagerParametrs component not found on unit with ID {unitId}!");
            return;
        }
        Vector3 postionObject = new Vector3(
           float.Parse(parts[3], CultureInfo.InvariantCulture),
           float.Parse(parts[4], CultureInfo.InvariantCulture),
           float.Parse(parts[5], CultureInfo.InvariantCulture)
       );
        Vector3 rotateObject = new Vector3(
            float.Parse(parts[6], CultureInfo.InvariantCulture),
            float.Parse(parts[7], CultureInfo.InvariantCulture),
            float.Parse(parts[8], CultureInfo.InvariantCulture)
        );


        NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("No NavMeshAgent found on unit!");
            return;
        }

        // Встановити стартову позицію (тільки якщо потрібно «телепортувати»)
        agent.Warp(postionObject); // Warp краще за transform.position для агента

        // Встановити обертання
        unit.transform.rotation = Quaternion.Euler(rotateObject);
        unitObj.IsRunningToBuild(true);
        unitObj.moveToBuild(building);
        Debug.Log($"Unit {unitId} started extracting resource {targetId}");
    }
    private void ProcessExtractByUnitMessage(string message)
    {
        string[] parts = message.Split(' ');
        if (parts.Length < 3)
        {
            Debug.LogWarning($"Invalid GOEXTRACT message format: {message}");
            return;
        }

        if (!int.TryParse(parts[1], out int unitId) || !int.TryParse(parts[2], out int targetId))
        {
            Debug.LogError($"Error parsing IDs in GOEXTRACT: {message}");
            return;
        }
        Vector3 postionObject = new Vector3(
                  float.Parse(parts[3], CultureInfo.InvariantCulture),
                  float.Parse(parts[4], CultureInfo.InvariantCulture),
                  float.Parse(parts[5], CultureInfo.InvariantCulture)
              );
        Vector3 rotateObject = new Vector3(
            float.Parse(parts[6], CultureInfo.InvariantCulture),
            float.Parse(parts[7], CultureInfo.InvariantCulture),
            float.Parse(parts[8], CultureInfo.InvariantCulture)
        );
        GameObject unit = FindObjectByServerID(unitId);
        GameObject resource = FindObjectByServerID(targetId);
        if (unit == null)
        {
            Debug.LogError($"Unit with ID {unitId} not found!");
            return;
        }
        if (resource == null)
        {
            Debug.LogError($"Resource with ID {targetId} not found!");
            return;
        }

        VillagerParametrs unitObj = unit.GetComponent<VillagerParametrs>();
        if (unitObj == null)
        {

            unitObj.StopExtracting();
            unitObj.StopBuilding();

            Debug.LogError($"VillagerParametrs component not found on unit with ID {unitId}!");
            return;
        }
        if (unit == null)
        {
            Debug.LogWarning("Unit is null in MoveObject!");
            return;
        }

        NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("No NavMeshAgent found on unit!");
            return;
        }

        // Встановити стартову позицію (тільки якщо потрібно «телепортувати»)
        agent.Warp(postionObject); // Warp краще за transform.position для агента

        // Встановити обертання
        unit.transform.rotation = Quaternion.Euler(rotateObject);
        unitObj.IsRunningToResource(true);
        unitObj.MoveToResource(resource);
        Debug.Log($"Unit {unitId} started extracting resource {targetId}");
    }

    private void ProcessExtractMessage(string message)
    {
        try
        {
            string[] parts = message.Split(' ');
            if (parts.Length < 3)
            {
                Debug.LogError($"Invalid EXTRACT message format: {message}");
                return;
            }

            int resourceID = int.Parse(parts[1]);
            int amount = int.Parse(parts[2]);
            GameObject resource = FindObjectByServerID(resourceID);
            AmountResource amountResource = resource?.GetComponent<AmountResource>();
            if (amountResource != null)
            {
                amountResource.Extraction(amount);
                Debug.Log($"Extracted {amount} units from resource ID {resourceID}.");
            }
            else
            {
                Debug.LogError($"Resource with ID {resourceID} not found.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing EXTRACT message: {ex.Message}");
        }
    }

    private void ProcessDieMessage(string message)
    {
        try
        {
            string[] parts = message.Split(' ');
            if (parts.Length < 2)
            {
                Debug.LogError($"Invalid DIE message format: {message}");
                return;
            }

            int objectID = int.Parse(parts[1]);
            GameObject obj = FindObjectByServerID(objectID);
            if (obj != null)
            {
                Destroy(obj);
                Debug.Log($"Object with ID {objectID} was destroyed.");
            }
            else
            {
                Debug.LogError($"Object with ID {objectID} not found.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing DIE message: {ex.Message}");
        }
    }

    private GameObject FindObjectByServerID(int objectID)
    {
        foreach (var obj in FindObjectsOfType<GameObject>())
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
            Debug.LogWarning($"Invalid ATTACK message format: {message}");
            return;
        }
        int attackerId = int.Parse(parts[1]);
        int targetId = int.Parse(parts[2]);
        int damage = int.Parse(parts[3]);

        Vector3 postionObject = new Vector3(
                   float.Parse(parts[4], CultureInfo.InvariantCulture),
                   float.Parse(parts[5], CultureInfo.InvariantCulture),
                   float.Parse(parts[6], CultureInfo.InvariantCulture)
               );
        Vector3 rotateObject = new Vector3(
            float.Parse(parts[7], CultureInfo.InvariantCulture),
            float.Parse(parts[8], CultureInfo.InvariantCulture),
            float.Parse(parts[9], CultureInfo.InvariantCulture)
        );


        GameObject attacker = FindObjectByServerID(attackerId);
        WarriorParametrs attackerObj = attacker?.GetComponent<WarriorParametrs>();
        GameObject target = FindObjectByServerID(targetId);
        if (attackerObj != null && target != null)
        {
            if (attacker == null)
            {
                Debug.LogWarning("Unit is null in MoveObject!");
                return;
            }

            NavMeshAgent agent = attacker.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogError("No NavMeshAgent found on unit!");
                return;
            }

            // Встановити стартову позицію (тільки якщо потрібно «телепортувати»)
            agent.Warp(postionObject); // Warp краще за transform.position для агента

            // Встановити обертання
            attacker.transform.rotation = Quaternion.Euler(rotateObject);
            attackerObj.AttackEnemy(target);
        }
    }

    private void ProcessBuiltMessage(string message)
    {
        string[] parts = message.Split(' ');
        if (parts.Length == 8 && parts[0] == "BUILT" && parts[1] == "Bridge3")
        {
            GameObject bridgePlacer = GameObject.Find("BridgePlacer");
            BridgePlacer bp = bridgePlacer?.GetComponent<BridgePlacer>();
            string prefabName = parts[1];
            string buildXStr = parts[2].Replace(',', '.');
            string buildYStr = parts[3].Replace(',', '.');
            string buildZStr = parts[4].Replace(',', '.');
            string buildXEnd = parts[5].Replace(',', '.');
            string buildYEnd = parts[6].Replace(',', '.');
            string buildZEnd = parts[7].Replace(',', '.');

            if (bp != null)
            {
                bp.messageFromServer = true;
                bp.firstPoint = new Vector3(float.Parse(buildXStr, CultureInfo.InvariantCulture), float.Parse(buildYStr, CultureInfo.InvariantCulture), float.Parse(buildZStr, CultureInfo.InvariantCulture));
                bp.secondPoint = new Vector3(float.Parse(buildXEnd, CultureInfo.InvariantCulture), float.Parse(buildYEnd, CultureInfo.InvariantCulture), float.Parse(buildZEnd, CultureInfo.InvariantCulture));
                bp.PlaceBridge();
                bp.messageFromServer = false;
                bp.firstPoint = Vector3.zero;
                bp.secondPoint = Vector3.zero;
            }
        }
        else if (parts.Length == 9 && parts[0] == "BUILT")
        {
            string prefabName = parts[1];
            string prefabId = parts[2];
            string buildXStr = parts[3].Replace(',', '.');
            string buildYStr = parts[4].Replace(',', '.');
            string buildZStr = parts[5].Replace(',', '.');
            string rotXStr = parts[6].Replace(',', '.');
            string rotYStr = parts[7].Replace(',', '.');
            string rotZStr = parts[8].Replace(',', '.');

            LoadAndInstantiateBuilding(prefabName, prefabId, buildXStr, buildYStr, buildZStr, rotXStr, rotYStr, rotZStr);
        }
        else
        {
            Debug.LogError($"Invalid BUILT message format: {message}");
        }
    }

    private void LoadAndInstantiateBuilding(string prefabName, string idStr, string buildXStr, string buildYStr, string buildZStr, string rotXStr, string rotYStr, string rotZStr)
    {
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
                newBuilding.tag = tagOwner[1 - IDclient];
                ServerId serverId = newBuilding.GetComponent<ServerId>();
                serverId.serverId = id;
                TowerAttack ta = newBuilding.GetComponent<TowerAttack>();
                if (ta != null)
                    ta.enabled = true;
                GameObject obstracle = newBuilding.transform.GetChild(0).gameObject;
                obstracle.SetActive(true);
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

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        ws?.DispatchMessageQueue();
#endif
    }
}