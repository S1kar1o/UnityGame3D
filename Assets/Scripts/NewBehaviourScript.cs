using UnityEngine;
using UnityEngine.AI;

public class ConvertTreesToObjects : MonoBehaviour
{
    public Terrain terrain; // Посилання на ваш Terrain
    public GameObject[] treePrefabs; // Масив префабів дерев

    void Start()
    {
        TerrainData terrainData = terrain.terrainData;
        TreePrototype[] treePrototypes = terrainData.treePrototypes;

        foreach (TreeInstance treeInstance in terrainData.treeInstances)
        {
            // Знаходимо префаб, відповідний прототипу дерева
            int prototypeIndex = treeInstance.prototypeIndex;
            if (prototypeIndex >= 0 && prototypeIndex < treePrefabs.Length)
            {
                // Визначаємо світові координати дерева
                Vector3 worldPos = Vector3.Scale(treeInstance.position, terrainData.size) + terrain.transform.position;

                // Створюємо дерево з відповідного префабу
                GameObject treeObject = Instantiate(treePrefabs[prototypeIndex], worldPos, Quaternion.identity);

                // Додаємо NavMeshObstacle
                NavMeshObstacle obstacle = treeObject.AddComponent<NavMeshObstacle>();
                obstacle.carving = true; // Вмикає вирізання на NavMesh
            }
        }

        // Видаляємо дерева з Terrain
        terrain.terrainData.treeInstances = new TreeInstance[0];
    }
}
