using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class PersistentRockSpawner : MonoBehaviour
{
    public GameObject[] rockPrefabs; // Масив префабів каміння
    public GameObject terrain; // Терейн, до якого будуть прив'язані об'єкти
    public int rockCount = 100; // Кількість об'єктів

#if UNITY_EDITOR
    [ContextMenu("Generate Rocks")] // Дозволяє запускати генерацію з контекстного меню
#endif
    public void GenerateRocks()
    {
        if (terrain == null || rockPrefabs == null || rockPrefabs.Length == 0)
        {
            Debug.LogError("Терейн або префаби не задані!");
            return;
        }

        Terrain terrainComponent = terrain.GetComponent<Terrain>();
        if (terrainComponent == null)
        {
            Debug.LogError("Терейн повинен містити компонент Terrain!");
            return;
        }

        // Отримуємо розмір і позицію терейну
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrainComponent.terrainData.size;

        for (int i = 0; i < rockCount; i++)
        {
            // Випадкова позиція
            float randomX = Random.Range(terrainPosition.x, terrainPosition.x + terrainSize.x);
            float randomZ = Random.Range(terrainPosition.z, terrainPosition.z + terrainSize.z);
            float height = terrainComponent.SampleHeight(new Vector3(randomX, 0, randomZ)) + terrainPosition.y;

            Vector3 rockPosition = new Vector3(randomX, height, randomZ);

            // Випадковий вибір префабу
            GameObject randomPrefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];

            // Створюємо копію об'єкта
            GameObject rockInstance = Instantiate(randomPrefab, rockPosition, Quaternion.Euler(0, Random.Range(0, 360), 0));

            // Додаємо до терейну
            rockInstance.transform.parent = terrain.transform;

#if UNITY_EDITOR
            // Визначаємо об'єкт як змінений (для збереження після Play Mode)
            Undo.RegisterCreatedObjectUndo(rockInstance, "Create Rock");
            EditorUtility.SetDirty(rockInstance);
#endif
        }

#if UNITY_EDITOR
        // Позначаємо сцену зміненою
        EditorSceneManager.MarkSceneDirty(terrain.scene);
#endif
    }
}
