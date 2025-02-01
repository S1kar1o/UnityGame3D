using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class PersistentRockSpawner : MonoBehaviour
{
    public GameObject[] rockPrefabs; // ����� ������� ������
    public GameObject terrain; // ������, �� ����� ������ ����'���� ��'����
    public int rockCount = 100; // ʳ������ ��'����

#if UNITY_EDITOR
    [ContextMenu("Generate Rocks")] // �������� ��������� ��������� � ������������ ����
#endif
    public void GenerateRocks()
    {
        if (terrain == null || rockPrefabs == null || rockPrefabs.Length == 0)
        {
            Debug.LogError("������ ��� ������� �� �����!");
            return;
        }

        Terrain terrainComponent = terrain.GetComponent<Terrain>();
        if (terrainComponent == null)
        {
            Debug.LogError("������ ������� ������ ��������� Terrain!");
            return;
        }

        // �������� ����� � ������� �������
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrainComponent.terrainData.size;

        for (int i = 0; i < rockCount; i++)
        {
            // ��������� �������
            float randomX = Random.Range(terrainPosition.x, terrainPosition.x + terrainSize.x);
            float randomZ = Random.Range(terrainPosition.z, terrainPosition.z + terrainSize.z);
            float height = terrainComponent.SampleHeight(new Vector3(randomX, 0, randomZ)) + terrainPosition.y;

            Vector3 rockPosition = new Vector3(randomX, height, randomZ);

            // ���������� ���� �������
            GameObject randomPrefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];

            // ��������� ���� ��'����
            GameObject rockInstance = Instantiate(randomPrefab, rockPosition, Quaternion.Euler(0, Random.Range(0, 360), 0));

            // ������ �� �������
            rockInstance.transform.parent = terrain.transform;

#if UNITY_EDITOR
            // ��������� ��'��� �� ������� (��� ���������� ���� Play Mode)
            Undo.RegisterCreatedObjectUndo(rockInstance, "Create Rock");
            EditorUtility.SetDirty(rockInstance);
#endif
        }

#if UNITY_EDITOR
        // ��������� ����� �������
        EditorSceneManager.MarkSceneDirty(terrain.scene);
#endif
    }
}
