using Unity.AI.Navigation; // Використовуємо простори імен для роботи з навігацією
using UnityEngine; // Основний простір імен Unity
using UnityEngine.AI; // Простір імен для роботи з AI та навігацією

public class WallGenerator : MonoBehaviour // Клас для генерації стін
{
    [Header("Wall Settings")] // Заголовок для налаштувань мосту
    public GameObject wallPrefab; // Префаб стіни
    public float navMeshLinkWidth = 5f; // Ширина посилання навігаційної сітки
    public bool navMeshLinkBidirectional = true; // Чи є посилання двонаправленим
    public float maxSlopeAngle = 15f; // Максимальний кут нахилу
    public Color validPlacementColor = new Color(0, 1, 0, 0.5f); // Колір для валідного розміщення
    public Color invalidPlacementColor = new Color(1, 0, 0, 0.5f); // Колір для невалідного розміщення
    public Color cursorFollowColor = new Color(0, 0.5f, 1f, 0.7f); // Колір для прев'ю під курсором

    [Header("Terrain Connection")] // Заголовок для налаштувань з'єднання з террейном
    public float connectionSearchDistance = 20f; // Дистанція пошуку з'єднання
    public LayerMask groundLayerMask; // Маска шару землі
    public float waterCheckDistance = 10f; // Дистанція перевірки води
    public int waterCheckPoints = 1; // Кількість точок для перевірки води

    private Vector3 firstPoint; // Перша точка розміщення
    private Vector3 secondPoint; // Друга точка розміщення
    private bool isFirstPointSelected = false; // Чи обрана перша точка
    private bool isPlacingWall = false; // Чи йде процес розміщення
    private bool isValidPlacement = true; // Чи валідне розміщення
   
    private GameObject[] WallParts; // Масив частин мосту
    private GameObject[] previewParts; // Масив частин прев'ю
    private GameObject cursorFollowPreview; // Прев'ю, що слідує за курсором

    public float maxAllowedSlopeAngle = 45f;
    public float maxHeightDifference = 0.5f;

