using System.Collections.Generic;
using UnityEngine;

public class CreateBridge : MonoBehaviour
{
    public Dictionary<int, River> rivers;
    public GameObject boxPrefab;
    public GameObject boxPreviewPrefab;
    private GameObject previewBridge;
    private Vector3 scaleBase;

    private void Start()
    {
        GameObject box = Instantiate(boxPrefab, Vector3.zero, Quaternion.identity);
        scaleBase = box.transform.localScale;

        RiverGenerator river = FindAnyObjectByType<RiverGenerator>();
        rivers = river.LoadDataRivers(rivers);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.B))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObject = hit.collider.gameObject;
                Vector3 localHitPoint = hit.transform.InverseTransformPoint(hit.point);

                foreach (var river in rivers)
                {
                    if (river.Value.meshObject == hitObject)
                    {
                        int id = river.Key;
                        Vector3 point1 = CalculateRiverBankPoint(river.Value.riverLineDown, localHitPoint);
                        Vector3 point2 = CalculateRiverBankPoint(river.Value.riverLineTop, localHitPoint);

                        // Управляем предварительным просмотром моста
                        HandlePreviewBridge(point1, point2);

                        if (Input.GetMouseButtonDown(0))
                        {
                            SpawnBridge(boxPrefab, point1, point2);
                            Destroy(previewBridge);
                        }
                        return;
                    }
                }

                // Удаляем предварительный просмотр, если мышь не над рекой
                DestroyPreviewBridge();
            }
        }
        else
        {
            DestroyPreviewBridge();
        }

        
    }

    private Vector3 CalculateRiverBankPoint(Vector2[] riverLine, Vector3 hitPoint)
    {
        Vector2 hitPoint2D = new Vector2(hitPoint.x, hitPoint.z);
        Vector2 closestPoint = FindClosestPoint(riverLine, hitPoint2D);
        return new Vector3(closestPoint.x, hitPoint.y, closestPoint.y);
    }

    private Vector2 FindClosestPoint(Vector2[] points, Vector2 targetPoint)
    {
        Vector2 closestPoint = Vector2.zero;
        float minDistance = float.MaxValue;

        foreach (Vector2 point in points)
        {
            float distance = Vector2.Distance(targetPoint, point);
            if (distance < minDistance)
            {
                closestPoint = point;
                minDistance = distance;
            }
        }

        return closestPoint;
    }

    private void HandlePreviewBridge(Vector3 point1, Vector3 point2)
    {
        if (previewBridge == null)
        {
            previewBridge = InstantiateBridge(boxPreviewPrefab, point1, point2);
        }
        else
        {
            UpdateBridge(previewBridge, point1, point2);
        }
    }

    private void DestroyPreviewBridge()
    {
        if (previewBridge != null)
        {
            Destroy(previewBridge);
        }
    }

    private GameObject InstantiateBridge(GameObject prefab, Vector3 point1, Vector3 point2)
    {
        GameObject bridge = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        UpdateBridge(bridge, point1, point2);
        return bridge;
    }

    private void UpdateBridge(GameObject bridge, Vector3 point1, Vector3 point2)
    {
        bridge.transform.position = Vector3.zero;
        bridge.transform.rotation = Quaternion.identity;
        bridge.transform.localScale = scaleBase;

        Vector3 direction = (point2 - point1).normalized;
        float distance = Vector3.Distance(point1, point2);

        MeshRenderer meshRenderer = bridge.GetComponent<MeshRenderer>();
        float initialLength = 1f;
        Vector3 centerOffset = Vector3.zero;

        if (meshRenderer != null)
        {
            initialLength = meshRenderer.bounds.size.z / bridge.transform.localScale.z;
            centerOffset = meshRenderer.bounds.center - bridge.transform.position;
        }
        else
        {
            Debug.LogWarning("MeshRenderer отсутствует. Используются значения по умолчанию.");
        }

        // Обновляем масштаб
        Vector3 scale = bridge.transform.localScale;
        scale.z = distance / initialLength;
        bridge.transform.localScale = scale;

        // Обновляем ориентацию
        bridge.transform.rotation = Quaternion.LookRotation(direction);

        // Обновляем позицию
        Vector3 adjustedCenter = (point1 + point2) / 2 - bridge.transform.rotation * Vector3.Scale(centerOffset, bridge.transform.localScale);
        adjustedCenter.y += bridge.transform.localScale.y + 1f; // Поднимаем мост над поверхностью
        bridge.transform.position = adjustedCenter;
    }

    private void SpawnBridge(GameObject prefab, Vector3 point1, Vector3 point2)
    {
        InstantiateBridge(prefab, point1, point2);
    }
}
