using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class MovingObjects : MonoBehaviour
{
    private Vector2 startScreenPosition; // Початкова позиція рамки вибору
    private Vector2 endScreenPosition;   // Поточна позиція миші
    private bool isDragging = false;     // Чи зараз малюється рамка

    private Camera mainCamera;           // Головна камера
    public LayerMask groundLayer;        // Шар для землі (xz-площина)
    public LayerMask selectionLayer;     // Шар для об'єктів вибору
    public List<GameObject> selectedUnits = new List<GameObject>(); // Список вибраних юнітів

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        if(Input.GetMouseButtonDown(1)&&selectedUnits.Count>0) {
            Ray ray= mainCamera.ScreenPointToRay( Input.mousePosition );
            if(Physics.Raycast(ray,out RaycastHit agentTarget,3000f,groundLayer) ) {
            foreach(var obj in selectedUnits)
                {
                    obj.GetComponent<NavMeshAgent>().SetDestination(agentTarget.point);
                }
            
            }
        }
        if (Input.GetMouseButtonDown(0))
        {
            startScreenPosition = Input.mousePosition;
            isDragging = true;
            Debug.Log($"Початок вибору (екранні координати): {startScreenPosition}");
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            endScreenPosition = Input.mousePosition;

            Debug.Log($"Кінець вибору (екранні координати): {endScreenPosition}");
            SelectUnits();
        }

        if (isDragging)
        {
            endScreenPosition = Input.mousePosition;
        }
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