    void Update() // Оновлення кожного кадру
    {
        if (!isPlacingWall) // Якщо не в режимі розміщення
        {
            ClearPreview(); // Очистити прев'ю
            return; // Вийти з методу
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // Отримати промінь від камери до курсора
        if (Physics.Raycast(ray, out RaycastHit hit)) // Якщо промінь перетинає об'єкт
        {
            if (!isFirstPointSelected) // Якщо перша точка не обрана
            {
                UpdateCursorFollowPreview(hit.point); // Оновити прев'ю під курсором
            }
            else // Якщо перша точка обрана
            {
                secondPoint = hit.point; // Запам'ятати другу точку
                isValidPlacement = CheckPlacementValidity(firstPoint, secondPoint); // Перевірити валідність розміщення
                
                UpdateWallPreview(); // Оновити прев'ю мосту
            }

            if (Input.GetMouseButtonDown(0)) // При натисканні лівої кнопки миші
            {
                if (IsValidLocation(hit)) // Якщо місце валідне
                {
                    if (!isFirstPointSelected) // Якщо перша точка не обрана
                    {
                        firstPoint = hit.point; // Запам'ятати першу точку
                        isFirstPointSelected = true; // Позначити, що перша точка обрана
                        Destroy(cursorFollowPreview); // Видалити прев'ю під курсором
                        cursorFollowPreview = null; // Очистити посилання
                    }
                    else if (isValidPlacement) // Якщо розміщення валідне і є вода
                    {
                        ClearPreview(); // Очистити прев'ю
                        PlaceWall(); // Розмістити міст
                        isPlacingWall = false; // Вийти з режиму розміщення
                    }
                }
            }
        }

        if (Input.GetMouseButtonDown(1)) // При натисканні правої кнопки миші
        {
            CancelPlacement(); // Скасувати розміщення
        }
    }
    private void UpdateCursorFollowPreview(Vector3 position) // Оновлення прев'ю під курсором
    {
        if (cursorFollowPreview == null) // Якщо прев'ю ще не створене
        {
            cursorFollowPreview = Instantiate(wallPrefab, position, Quaternion.identity); // Створити прев'ю
            cursorFollowPreview.tag = "Untagged"; // Встановити тег

            foreach (var collider in cursorFollowPreview.GetComponentsInChildren<Collider>()) // Для всіх колайдерів
            {
                collider.enabled = false; // Вимкнути колайдери
            }

            foreach (var renderer in cursorFollowPreview.GetComponentsInChildren<Renderer>()) // Для всіх рендерерів
            {
                renderer.material.color = cursorFollowColor; // Встановити колір
            }
        }
        else // Якщо прев'ю вже існує
        {
            cursorFollowPreview.transform.position = position; // Оновити позицію
        }
    }

    private bool IsValidLocation(RaycastHit hit) // Перевірка валідності місця
    {
        return hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") && // Чи це земля
               !hit.collider.CompareTag("Water") && // Чи не вода
               !hit.collider.CompareTag("Wall"); // Чи не міст
    }

    private bool CheckPlacementValidity(Vector3 start, Vector3 end) // Перевірка валідності розміщення
    {
        Vector3 direction = end - start; // Напрямок між точками
        float horizontalDistance = new Vector2(direction.x, direction.z).magnitude; // Горизонтальна дистанція
        float verticalDistance = Mathf.Abs(direction.y); // Вертикальна дистанція
        float angle = Mathf.Atan2(verticalDistance, horizontalDistance) * Mathf.Rad2Deg; // Кут нахилу

        if (angle > maxSlopeAngle) // Якщо кут перевищує максимальний
        {
            Debug.LogWarning($"Wall angle {angle:F1}° exceeds maximum {maxSlopeAngle}°"); // Попередження
            return false; // Повернути false
        }
        return true; // Повернути true
    }

    private void UpdateWallPreview() // Оновлення прев'ю стіни з урахуванням рельєфу
    {
        ClearPreview(); // Очистити попереднє прев'ю

        Vector3 direction = (secondPoint - firstPoint).normalized; // Напрямок стіни
        Quaternion rotation = Quaternion.LookRotation(direction); // Поворот стіни
        float distance = Vector3.Distance(firstPoint, secondPoint); // Дистанція між точками
        float wallLength = wallPrefab.GetComponentInChildren<Renderer>().bounds.size.z; // Довжина сегменту

        int wallCount = Mathf.Max(1, Mathf.FloorToInt(distance / wallLength) + 1); // Кількість сегментів
        float offset = (wallCount * wallLength - distance) / 2; // Відступ для центрування

        Vector3 currentPosition = firstPoint - direction * offset; // Початкова позиція
        previewParts = new GameObject[wallCount]; // Ініціалізація масиву прев'ю

        Color previewColor = isValidPlacement ? validPlacementColor : invalidPlacementColor; // Вибір кольору
        bool allSegmentsValid = true; // Чи всі сегменти мають валідне розміщення

        for (int i = 0; i < wallCount; i++) // Для кожного сегменту
        {
            // Визначаємо позицію на рельєфі
            Vector3 groundPosition = GetPositionOnTerrain(currentPosition);

            // Створюємо прев'ю сегмента
            previewParts[i] = Instantiate(wallPrefab, groundPosition, rotation);
            previewParts[i].tag = "Untagged";

            // Перевіряємо валідність позиції для поточного сегмента
            bool segmentValid = CheckSegmentPlacement(groundPosition, rotation);
            if (!segmentValid) allSegmentsValid = false;

            // Встановлюємо колір (можна зробити індивідуальний для кожного сегмента)
            Color segmentColor = segmentValid ? validPlacementColor : invalidPlacementColor;

            // Налаштування прев'ю
            foreach (var collider in previewParts[i].GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            foreach (var renderer in previewParts[i].GetComponentsInChildren<Renderer>())
            {
                renderer.material.color = segmentColor;
                // Додаємо напівпрозорість для прев'ю
                var material = renderer.material;
                material.color = new Color(segmentColor.r, segmentColor.g, segmentColor.b, 0.7f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = 3000;
            }

            currentPosition += direction * wallLength;
        }

        // Оновлюємо загальний статус валідності
        isValidPlacement = allSegmentsValid;
    }

    private void PlaceWall()
    {
        Vector3 direction = (secondPoint - firstPoint).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);
        float distance = Vector3.Distance(firstPoint, secondPoint);
        float wallLength = wallPrefab.GetComponentInChildren<Renderer>().bounds.size.z;

        int wallCount = Mathf.Max(1, Mathf.FloorToInt(distance / wallLength) + 1);
        float offset = (wallCount * wallLength - distance) / 2;

        Vector3 currentPosition = firstPoint - direction * offset;
        WallParts = new GameObject[wallCount];

        for (int i = 0; i < wallCount; i++)
        {
            // Визначаємо висоту для поточного сегмента
            Vector3 groundPosition = GetPositionOnTerrain(currentPosition);

            WallParts[i] = Instantiate(wallPrefab, groundPosition, rotation);
            WallParts[i].tag = "Wall";

            currentPosition += direction * wallLength;
        }
    }

    private Vector3 GetPositionOnTerrain(Vector3 position)
    {
        Ray ray = new Ray(position + Vector3.up * 100f, Vector3.down); // Пускаємо промінь зверху
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            return hit.point;
        }

        // Якщо промінь не влучив (на всякий випадок)
        return position;
    }
    private bool CheckSegmentPlacement(Vector3 position, Quaternion rotation)
    {
        // 1. Перевірка наявності опори під сегментом (чи не "висить" у повітрі)
        Ray groundCheckRay = new Ray(position + Vector3.up * 0.5f, Vector3.down);
        if (!Physics.Raycast(groundCheckRay, 1f, LayerMask.GetMask("Ground")))
        {
            return false;
        }

        // 2. Перевірка на перетин з іншими об'єктами
        Collider[] wallColliders = wallPrefab.GetComponentsInChildren<Collider>();
        foreach (var collider in wallColliders)
        {
            // Створюємо фізичну копію колайдера для перевірки
            Vector3 colliderCenter = position + rotation * collider.bounds.center;
            if (Physics.CheckBox(colliderCenter,
                               collider.bounds.extents,
                               rotation,
                               LayerMask.GetMask("Default"))) // Шар з перешкодами
            {
                return false;
            }
        }

        // 3. Перевірка кута нахилу поверхні (чи не надто крутий схил)
        Ray slopeCheckRay = new Ray(position + Vector3.up * 0.5f, Vector3.down);
        RaycastHit slopeHit;
        if (Physics.Raycast(slopeCheckRay, out slopeHit, 1f, LayerMask.GetMask("Ground")))
        {
            float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);
            if (slopeAngle > maxAllowedSlopeAngle) // maxAllowedSlopeAngle - заданий ліміт кута
            {
                return false;
            }
        }

        // 4. Перевірка достатнього простору для сегмента
        float requiredSpace = 0.2f; // Додатковий простір навколо
        Bounds wallBounds = wallPrefab.GetComponentInChildren<Renderer>().bounds;
        Vector3 halfExtents = wallBounds.extents + Vector3.one * requiredSpace;
        if (Physics.CheckBox(position, halfExtents, rotation, LayerMask.GetMask("Obstacles")))
        {
            return false;
        }

        // 5. Перевірка висоти (чи не занадто високо/низько)
        float terrainHeight = Terrain.activeTerrain.SampleHeight(position);
        if (Mathf.Abs(position.y - terrainHeight) > maxHeightDifference) // maxHeightDifference - допустима різниця
        {
            return false;
        }

        // 6. Додаткова перевірка на наявність води (якщо потрібно)
        if (CheckWaterPresence(position))
        {
            return false;
        }

        // Всі перевірки пройдено - позиція валідна
        return true;
    }

