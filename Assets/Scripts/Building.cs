using UnityEngine;

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
    private Renderer buildingRenderer;  // Для зміни кольору/визуального індикатора
    private Color validColor = Color.green;
    private Color invalidColor = Color.red;

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

        buildingRenderer = GetComponent<Renderer>();
        if (buildingRenderer == null)
        {
            Debug.LogError("Renderer not found on the building object.");
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

            // Оновлюємо індикатор на основі валідності позиції
            UpdatePositionIndicator();

            if (Input.GetMouseButtonDown(0) && IsValidPosition())
            {
                AdjustTerrainUnderBuilding();
                isPlaced = true;
                buildingRenderer.material.color = Color.white; // Зміна кольору на білий після розміщення будинку
                Destroy(this); // Знищуємо цей скрипт
            }

            if (Input.GetMouseButtonDown(1))
            {
                Destroy(gameObject);
            }
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
            // Коригуємо розташування, додаючи 3 до координати Y
            Vector3 correctedPosition = hit.point;
            correctedPosition.y += 3f;  // Додаємо 3 до Y для виправлення

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
        if (buildingRenderer != null)
        {
            if (IsValidPosition())
            {
                buildingRenderer.material.color = validColor;  // Зелений колір для валідної позиції
            }
            else
            {
                buildingRenderer.material.color = invalidColor;  // Червоний колір для невалідної позиції
            }
        }
    }

    private void AdjustTerrainUnderBuilding()
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
                // Вирівнювання тільки якщо висота не виходить за межі дозволеного порогу
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
    }
}
