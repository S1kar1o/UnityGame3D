using UnityEngine;

public class CreateSphereOnTerrain : MonoBehaviour
{
    Terrain terrain = Terrain.activeTerrain; // Префаб сферы (можно указать в инспекторе)

    [ContextMenu("Gen1")]
    public void Gen1()
    {
        // Получаем текущий террейн
        
        if (terrain == null)
        {
            Debug.LogError("Террейн не найден!");
            return;
        }

        // Получаем размер и позицию террейна
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainPosition = terrain.transform.position;

        // Определяем центральную точку террейна
        float centerX = terrainPosition.x + terrainSize.x / 2f;
        float centerZ = terrainPosition.z + terrainSize.z / 2f;

        // Определяем высоту в центральной точке
        float centerY = terrain.SampleHeight(new Vector3(centerX, 0, centerZ)) + terrainPosition.y;

        // Создаём сферу
        
            // Создаём сферу программно, если префаб не указан
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = new Vector3(centerX, centerY, centerZ);
            sphere.transform.localScale = new Vector3(2, 2, 2); // Изменяем размер (по желанию)
        
    }
}