    // Допоміжний метод для перевірки води
    private bool CheckWaterPresence(Vector3 position)
    {
        // Реалізація залежить від того, як у вашому проекті реалізована вода
        // Наприклад, перевірка по шарах або по висоті
        return false;
    }
    private void ClearPreview() // Очищення прев'ю
    {
        if (previewParts != null) // Якщо є частини прев'ю
        {
            foreach (GameObject part in previewParts) // Для кожної частини
            {
                if (part != null) Destroy(part); // Знищити об'єкт
            }
            previewParts = null; // Очистити масив
        }

        if (cursorFollowPreview != null) // Якщо є прев'ю під курсором
        {
            Destroy(cursorFollowPreview); // Знищити об'єкт
            cursorFollowPreview = null; // Очистити посилання
        }
    }

    private void CancelPlacement() // Скасування розміщення
    {
        isPlacingWall = false; // Вийти з режиму розміщення
        isFirstPointSelected = false; // Скинути вибір точки
        ClearPreview(); // Очистити прев'ю
        Debug.Log("Wall placement canceled"); // Логування
    }

    public void StartPlacingWall() // Початок розміщення мосту
    {
        isPlacingWall = !isPlacingWall; // Перемкнути режим розміщення
        isFirstPointSelected = false; // Скинути вибір точки
        ClearPreview(); // Очистити прев'ю
        Debug.Log("Wall placement mode: " + (isPlacingWall ? "ON" : "OFF")); // Логування
    }
}