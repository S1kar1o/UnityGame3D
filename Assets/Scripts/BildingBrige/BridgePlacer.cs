using UnityEngine;
using UnityEngine.UIElements;

public class BridgePlacer : MonoBehaviour
{
    public GameObject bridgePrefab; // ������ �����
    private Vector3 firstPoint;
    private Vector3 secondPoint;
    private bool isFirstPointSelected = false;
    private bool isPlacingBridge = false;

    void Update()
    {
        if (!isPlacingBridge) return; // ���� ����� ������������� �� �������, ������ �� ������

        if (Input.GetMouseButtonDown(0)) // ����� ������ ����
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (!isFirstPointSelected)
                {
                    firstPoint = hit.point;
                    isFirstPointSelected = true;
                    Debug.Log("������ ����� �������: " + firstPoint);
                }
                else
                {
                    secondPoint = hit.point;
                    isFirstPointSelected = false;
                    Debug.Log("������ ����� �������: " + secondPoint);
                    PlaceBridge(); // ������������� ����
                }
            }
        }
    }

    public void StartPlacingBridge()
    {
        isPlacingBridge = !isPlacingBridge;
        isFirstPointSelected = false;
        Debug.Log("����� ������������� �����: " + (isPlacingBridge ? "�������" : "��������"));
    }

    private void PlaceBridge()
    {
        // ������������ ����� ����� ����� �������
        Vector3 midPoint = (firstPoint + secondPoint) / 2;

        // ����������� �� ������ ����� �� ������
        Vector3 direction = (secondPoint - firstPoint).normalized;

        // ������� ����� ��� ����������� ����������
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, secondPoint - firstPoint);

        // ���������� ����� �������
        float distance = Vector3.Distance(firstPoint, secondPoint);

        // ����� ������� (�� ��� Z)
        float bridgeLength = Mathf.Abs(bridgePrefab.GetComponentInChildren<Renderer>().bounds.size.z);

        // ���������� ��������
        int bridgeCount = Mathf.Max(1, Mathf.FloorToInt(distance / bridgeLength) + 1);

        // ������� ��� ��������� ������ � �������
        Debug.Log("�������������� ������:");
        Debug.Log("����� 1: " + firstPoint);
        Debug.Log("����� 2: " + secondPoint);
        Debug.Log("����� (midPoint): " + midPoint);
        Debug.Log("����������� (direction): " + direction);
        Debug.Log("��������� (distance): " + distance);
        Debug.Log("����� ������� (bridgeLength): " + bridgeLength);
        Debug.Log("���������� �������� (bridgeCount): " + bridgeCount);

        float p1 = (bridgeCount * bridgeLength - distance) / 2;
        
        Vector3 position = firstPoint - direction * p1;


        
        Debug.Log("�������� " + p1);

        for (int i = 0; i < bridgeCount; i++)
        {
           
            Debug.Log("������� ����� " + i + ": " + position);

            Instantiate(bridgePrefab, position, rotation);
            position += direction * bridgeLength;
        }


       
    }




}
