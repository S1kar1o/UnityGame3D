using UnityEngine;

public class CreateSphereOnTerrain : MonoBehaviour
{
    Terrain terrain = Terrain.activeTerrain; // ������ ����� (����� ������� � ����������)

    [ContextMenu("Gen1")]
    public void Gen1()
    {
        // �������� ������� �������
        
        if (terrain == null)
        {
            Debug.LogError("������� �� ������!");
            return;
        }

        // �������� ������ � ������� ��������
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainPosition = terrain.transform.position;

        // ���������� ����������� ����� ��������
        float centerX = terrainPosition.x + terrainSize.x / 2f;
        float centerZ = terrainPosition.z + terrainSize.z / 2f;

        // ���������� ������ � ����������� �����
        float centerY = terrain.SampleHeight(new Vector3(centerX, 0, centerZ)) + terrainPosition.y;

        // ������ �����
        
            // ������ ����� ����������, ���� ������ �� ������
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = new Vector3(centerX, centerY, centerZ);
            sphere.transform.localScale = new Vector3(2, 2, 2); // �������� ������ (�� �������)
        
    }
}
