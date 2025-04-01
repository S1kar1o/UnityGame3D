using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class BridgePlacer : MonoBehaviour
{
    public GameObject bridgePrefab; // ������ �����
    public float navMeshLinkWidth = 2f; // ������ NavMeshLink
    public bool navMeshLinkBidirectional = true; // ����������� ���

    private Vector3 firstPoint;
    private Vector3 secondPoint;
    private bool isFirstPointSelected = false;
    private bool isPlacingBridge = false;
    private GameObject[] bridgeParts; // ����� ������ �����

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
                    Debug.Log("����� ����� ������: " + firstPoint);
                }
                else
                {
                    secondPoint = hit.point;
                    isFirstPointSelected = false;
                    Debug.Log("����� ����� ������: " + secondPoint);
                    PlaceBridge();
                }
            }
        }
    }

    public void StartPlacingBridge()
    {
        isPlacingBridge = !isPlacingBridge;
        isFirstPointSelected = false;
        Debug.Log("����� ���������� �����: " + (isPlacingBridge ? "��������" : "��������"));
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

        // ��������� ������� �����
        for (int i = 0; i < bridgeCount; i++)
        {
            bridgeParts[i] = Instantiate(bridgePrefab, position, rotation);
            position += direction * bridgeLength;
        }

        // ��������� NavMeshLinks �� ���������
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

                // ˳�� ������ ����� � ����
                float raycastLimit = 20f;

                // �������� ��� "Ground"
                int groundLayerMask = LayerMask.GetMask("Ground");

                RaycastHit[] hits = Physics.RaycastAll(endPosTer + Vector3.up * raycastLimit, Vector3.down, raycastLimit * 2, groundLayerMask);
                if (hits.Length > 0)
                {
                    endPosTer = hits[0].point;
                }
                else
                {
                    Debug.LogWarning("�� ������� ������ �����!");
                }
                endPosTer.z += 1;


                NavMeshLink linkTer = current.AddComponent<NavMeshLink>();
                linkTer.startPoint = current.transform.InverseTransformPoint(startPosTer);
                linkTer.endPoint = current.transform.InverseTransformPoint(endPosTer);
                linkTer.width = navMeshLinkWidth;
                linkTer.bidirectional = navMeshLinkBidirectional;
                linkTer.UpdateLink();
            }
            // ��������� ������� ��� ����
            Vector3 startPos = current.transform.position + current.transform.forward * 1.05f*(current.GetComponentInChildren<Renderer>().bounds.size.z);
            Vector3 endPos = current.transform.position + current.transform.forward * 1.5f * (current.GetComponentInChildren<Renderer>().bounds.size.z);
            if (bridgeParts.Length-1 == i)
            {
                // ˳�� ������ ����� � ����
                float raycastLimit = 20f;

                // �������� ��� "Ground"
                int groundLayerMask = LayerMask.GetMask("Ground");

                RaycastHit[] hits = Physics.RaycastAll(endPos + Vector3.up * raycastLimit, Vector3.down, raycastLimit * 2, groundLayerMask);
                if (hits.Length > 0)
                {
                    endPos = hits[0].point;
                }
                else
                {
                    Debug.LogWarning("�� ������� ������ �����!");
                }
                endPos.z += 1;
            }
            // ��������� ���
            NavMeshLink link = current.AddComponent<NavMeshLink>();
            link.startPoint = current.transform.InverseTransformPoint(startPos);
            link.endPoint = current.transform.InverseTransformPoint(endPos);
            link.width = navMeshLinkWidth;
            link.bidirectional = navMeshLinkBidirectional;
            link.UpdateLink();
        }

        Debug.Log($"�������� {bridgeParts.Length} NavMeshLinks �� ��������� �����");
    }
}