using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using System; // Додайте цей рядок, щоб використовувати Task

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
                TowerAttack ta = gameObject.GetComponent<TowerAttack>();
                if (ta != null)
                {
                    ta.enabled = true;
                }
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
        SpawnUnits sp = gameObject.GetComponent<SpawnUnits>();
        if (sp != null)
            sp.enabled = true;
        string prefabName = gameObject.name.Replace("(Clone)", "").Trim();
        float rotationX = gameObject.transform.rotation.x;
        float rotationY = gameObject.transform.rotation.y;
        float rotationZ = gameObject.transform.rotation.z;


        if (UnityTcpClient.Instance != null)
        {
            try
            {
                string requestIdMessage = "ID_GENERATED";

                // Відправляємо запит на ID і чекаємо відповіді
                bool sendRequestResult = await SpawnUnitOnServer(requestIdMessage);

                if (!sendRequestResult)
                {
                    Debug.LogError("Failed to notify server, Unit will NOT be spawned.");
                    return;
                }
                bool idReceived = await WaitForIDAsync();

                ServerId sr = gameObject.GetComponent<ServerId>();
                sr.serverId = UnityTcpClient.Instance.idUnitGeneratedAtServer;
                UnityTcpClient.Instance.idUnitGeneratedAtServer = 0;

                string message = $"BUILT {prefabName} {sr.serverId} {gameObject.transform.position.x} {gameObject.transform.position.y} {gameObject.transform.position.z} {rotationX} {rotationY} {rotationZ}\n";
                gameObject.transform.GetChild(0).gameObject.SetActive(true);

                await UnityTcpClient.Instance.SendMessage(message);

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

    private async Task<bool> SpawnUnitOnServer(string information)
    {
        if (UnityTcpClient.Instance != null)
        {
            try
            {
                await UnityTcpClient.Instance.SendMessage(information);
                return true; // Indicate success

            }
            catch (Exception e)
            {
                Debug.LogError("Error sending spawn command: " + e.Message);
                return false; // Indicate failure
            }
        }
        else
        {
            Debug.LogError("TCPClient is null.  Make sure it's assigned.");
            return false;
        }
    }
    private async Task<bool> WaitForIDAsync()
    {
        float timeout = 5f; // Таймаут 5 секунд
        float startTime = Time.time;

        while (UnityTcpClient.Instance.idUnitGeneratedAtServer == 0)
        {
            if (Time.time - startTime > timeout)
            {
                Debug.LogError("Timeout waiting for ID");
                return false;
            }

            await Task.Yield(); // Аналог yield return null для async/await
        }

        return true;
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
            Color targetColor = IsValidPosition() ? validColor : invalidColor;

            // Пройтися по всіх матеріалах рендера
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].color = targetColor;
            }
        }
    }

    private void RestoreOriginalColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;

            // Якщо початкові кольори лише одного матеріалу, застосовуй для всіх
            for (int j = 0; j < materials.Length; j++)
            {
                materials[j].color = originalColors[i];
            }
        }
    }
}
