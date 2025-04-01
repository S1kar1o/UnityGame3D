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
    public Color cursorFollowColor = new Color(0, 0.5f, 1f, 0.7f); // Новый цвет для элемента, следующего за курсором

    [Header("Terrain Connection")]
    public float connectionSearchDistance = 20f;
    public LayerMask groundLayerMask;

    private Vector3 firstPoint;
    private Vector3 secondPoint;
    private bool isFirstPointSelected = false;
    private bool isPlacingBridge = false;
    private bool isValidPlacement = true;
    private GameObject[] bridgeParts;
    private GameObject[] previewParts;
    private GameObject cursorFollowPreview; // Новый объект для превью, следующего за курсором

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
                UpdateCursorFollowPreview(hit.point); // Обновляем позицию превью, следующего за курсором
            }
            else
            {
                secondPoint = hit.point;
                isValidPlacement = CheckPlacementValidity(firstPoint, secondPoint);
                UpdateBridgePreview();
            }

            if (Input.GetMouseButtonDown(0) && isValidPlacement)
            {
                if (IsValidLocation(hit))
                {
                    if (!isFirstPointSelected)
                    {
                        firstPoint = hit.point;
                        isFirstPointSelected = true;
                        Destroy(cursorFollowPreview); // Уничтожаем превью, следующее за курсором
                        cursorFollowPreview = null;
                    }
                    else
                    {
                        secondPoint = hit.point;
                        if (CheckPlacementValidity(firstPoint, secondPoint))
                        {
                            ClearPreview();
                            PlaceBridge();
                            isPlacingBridge = false;
                        }
                    }
                }
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }

    private void UpdateCursorFollowPreview(Vector3 position)
    {
        if (cursorFollowPreview == null)
        {
            cursorFollowPreview = Instantiate(bridgePrefab, position, Quaternion.identity);
            cursorFollowPreview.tag = "Untagged";

            // Отключаем коллайдеры
            foreach (var collider in cursorFollowPreview.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            // Устанавливаем цвет
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

        Color previewColor = isValidPlacement ? validPlacementColor : invalidPlacementColor;

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

            if (bridgeParts[0] == current)
            {
                Vector3 startPosTer = current.transform.position + current.transform.forward * 2f;
                Vector3 endPosTer = current.transform.position - current.transform.forward * 3f;

                float raycastLimit = 20f;
                int groundLayerMask = LayerMask.GetMask("Ground");

                RaycastHit[] hits = Physics.RaycastAll(endPosTer + Vector3.up * raycastLimit, Vector3.down, raycastLimit * 2, groundLayerMask);
                if (hits.Length > 0)
                {
                    endPosTer = hits[0].point;
                }
                else
                {
                    RaycastHit[] hits1 = Physics.RaycastAll(endPosTer + Vector3.down * raycastLimit, Vector3.up, raycastLimit * 2, groundLayerMask);
                    endPosTer = hits1[0].point;
                    Debug.LogWarning("Не вдалося знайти землю для початкового NavMeshLink!");
                }
                endPosTer.z += 1;

                NavMeshLink linkTer = current.AddComponent<NavMeshLink>();
                linkTer.startPoint = current.transform.InverseTransformPoint(startPosTer);
                linkTer.endPoint = current.transform.InverseTransformPoint(endPosTer);
                linkTer.width = navMeshLinkWidth;
                linkTer.bidirectional = navMeshLinkBidirectional;
                linkTer.UpdateLink();
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

    public void StartPlacingBridge()
    {
        isPlacingBridge = !isPlacingBridge;
        isFirstPointSelected = false;
        ClearPreview();
        Debug.Log("Bridge placement mode: " + (isPlacingBridge ? "ON" : "OFF"));
    }
}