using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class BridgePlacer : MonoBehaviour
{
    public GameObject bridgePrefab; // Префаб моста
    public float navMeshLinkWidth = 2f; // Ширина NavMeshLink
    public bool navMeshLinkBidirectional = true; // Двосторонній лінк

    private Vector3 firstPoint;
    private Vector3 secondPoint;
    private bool isFirstPointSelected = false;
    private bool isPlacingBridge = false;
    private GameObject[] bridgeParts; // Масив частин мосту

    void Update()
    {
        if (!isPlacingBridge) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (!isFirstPointSelected)
                {
                    firstPoint = hit.point;
                    isFirstPointSelected = true;
                    Debug.Log("Перша точка обрана: " + firstPoint);
                }
                else
                {
                    secondPoint = hit.point;
                    isFirstPointSelected = false;
                    Debug.Log("Друга точка обрана: " + secondPoint);
                    PlaceBridge();
                }
            }
        }
    }

    public void StartPlacingBridge()
    {
        isPlacingBridge = !isPlacingBridge;
        isFirstPointSelected = false;
        Debug.Log("Режим будівництва мосту: " + (isPlacingBridge ? "Увімкнено" : "Вимкнено"));
    }

    private void PlaceBridge()
    {
        Vector3 direction = (secondPoint - firstPoint).normalized;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, direction);
        float distance = Vector3.Distance(firstPoint, secondPoint);
        float bridgeLength = bridgePrefab.GetComponentInChildren<Renderer>().bounds.size.z;

        int bridgeCount = Mathf.Max(1, Mathf.FloorToInt(distance / bridgeLength) + 1);
        float p1 = (bridgeCount * bridgeLength - distance) / 2;

        Vector3 position = firstPoint - direction * p1;
        bridgeParts = new GameObject[bridgeCount];

        // Створюємо частини мосту
        for (int i = 0; i < bridgeCount; i++)
        {
            bridgeParts[i] = Instantiate(bridgePrefab, position, rotation);
            position += direction * bridgeLength;
        }

        // Створюємо NavMeshLinks між частинами
        CreateNavMeshLinks();
    }

    private void CreateNavMeshLinks()
    {
        for (int i = 0; i < bridgeParts.Length; i++)
        {
            GameObject current = bridgeParts[i];

            if (bridgeParts[0] == current) {
                Vector3 startPosTer = current.transform.position + current.transform.forward * 2f;
                Vector3 endPosTer = current.transform.position - current.transform.forward *3f;

                // Ліміт пошуку вгору і вниз
                float raycastLimit = 20f;

                // Отримуємо шар "Ground"
                int groundLayerMask = LayerMask.GetMask("Ground");

                RaycastHit[] hits = Physics.RaycastAll(endPosTer + Vector3.up * raycastLimit, Vector3.down, raycastLimit * 2, groundLayerMask);
                if (hits.Length > 0)
                {
                    endPosTer = hits[0].point;
                }
                else
                {
                    Debug.LogWarning("Не вдалося знайти землю!");
                }
                endPosTer.z += 1;


                NavMeshLink linkTer = current.AddComponent<NavMeshLink>();
                linkTer.startPoint = current.transform.InverseTransformPoint(startPosTer);
                linkTer.endPoint = current.transform.InverseTransformPoint(endPosTer);
                linkTer.width = navMeshLinkWidth;
                linkTer.bidirectional = navMeshLinkBidirectional;
                linkTer.UpdateLink();
            }
            // Визначаємо позиції для лінків
            Vector3 startPos = current.transform.position + current.transform.forward * 1.05f*(current.GetComponentInChildren<Renderer>().bounds.size.z);
            Vector3 endPos = current.transform.position + current.transform.forward * 1.5f * (current.GetComponentInChildren<Renderer>().bounds.size.z);
            if (bridgeParts.Length-1 == i)
            {
                // Ліміт пошуку вгору і вниз
                float raycastLimit = 20f;

                // Отримуємо шар "Ground"
                int groundLayerMask = LayerMask.GetMask("Ground");

                RaycastHit[] hits = Physics.RaycastAll(endPos + Vector3.up * raycastLimit, Vector3.down, raycastLimit * 2, groundLayerMask);
                if (hits.Length > 0)
                {
                    endPos = hits[0].point;
                }
                else
                {
                    Debug.LogWarning("Не вдалося знайти землю!");
                }
                endPos.z += 1;
            }
            // Створюємо лінк
            NavMeshLink link = current.AddComponent<NavMeshLink>();
            link.startPoint = current.transform.InverseTransformPoint(startPos);
            link.endPoint = current.transform.InverseTransformPoint(endPos);
            link.width = navMeshLinkWidth;
            link.bidirectional = navMeshLinkBidirectional;
            link.UpdateLink();
        }

        Debug.Log($"Створено {bridgeParts.Length} NavMeshLinks між частинами мосту");
    }
}