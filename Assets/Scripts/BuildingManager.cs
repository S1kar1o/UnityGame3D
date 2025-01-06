using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public GameObject prefab;
    public Terrain terrain;  // Публічне поле для терейну, щоб ви могли передати посилання через інспектор

    public void PlaceBuilt()
    {
        if (terrain != null)
        {
            GameObject newBuilding = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            Building buildingScript = newBuilding.GetComponent<Building>();
            buildingScript.terrain = terrain;  // Передаємо посилання на терейн
        }
        else
        {
            Debug.LogError("Терейн не вказано!");
        }
    }
}
