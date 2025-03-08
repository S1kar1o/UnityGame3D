using UnityEngine;
using Unity.AI.Navigation; // ���� ������������� AI Navigation Package

public class BridgeNavMeshUpdater : MonoBehaviour
{
    public NavMeshSurface groundSurface;  // ��������� �� NavMesh �������� ����
    public NavMeshSurface bridgeSurface;  // ��������� �� NavMesh �������� �����

    void Start()
    {
        // ������ ��� � ��������
        UpdateNavMesh();
    }

    public void UpdateNavMesh()
    {
        // �������� ��������� NavMesh ��� �����
        bridgeSurface.BuildNavMesh();

        // ����� ��������� ������� �������� ����, ��������� ���
        groundSurface.BuildNavMesh();
    }
}
