using UnityEngine;
using UnityEngine.AI;

public class ConvertTreesToObjects : MonoBehaviour
{
    public Terrain terrain; // ��������� �� ��� Terrain
    public GameObject[] treePrefabs; // ����� ������� �����

    void Start()
    {
        TerrainData terrainData = terrain.terrainData;
        TreePrototype[] treePrototypes = terrainData.treePrototypes;

        foreach (TreeInstance treeInstance in terrainData.treeInstances)
        {
            // ��������� ������, ��������� ��������� ������
            int prototypeIndex = treeInstance.prototypeIndex;
            if (prototypeIndex >= 0 && prototypeIndex < treePrefabs.Length)
            {
                // ��������� ����� ���������� ������
                Vector3 worldPos = Vector3.Scale(treeInstance.position, terrainData.size) + terrain.transform.position;

                // ��������� ������ � ���������� �������
                GameObject treeObject = Instantiate(treePrefabs[prototypeIndex], worldPos, Quaternion.identity);

                // ������ NavMeshObstacle
                NavMeshObstacle obstacle = treeObject.AddComponent<NavMeshObstacle>();
                obstacle.carving = true; // ����� �������� �� NavMesh
            }
        }

        // ��������� ������ � Terrain
        terrain.terrainData.treeInstances = new TreeInstance[0];
    }
}
