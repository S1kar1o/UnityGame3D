using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static System.Net.WebRequestMethods;
using System;
using System.Threading.Tasks;

public class MovingObjects : MonoBehaviour
{
    private Vector2 startScreenPosition; // Початкова позиція рамки вибору
    private Vector2 endScreenPosition;   // Поточна позиція миші
    private bool isDragging = false;     // Чи зараз малюється рамка

    private Camera mainCamera;           // Головна камера
    public LayerMask groundLayer;        // Шар для землі (xz-площина)
    public LayerMask selectionLayer;     // Шар для об'єктів вибору
    public LayerMask riverLayer;
    public UnityTcpClient utp;

    public GameObject hirePanel;
    public Button hireButton;
    private ButtonControler buttonControler;
    public GameObject spawnBuilding;
    public List<GameObject> selectedUnits = new List<GameObject>(); // Список вибраних юнітів
    private float holdTime=0.0f; // Час утримання кнопки
    public float holdThreshold = 0.3f; // Час (у секундах), після якого вважаємо, що кнопку утримують

    void Start()
    {
        GameObject obj = GameObject.Find("UnityTcpClient");
        if (obj != null)
        {
            utp = obj.GetComponent<UnityTcpClient>();
        }
        else
        {
            Debug.LogError("UnityTcpClient not found in the scene!");
        }
        mainCamera = Camera.main;
        GameObject eventSestem = GameObject.Find("EventSystem");
        buttonControler = eventSestem.GetComponent<ButtonControler>();
        GameObject canvasObject = GameObject.Find("Canvas");
        Canvas canvas = canvasObject.GetComponent<Canvas>();

        // Знайти панель в Canvas
        if (canvas != null)
        {
            Transform panelTransform = canvas.transform.Find("RecruitPanel");

            if (panelTransform != null)
            {
                hirePanel = panelTransform.gameObject;

            }
        }
        hireButton = hirePanel.transform.Find("Recruit").GetComponent<Button>();
        if (hirePanel != null)
        {
            hirePanel.SetActive(false); // Приховуємо панель на старті
            hireButton.onClick.AddListener(OnConnectedToServer); // Викликаємо коректний метод

        }
    }
    public void OnConnectedToServer()
    {
        SpawnUnits sp = spawnBuilding.GetComponent<SpawnUnits>();
        sp.SpawnUnit();
    }
    public void ActivatePanelRecruiting()
    {
        hirePanel.SetActive(!buttonControler.hirePanel);
        buttonControler.hirePanel = !buttonControler.hirePanel;

    }
    void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        if (holdTime != 0.0f)
        {
            if (Time.time - holdTime > holdThreshold)
            {
                Debug.Log(Time.time - holdTime);
                isDragging = true;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit, 3000f))
                {
                    if (hit.collider.CompareTag("Resource")) // Якщо натиснуто на ресурс
                    {
                        GameObject resource = hit.collider.gameObject;

                        foreach (var obj in selectedUnits)
                        {
                            var extractor = obj.GetComponent<VillagerParametrs>();
                            if (extractor != null)
                            {
                                extractor.IsRunningToResource(true);
                                extractor.MoveToResource(resource); // Рух до ресурсу
                            }
                        }
                    }
                    else if (((1 << hit.collider.gameObject.layer) & groundLayer) != 0) // Якщо натиснуто на землю
                    {
                        foreach (var obj in selectedUnits)
                        {
                            var extractor = obj.GetComponent<VillagerParametrs>();
                            if (extractor != null)
                            {
                                // Спроба привести до WarriorParametrs, якщо це можливо
                                if (extractor is WarriorParametrs warrior)
                                {
                                    warrior.targetEnemy = null;
                                }
                                else
                                {
                                    extractor.StopExtracting(); //  Викликає батьківський метод
                                }

                                extractor.targetPosition = hit.point; // Якщо targetPosition є в батьківському класі
                            }
                            obj.GetComponent<NavMeshAgent>().SetDestination(hit.point);
                            SendMoveMessage(obj, hit.point);

                        }
                    }
                    else if (hit.collider.CompareTag("Enemy"))
                    {
                        GameObject enemy = hit.collider.gameObject;
                        if (enemy != null)
                        {
                            foreach (var obj in selectedUnits)
                            {
                                var warrior = obj.GetComponent<WarriorParametrs>();
                                if (warrior != null)
                                {
                                    warrior.AttackEnemy(enemy);
                                    SendAttackMessage(obj,enemy, warrior.GetDamage());
                                }
                            }
                        }
                    }
                }
            }
        }
        if (Input.GetMouseButtonDown(0))
        {
            holdTime = Time.time; // Запам'ятовуємо час натискання

            if (!ColisionBuildings())
            {
                startScreenPosition = Input.mousePosition;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            endScreenPosition = Input.mousePosition;
            if (isDragging)
            {
                SelectUnits();
                isDragging = false;

            }
            holdTime = 0.0f;
        }

        if (isDragging)
        {
            endScreenPosition = Input.mousePosition;
        }
    }
    public async Task SendAttackMessage(GameObject unit, GameObject target, int damage)
    {
        if (utp == null)
        {
            Debug.LogError("UnityTcpClient не ініціалізований!");
            return;
        }

        int unitId = unit.GetInstanceID();
        int targetId = target.GetInstanceID();
        string message = $"ATTACK {unitId} {targetId} {damage}";
        Debug.Log($"Надсилаю повідомлення про атаку: {message}");

        try
        {
            await utp.SendMessage(message);
            Debug.Log($"Повідомлення про атаку для юніта {unitId} успішно відправлено.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Помилка при відправці повідомлення про атаку: {e.Message}");
        }
    }
    public async Task SendMoveMessage(GameObject unit, Vector3 destination)
    {
        if (utp == null)
        {
            Debug.LogError("UnityTcpClient не ініціалізований!");
            return;
        }

        // Створення повідомлення про рух
        int unitId = unit.GetInstanceID();
        string message = $"MOVE {unitId} {destination.x} {destination.y} {destination.z}";
        Debug.Log($"Надсилаю повідомлення про рух: {message}");

        try
        {
            // Асинхронна відправка повідомлення на сервер
            await utp.SendMessage(message);
            Debug.Log($"Повідомлення про рух успішно відправлено: unitId={unitId}, destination={destination}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Помилка при відправці повідомлення про рух: {e.Message}");
        }
    }
    bool ColisionBuildings()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 3000f))
        {
            if (hit.collider.CompareTag("Building"))
            {
                GameObject building = hit.collider.gameObject;
                spawnBuilding = building;
                SpawnUnits buildingStatement=building.GetComponent<SpawnUnits>();
                if (building.GetComponent<Building>() == null)
                {
                    ActivatePanelRecruiting();
                }
                return true;

            }

        }
        return false;
    }
    void OnGUI()
    {
        if (isDragging)
        {
            Rect rect = GetScreenRect(startScreenPosition, endScreenPosition);
            DrawSelectionRect(rect);
        }
    }

    void SelectUnits()
    {
        // Перетворення екранних координат у глобальні світові
        Vector3 worldStart = ScreenToWorldXZ(startScreenPosition);
        Vector3 worldEnd = ScreenToWorldXZ(endScreenPosition);

        Debug.Log($"Початок вибору у світі (xz): {worldStart}");
        Debug.Log($"Кінець вибору у світі (xz): {worldEnd}");

        // Знаходження меж xz
        float minX = Mathf.Min(worldStart.x, worldEnd.x);
        float maxX = Mathf.Max(worldStart.x, worldEnd.x);
        float minZ = Mathf.Min(worldStart.z, worldEnd.z);
        float maxZ = Mathf.Max(worldStart.z, worldEnd.z);

        // Очищення попереднього вибору
        selectedUnits.Clear();
        foreach (GameObject unit in GameObject.FindGameObjectsWithTag("Unit"))
        {
            Vector3 unitPosition = unit.transform.position;
            // Перевірка, чи потрапляє юніт у рамку
            if (unitPosition.x >= minX && unitPosition.x <= maxX && unitPosition.z >= minZ && unitPosition.z <= maxZ)
            {
                selectedUnits.Add(unit);
                HighlightUnit(unit, true);
            }
            else
            {
                HighlightUnit(unit, false);
            }
        }

        // Логування вибраних юнітів
        Debug.Log($"Вибрано {selectedUnits.Count} юнітів.");
        foreach (GameObject unit in selectedUnits)
        {
            Debug.Log($"Вибраний юніт: {unit.name}, Позиція у світі (xz): ({unit.transform.position.x}, {unit.transform.position.z})");
        }
    }

    Vector3 ScreenToWorldXZ(Vector2 screenPosition)
    {
        // Створення променя з екранних координат
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            // Перетворення в світові координати; y = 0 для площини xz
            return new Vector3(hit.point.x, 0, hit.point.z);
        }

        // Якщо промінь не перетинає землю, повертаємо 0 (може бути змінено)
        Debug.LogWarning("Промінь не перетинає площину.");
        return Vector3.zero;
    }

    Rect GetScreenRect(Vector2 screenPosition1, Vector2 screenPosition2)
    {
        screenPosition1.y = Screen.height - screenPosition1.y;
        screenPosition2.y = Screen.height - screenPosition2.y;

        Vector2 topLeft = Vector2.Min(screenPosition1, screenPosition2);
        Vector2 bottomRight = Vector2.Max(screenPosition1, screenPosition2);

        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    void DrawSelectionRect(Rect rect)
    {
        GUI.color = new Color(0, 1, 0, 0.25f); // Напівпрозорий зелений
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.green; // Окантовка
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - 2, rect.width, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, 2, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 2, rect.yMin, 2, rect.height), Texture2D.whiteTexture);
    }

    void HighlightUnit(GameObject unit, bool highlight)
    {
        if (unit != null && unit.transform.childCount > 0)
        {
            unit.transform.GetChild(0).gameObject.SetActive(highlight);
        }
        else
        {
            Debug.LogWarning($"Об'єкт {unit?.name ?? "null"} не має дочірнього елемента.");
        }
    }

}
