using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

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

    private Vector3 firstPoint;
    private Vector3 secondPoint;
    private bool isFirstPointSelected = false;
    private bool isPlacingBridge = false;
    private bool isValidPlacement = true;
    private bool hasWaterUnderBridge = false;
    private GameObject[] bridgeParts;
    private GameObject[] previewParts;
    private GameObject cursorFollowPreview;

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
                    if (!isFirstPointSelected)
                    {
                        firstPoint = hit.point;
                        isFirstPointSelected = true;
                        Destroy(cursorFollowPreview);
                        cursorFollowPreview = null;
                    }
                    else if (isValidPlacement && hasWaterUnderBridge)
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
        float offset = (bridgeCount * bridgeLength - distance) / 2;

        Vector3 position = firstPoint - direction * offset;
        previewParts = new GameObject[bridgeCount];

        Color previewColor = (isValidPlacement && hasWaterUnderBridge) ? validPlacementColor : invalidPlacementColor;

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

    private void PlaceBridge()
    {
        Vector3 direction = (secondPoint - firstPoint).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);
        float distance = Vector3.Distance(firstPoint, secondPoint);
        float bridgeLength = bridgePrefab.GetComponentInChildren<Renderer>().bounds.size.z;

        int bridgeCount = Mathf.Max(1, Mathf.FloorToInt(distance / bridgeLength) + 1);
        float offset = (bridgeCount * bridgeLength - distance) / 2;

        Vector3 position = firstPoint - direction * offset;
        bridgeParts = new GameObject[bridgeCount];

        for (int i = 0; i < bridgeCount; i++)
        {
            bridgeParts[i] = Instantiate(bridgePrefab, position, rotation);
            bridgeParts[i].tag = "Bridge";
            position += direction * bridgeLength;
        }

        CreateNavMeshLinks();
    }

    private void CreateNavMeshLinks()
    {
        for (int i = 0; i < bridgeParts.Length; i++)
        {
            GameObject current = bridgeParts[i];
            Bounds bounds = current.GetComponentInChildren<Renderer>().bounds;

            // Create connection to terrain for first and last parts
            if (i == 0 || i == bridgeParts.Length - 1)
            {
                CreateTerrainConnection(current, i == 0);
            }

            // Create links between bridge parts
            if (i < bridgeParts.Length - 1)
            {
                CreateBridgeLink(current, bridgeParts[i + 1]);
            }
        }
    }

    private void CreateTerrainConnection(GameObject bridgePart, bool isFirstPart)
    {
        Bounds bounds = bridgePart.GetComponentInChildren<Renderer>().bounds;
        Vector3 direction = isFirstPart ? -bridgePart.transform.forward : bridgePart.transform.forward;
        Vector3 edgePosition = bridgePart.transform.position + direction * (bounds.size.z / 2);

        Vector3 terrainPoint = FindTerrainConnectionPoint(edgePosition);

        NavMeshLink link = bridgePart.AddComponent<NavMeshLink>();
        link.startPoint = bridgePart.transform.InverseTransformPoint(edgePosition);
        link.endPoint = bridgePart.transform.InverseTransformPoint(terrainPoint);
        link.width = navMeshLinkWidth * 1.5f;
        link.bidirectional = navMeshLinkBidirectional;
        link.area = NavMesh.GetAreaFromName("Walkable");
        link.UpdateLink();
    }

    private Vector3 FindTerrainConnectionPoint(Vector3 startPosition)
    {
        if (Physics.Raycast(startPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit,
            connectionSearchDistance * 2, groundLayerMask))
        {
            return hit.point + Vector3.up * 0.1f;
        }

        if (Physics.Raycast(startPosition, Vector3.up, out hit, connectionSearchDistance, groundLayerMask))
        {
            return hit.point - Vector3.up * 0.1f;
        }

        Debug.LogWarning("No terrain found for connection!");
        return startPosition;
    }

    private void CreateBridgeLink(GameObject currentPart, GameObject nextPart)
    {
        Bounds currentBounds = currentPart.GetComponentInChildren<Renderer>().bounds;
        Bounds nextBounds = nextPart.GetComponentInChildren<Renderer>().bounds;

        Vector3 startPos = currentPart.transform.position +
                         currentPart.transform.forward * (currentBounds.size.z / 2);
        Vector3 endPos = nextPart.transform.position -
                       nextPart.transform.forward * (nextBounds.size.z / 2);

        NavMeshLink link = currentPart.AddComponent<NavMeshLink>();
        link.startPoint = currentPart.transform.InverseTransformPoint(startPos);
        link.endPoint = currentPart.transform.InverseTransformPoint(endPos);
        link.width = navMeshLinkWidth;
        link.bidirectional = navMeshLinkBidirectional;
        link.area = NavMesh.GetAreaFromName("Walkable");
        link.UpdateLink();
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

    public void StartPlacingBridge()
    {
        isPlacingBridge = !isPlacingBridge;
        isFirstPointSelected = false;
        ClearPreview();
        Debug.Log("Bridge placement mode: " + (isPlacingBridge ? "ON" : "OFF"));
    }
}