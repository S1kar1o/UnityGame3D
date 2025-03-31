using UnityEngine;
using System.Threading.Tasks;

public class BuildingWalls : MonoBehaviour
{
    public LayerMask layer;
    public float maxSlopeAngle = 15f;
    public Terrain terrain;
    public float flattenScale = 0.9f;
    public float rotationSpeed = 90f;
    public float snapDistance = 25f; // Відстань для кріплення до вежі 
    public  GameObject wallsPrefab;
    private bool isPlaced = false;
    private TerrainData terrainData;
    private Renderer[] renderers;
    private Color validColor = Color.green;
    private Color invalidColor = Color.red;
    private Color[] originalColors;

    private bool isSingleWall = true;
    private Vector3 starPosition;
    private Vector3 endPosition;

    void Start()
    {
        layer = 1 << LayerMask.NameToLayer("Ground"); // Створюємо маску для шару "Ground"
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

        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    void Update()
    {
        {
            if (isSingleWall)
            {
                PositionObject();
            }

            if (isSingleWall)
            {
                if (Input.GetKey(KeyCode.R))
                {
                    RotateObject();
                }
            }
            UpdatePositionIndicator();

            if (Input.GetMouseButtonDown(0))
            {

                if (isSingleWall)
                {
                    PlaceBuilding();
                    isSingleWall=false;
                }
                else
                {
                    Debug.Log(101);
                    BuildingManyWalls();
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                Destroy(gameObject);
            }
        }
    }

    private async void PlaceBuilding()
    {
        try
        {
            await PlaceBuildingAsync();/*
            Destroy(this);*/
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Помилка при розміщенні будівлі: {e.Message}");
        }
    }
    private void BuildingManyWalls() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 3000f, layer))
        {
            endPosition = hit.point;

            // Знаходимо найближчу вежу
            GameObject nearestTower = FindNearestTower(hit.point);
            if (nearestTower != null)
            {
                Vector3 towerCenter = nearestTower.transform.position;
                Vector3 direct = (hit.point - towerCenter).normalized; // Напрямок від вежі до курсору
                endPosition = towerCenter + direct * snapDistance; // Позиція на відстані 5 одиниць
            }


            transform.position = endPosition;
        }

        Renderer prefabRenderer = wallsPrefab.GetComponentInChildren<Renderer>();
        float width;

        if (prefabRenderer != null)
        {
            width = prefabRenderer.bounds.size.z; // Припускаємо, що стіна орієнтована по осі Z
        }
        else
        {
            Debug.LogError("No Renderer found in walls prefab or its children!");
            width = 25f; // Значення за замовчуванням
        }
        float distance1 = Vector3.Distance(starPosition, endPosition);
        int amount =(int)(distance1 / width);


        // Створюємо нові стіни в ряд
        Vector3 direction = (endPosition - starPosition).normalized;
        for (int i = 0; i < amount; i++)
        {
            Vector3 position = starPosition + direction * (width * i);
            GameObject newWall = Instantiate(wallsPrefab, position, Quaternion.identity, transform.parent);
            newWall.transform.LookAt(endPosition); // Орієнтуємо стіну у напрямку рядка

           /* // Якщо потрібно зберігати оригінальний об'єкт незмінним
            if (i == amount - 1)
            {
                transform.position = position + direction * width;
            }*/
        }
        Destroy(gameObject);

    }
    private async Task PlaceBuildingAsync()
    {
        if (transform.childCount == 0)
        {
            Debug.LogError("Помилка при розміщенні будівлі: немає дочірніх об’єктів!");
            return;
        }

        Transform child = transform.GetChild(0);
        isPlaced = true;
        RestoreOriginalColors();
        child.gameObject.SetActive(true);

        SpawnUnits sp = GetComponent<SpawnUnits>();
        if (sp != null)
        {
            sp.enabled = true;
        }

        string prefabName = gameObject.name.Replace("(Clone)", "").Trim();
        float rotationX = transform.rotation.x;
        float rotationY = transform.rotation.y;
        float rotationZ = transform.rotation.z;
        string message = $"BUILT {prefabName} {transform.position.x} {transform.position.y} {transform.position.z} {rotationX} {rotationY} {rotationZ}\n";

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
                throw;
            }
        }
        else
        {
            Debug.LogError("UnityTcpClient not found in the scene!");
        }
    }

    private void RotateObject()
    {
        float rotationStep = rotationSpeed * Time.deltaTime;
        transform.Rotate(0f, rotationStep, 0f, Space.World);
    }

    private void PositionObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 3000f, layer))
        {
           starPosition = hit.point;

            // Знаходимо найближчу вежу
            GameObject nearestTower = FindNearestTower(hit.point);
            if (nearestTower != null)
            {
                Vector3 towerCenter = nearestTower.transform.position;
                Vector3 direction = (hit.point - towerCenter).normalized; // Напрямок від вежі до курсору
                starPosition = towerCenter + direction * snapDistance; // Позиція на відстані 5 одиниць
            }


            transform.position = starPosition;
        }
    }

    private GameObject FindNearestTower(Vector3 position)
    {
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower"); // Вежі повинні мати тег "Tower"
        GameObject nearestTower = null;
        float minDistance = 40;

        foreach (GameObject tower in towers)
        {
            float distance = Vector3.Distance(position, tower.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestTower = tower;
            }
        }

        return nearestTower;
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
            renderer.material.color = IsValidPosition() ? validColor : invalidColor;
        }
    }

    private void RestoreOriginalColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i];
        }
    }
}