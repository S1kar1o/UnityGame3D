using System.Net.Http;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class BridgePlacer : MonoBehaviour
{
    [Header("Bridge Settings")]
    public GameObject bridgePrefab;
    public float navMeshLinkWidth = 5f;
    public bool navMeshLinkBidirectional = true;
    public float maxSlopeAngle = 15f;
    public Color validPlacementColor = new Color(0, 1, 0, 0.5f);
    public Color invalidPlacementColor = new Color(1, 0, 0, 0.5f);
    public Color cursorFollowColor = new Color(0, 0.5f, 1f, 0.7f);

    [Header("Terrain Connection")]
    public float connectionSearchDistance = 20f;
    public LayerMask groundLayerMask;
    public float waterCheckDistance = 10f;
    public int waterCheckPoints = 1; // Кількість точок для перевірки води

    public Vector3 firstPoint;
    public Vector3 secondPoint;
    private bool isFirstPointSelected = false;
    private bool isPlacingBridge = false;
    private bool isValidPlacement = true;
    private bool hasWaterUnderBridge = false;
    private GameObject[] bridgeParts;
    private GameObject[] previewParts;
    private GameObject cursorFollowPreview;

    private int costOfTreeBrige, costOfRockBrige, costOfGoldBrige;
    private int brigeCount = 0;
    private bool priceValid = false;

    public bool messageFromServer = false;
    void Update()
    {
        if (!isPlacingBridge)
        {
            ClearPreview();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {

            if (!isFirstPointSelected)
            {
                UpdateCursorFollowPreview(hit.point);
            }
            else
            {
                secondPoint = hit.point;
                isValidPlacement = CheckPlacementValidity(firstPoint, secondPoint);
                hasWaterUnderBridge = HasWaterUnderBridge();
                UpdateBridgePreview();
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (IsValidLocation(hit))
                {
                    priceValid =
           (UnityTcpClient.Instance.goldAmount < brigeCount * costOfGoldBrige) ||
           (UnityTcpClient.Instance.rockAmount < brigeCount * costOfRockBrige) ||
           (UnityTcpClient.Instance.woodAmount < brigeCount * costOfTreeBrige);


                    if (!isFirstPointSelected)
                    {
                        firstPoint = hit.point;
                        isFirstPointSelected = true;
                        Destroy(cursorFollowPreview);
                        cursorFollowPreview = null;
                    }
                    else if (isValidPlacement && hasWaterUnderBridge && !priceValid)
                    {
                        ClearPreview();
                        PlaceBridge();
                        isPlacingBridge = false;
                    }
                }
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }

    private bool HasWaterUnderBridge()
    {
        if (!isFirstPointSelected) return false;

        // Визначаємо центральну точку мосту
        Vector3 centerPoint = (firstPoint + secondPoint) / 2f;

        // Виконуємо Raycast тільки вниз від центральної точки
        if (Physics.Raycast(centerPoint + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 100f))
        {
            if (hit.collider.CompareTag("Water"))
            {
                Debug.DrawLine(centerPoint, hit.point, Color.cyan, 200f);
                return true;
            }
            else
            {
                Debug.DrawLine(centerPoint, hit.point, Color.yellow, 2f);
                return false;
            }
        }

        Debug.DrawRay(centerPoint + Vector3.up * 2f, Vector3.down * waterCheckDistance, Color.red, 2f);
        return false;
    }

    private void UpdateCursorFollowPreview(Vector3 position)
    {
        if (cursorFollowPreview == null)
        {
            cursorFollowPreview = Instantiate(bridgePrefab, position, Quaternion.identity);
            cursorFollowPreview.tag = "Untagged";

            foreach (var collider in cursorFollowPreview.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            foreach (var renderer in cursorFollowPreview.GetComponentsInChildren<Renderer>())
            {
                renderer.material.color = cursorFollowColor;
            }
        }
        else
        {
            cursorFollowPreview.transform.position = position;
        }
    }

    private bool IsValidLocation(RaycastHit hit)
    {
        return hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") &&
               !hit.collider.CompareTag("Water") &&
               !hit.collider.CompareTag("Bridge");
    }

    private bool CheckPlacementValidity(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float horizontalDistance = new Vector2(direction.x, direction.z).magnitude;
        float verticalDistance = Mathf.Abs(direction.y);
        float angle = Mathf.Atan2(verticalDistance, horizontalDistance) * Mathf.Rad2Deg;

        if (angle > maxSlopeAngle)
        {
            Debug.LogWarning($"Bridge angle {angle:F1}° exceeds maximum {maxSlopeAngle}°");
            return false;
        }
        return true;
    }

    private void UpdateBridgePreview()
    {
        ClearPreview();

        Vector3 direction = (secondPoint - firstPoint).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);
        float distance = Vector3.Distance(firstPoint, secondPoint);
        float bridgeLength = bridgePrefab.GetComponentInChildren<Renderer>().bounds.size.z;

        int bridgeCount = Mathf.Max(1, Mathf.FloorToInt(distance / bridgeLength) + 1);
        this.brigeCount = bridgeCount;
        float offset = (bridgeCount * bridgeLength - distance) / 2;

        Vector3 position = firstPoint - direction * offset;
        previewParts = new GameObject[bridgeCount];

        Color previewColor = (isValidPlacement && hasWaterUnderBridge) ? validPlacementColor : invalidPlacementColor;
        bool priceValid =
           (UnityTcpClient.Instance.goldAmount < brigeCount * costOfGoldBrige) ||
           (UnityTcpClient.Instance.rockAmount < brigeCount * costOfRockBrige) ||
           (UnityTcpClient.Instance.woodAmount < brigeCount * costOfTreeBrige);

        if (priceValid) previewColor = invalidPlacementColor;

        for (int i = 0; i < bridgeCount; i++)
        {
            previewParts[i] = Instantiate(bridgePrefab, position, rotation);
            previewParts[i].tag = "Untagged";

            foreach (var collider in previewParts[i].GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            foreach (var renderer in previewParts[i].GetComponentsInChildren<Renderer>())
            {
                renderer.material.color = previewColor;
            }

            position += direction * bridgeLength;
        }
    }

    public async void PlaceBridge()
    {
        Vector3 direction = (secondPoint - firstPoint).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);
        float distance = Vector3.Distance(firstPoint, secondPoint);
        float bridgeLength = bridgePrefab.GetComponentInChildren<Renderer>().bounds.size.z;

        int bridgeCount = Mathf.Max(1, Mathf.FloorToInt(distance / bridgeLength) + 1);
        float offset = (bridgeCount * bridgeLength - distance) / 2;

        Vector3 position = firstPoint - direction * offset;
        bridgeParts = new GameObject[bridgeCount];
        /*
                string requestIdMessage = "ID_GENERATED";

                // Відправляємо запит на ID і чекаємо відповіді
                bool sendRequestResult = await SpawnObjectOnServer(requestIdMessage);

                if (!sendRequestResult)
                {
                    Debug.LogError("Failed to notify server, Unit will NOT be spawned.");
                    return;
                }

                // Чекаємо отримання ID від сервера
                bool idReceived = await WaitForIDAsync();

                if (!idReceived)
                {
                    Debug.LogError("Failed to receive ID from server.");
                    return;
                }*/
        if (!messageFromServer)
        {
            string name = bridgePrefab.name.Replace("(Clone)", "").Trim();

            string spawnMessage = $"BUILT {name} {firstPoint.x:F2} {firstPoint.y:F2} {firstPoint.z:F2} {secondPoint.x:F2} {secondPoint.y:F2} {secondPoint.z:F2}\n";
            UnityTcpClient.Instance.SendMessage(spawnMessage);
        }
        for (int i = 0; i < bridgeCount; i++)
        {
            bridgeParts[i] = Instantiate(bridgePrefab, position, rotation);
            bridgeParts[i].tag = "Bridge";
            position += direction * bridgeLength;
        }

        CreateNavMeshLinks();
        UnityTcpClient.Instance.goldAmount = UnityTcpClient.Instance.goldAmount - costOfGoldBrige * brigeCount;
        UnityTcpClient.Instance.woodAmount = UnityTcpClient.Instance.woodAmount - costOfTreeBrige * brigeCount;
        UnityTcpClient.Instance.rockAmount = UnityTcpClient.Instance.rockAmount - costOfRockBrige * brigeCount;
        UnityTcpClient.Instance.uIresource.UpdateAmoundOfResource();
    }
    private void CreateNavMeshLinks()
    {

        for (int i = 0; i < bridgeParts.Length; i++)
        {
            Debug.Log(120);
            GameObject current = bridgeParts[i];
            if (bridgeParts[0] == current)
            {
                Vector3 startPosTer = current.transform.position + current.transform.forward * 2f;
                Vector3 endPosTer = current.transform.position - current.transform.forward * 3f;
                Debug.Log(120);

                float raycastLimit = 20f;
                int groundLayerMask = LayerMask.GetMask("Ground");

                RaycastHit[] hits = Physics.RaycastAll(endPosTer + Vector3.up * raycastLimit, Vector3.down, raycastLimit * 2, groundLayerMask);
                if (hits.Length > 0)
                {
                    endPosTer = hits[0].point;
                    Debug.Log(120);

                }
                else
                {
                    RaycastHit[] hits1 = Physics.RaycastAll(endPosTer + Vector3.down * raycastLimit, Vector3.up, raycastLimit * 2, groundLayerMask);
                    endPosTer = hits1[0].point;
                    Debug.LogWarning("Не вдалося знайти землю для початкового NavMeshLink!");
                }
                endPosTer.z += 1;
                Debug.Log(120);

                NavMeshLink linkTer = current.AddComponent<NavMeshLink>();
                linkTer.startPoint = current.transform.InverseTransformPoint(startPosTer);
                linkTer.endPoint = current.transform.InverseTransformPoint(endPosTer);
                linkTer.width = navMeshLinkWidth;
                linkTer.bidirectional = navMeshLinkBidirectional;
                linkTer.UpdateLink();
                Debug.Log(120);

            }

            Vector3 startPos = current.transform.position + current.transform.forward * 1.05f * (current.GetComponentInChildren<Renderer>().bounds.size.z);
            Vector3 endPos = current.transform.position + current.transform.forward * 1.5f * (current.GetComponentInChildren<Renderer>().bounds.size.z);

            if (bridgeParts.Length - 1 == i)
            {
                float raycastLimit = 20f;
                int groundLayerMask = LayerMask.GetMask("Ground");

                RaycastHit[] hits = Physics.RaycastAll(endPos + Vector3.up * raycastLimit, Vector3.down, raycastLimit * 2, groundLayerMask);
                if (hits.Length > 0)
                {
                    endPos = hits[0].point;
                }
                else
                {
                    RaycastHit[] hits1 = Physics.RaycastAll(endPos + Vector3.down * raycastLimit, Vector3.up, raycastLimit * 2, groundLayerMask);
                    endPos = hits1[0].point;
                    Debug.LogWarning("Не вдалося знайти землю для початкового NavMeshLink!");
                }
                endPos.z += 1;
            }

            NavMeshLink link = current.AddComponent<NavMeshLink>();
            link.startPoint = current.transform.InverseTransformPoint(startPos);
            link.endPoint = current.transform.InverseTransformPoint(endPos);
            link.width = navMeshLinkWidth;
            link.bidirectional = navMeshLinkBidirectional;
            link.UpdateLink();
        }

        Debug.Log($"Створено {bridgeParts.Length} NavMeshLinks між частинами мосту");
    }

    private void ClearPreview()
    {
        if (previewParts != null)
        {
            foreach (GameObject part in previewParts)
            {
                if (part != null) Destroy(part);
            }
            previewParts = null;
        }

        if (cursorFollowPreview != null)
        {
            Destroy(cursorFollowPreview);
            cursorFollowPreview = null;
        }
    }

    private void CancelPlacement()
    {
        isPlacingBridge = false;
        isFirstPointSelected = false;
        ClearPreview();
        Debug.Log("Bridge placement canceled");
    }

    public void StartPlacingBridge(int costOfTreeBrige, int costOfRockBrige, int costOfGoldBrige)
    {
        this.costOfTreeBrige = costOfTreeBrige;
        this.costOfRockBrige = costOfRockBrige;
        this.costOfGoldBrige = costOfGoldBrige;

        isPlacingBridge = !isPlacingBridge;
        isFirstPointSelected = false;
        ClearPreview();
        Debug.Log("Bridge placement mode: " + (isPlacingBridge ? "ON" : "OFF"));
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
    private async Task<bool> SpawnObjectOnServer(string information)
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
}