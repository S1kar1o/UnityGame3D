using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public GameObject prefab;
    public Terrain terrain;  // ������� ���� ��� �������, ��� �� ����� �������� ��������� ����� ���������

    public void PlaceBuilt()
    {
        Debug.Log(120);
        if (terrain != null)
        {
            GameObject newBuilding = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            Building buildingScript = newBuilding.GetComponent<Building>();
            buildingScript.enabled = true;
            buildingScript.terrain = terrain;  // �������� ��������� �� ������
        }
        else
        {
            Debug.LogError("������ �� �������!");
        }
    }
}
