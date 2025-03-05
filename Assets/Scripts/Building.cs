using UnityEditor;
using UnityEngine;
using System.Threading.Tasks; // Додайте цей рядок, щоб використовувати Task

public class Building : MonoBehaviour
{
    public LayerMask layer;
    public float maxSlopeAngle = 15f;
    public Terrain terrain;
    public float flattenScale = 0.9f;
    public int smoothIterations = 2;
    public float maxHeightDifference = 0.1f; // Максимальний поріг для вирівнювання (0.1 = 10% від висоти)
    public float rotationSpeed = 90f; // Швидкість обертання (градусів за секунду)

    private bool isPlaced = false;
    private TerrainData terrainData;
    private Renderer[] renderers; // Масив Renderer для зміни кольорів
    private Color validColor = Color.green;
    private Color invalidColor = Color.red;
    private Color[] originalColors; // Початкові кольори для відновлення

    void Start()
    {
        if (terrain != null)
        {
            terrainData = terrain.terrainData;
        }
        else
        {
            Debug.LogError("Terrain not assigned!");
        }

        renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogError("No renderers found on the building object.");
            return;
        }

        // Збереження початкових кольорів кожного матеріалу
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    void Update()
    {
        if (!isPlaced)
        {
            PositionObject();

            if (Input.GetKey(KeyCode.R))
            {
                RotateObject();
            }

            // Оновлення кольору для індикатора валідності
            UpdatePositionIndicator();

            if (Input.GetMouseButtonDown(0) && IsValidPosition())
            {             
                PlaceBuilding();
            }

            if (Input.GetMouseButtonDown(1))
            {
                Destroy(gameObject); // Видалення об'єкта правою кнопкою
            }
        }
    }

    private async void PlaceBuilding()
    {
        try
        {
            await PlaceBuildingAsync();
            Destroy(this); // Знищення скрипта після успішного розміщення будівлі та відправки повідомлення
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Помилка при розміщенні будівлі: {e.Message}");
            // Тут можна додати додаткову логіку обробки помилок, наприклад, скасувати розміщення.
        }
    }

    private async Task PlaceBuildingAsync()
    {
        Transform child = transform.GetChild(0);
        isPlaced = true;
        RestoreOriginalColors();
        child.gameObject.SetActive(true);
        SpawnUnits sp = this.GetComponent<SpawnUnits>();
        sp.enabled = true;
        string prefabName = gameObject.name.Replace("(Clone)", "").Trim();
        float rotationX = gameObject.transform.rotation.x;
        float rotationY = gameObject.transform.rotation.y;
        float rotationZ = gameObject.transform.rotation.z;
        string message = $"BUILT {prefabName} {gameObject.transform.position.x} {gameObject.transform.position.y} {gameObject.transform.position.z} {rotationX} {rotationY} {rotationZ}\n";

        UnityTcpClient tcp = FindAnyObjectByType<UnityTcpClient>();
        if (tcp != null)
        {
              Debug.Log(message);
            try
            {
                await tcp.SendMessage(message);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Помилка при відправці TCP повідомлення: {e.Message}");
                // Важливо обробити помилку відправки повідомлення. Можливо, потрібно повторити спробу або повідомити про помилку.
                throw; // Перекидаємо виняток, щоб він був оброблений в PlaceBuilding()
            }
            return;
        }
        else
        {
            Debug.LogError("UnityTcpClient not found in the scene!");
            // Можливо, тут варто передбачити якусь альтернативну дію, якщо TCP клієнт не знайдено.
            return;
        }
    }
    private void RotateObject()
    {
        float rotationStep = rotationSpeed * Time.deltaTime; // Кут обертання за кадр
        transform.Rotate(0f, rotationStep, 0f, Space.World); // Обертання навколо осі Y
    }

    private void PositionObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 3000f, layer))
        {
            Vector3 correctedPosition = hit.point;
            correctedPosition.y += 3f; // Додавання до координати Y для корекції
            transform.position = correctedPosition;
        }
    }

    private bool IsValidPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 3000f, layer))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            return slopeAngle <= maxSlopeAngle;
        }
        return false;
    }

    private void UpdatePositionIndicator()
    {
        foreach (Renderer renderer in renderers)
        {
            if (IsValidPosition())
            {
                renderer.material.color = validColor; // Зелений колір
            }
            else
            {
                renderer.material.color = invalidColor; // Червоний колір
            }
        }
    }

    private void RestoreOriginalColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i]; // Повернення початкових кольорів
        }
    }

   /* private void AdjustTerrainUnderBuilding()
    {
        if (terrainData == null) return;

        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError("Collider is missing on the building.");
            return;
        }

        Vector3 buildingCenter = collider.bounds.center - terrain.transform.position;
        int terrainX = Mathf.RoundToInt((buildingCenter.x / terrainData.size.x) * terrainData.heightmapResolution);
        int terrainZ = Mathf.RoundToInt((buildingCenter.z / terrainData.size.z) * terrainData.heightmapResolution);

        int radiusX = Mathf.CeilToInt((collider.bounds.size.x * flattenScale / terrainData.size.x) * terrainData.heightmapResolution / 2);
        int radiusZ = Mathf.CeilToInt((collider.bounds.size.z * flattenScale / terrainData.size.z) * terrainData.heightmapResolution / 2);

        int startX = Mathf.Clamp(terrainX - radiusX, 0, terrainData.heightmapResolution - 1);
        int startZ = Mathf.Clamp(terrainZ - radiusZ, 0, terrainData.heightmapResolution - 1);
        int endX = Mathf.Clamp(terrainX + radiusX, 0, terrainData.heightmapResolution - 1);
        int endZ = Mathf.Clamp(terrainZ + radiusZ, 0, terrainData.heightmapResolution - 1);

        float[,] heights = terrainData.GetHeights(startX, startZ, endX - startX, endZ - startZ);

        float averageHeight = 0f;
        foreach (float height in heights)
        {
            averageHeight += height;
        }
        averageHeight /= heights.Length;

        for (int x = 0; x < heights.GetLength(0); x++)
        {
            for (int z = 0; z < heights.GetLength(1); z++)
            {
                if (Mathf.Abs(heights[x, z] - averageHeight) <= maxHeightDifference)
                {
                    heights[x, z] = averageHeight;
                }
            }
        }

        terrainData.SetHeights(startX, startZ, heights);

        SmoothTerrain(startX, startZ, endX - startX, endZ - startZ);
    }

    private void SmoothTerrain(int startX, int startZ, int width, int height)
    {
        for (int i = 0; i < smoothIterations; i++)
        {
            float[,] heights = terrainData.GetHeights(startX, startZ, width, height);
            float[,] smoothed = (float[,])heights.Clone();

            for (int x = 1; x < heights.GetLength(0) - 1; x++)
            {
                for (int z = 1; z < heights.GetLength(1) - 1; z++)
                {
                    smoothed[x, z] = (
                        heights[x - 1, z] + heights[x + 1, z] +
                        heights[x, z - 1] + heights[x, z + 1] +
                        heights[x, z]
                    ) / 5f;
                }
            }

            terrainData.SetHeights(startX, startZ, smoothed);
        }
    }*/
}
