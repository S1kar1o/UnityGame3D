using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public GameObject prefab;
    public Terrain terrain; 
    public void PlaceBuilt()
    {
        if (terrain != null)
        {
            GameObject newBuilding = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            Building buildingScript = newBuilding.GetComponent<Building>();
            BuildingWalls building;
            if(buildingScript == null )
            {
                building = newBuilding.GetComponent<BuildingWalls>();
                building.enabled = true;
            }
            buildingScript.enabled = true;
            buildingScript.terrain = terrain;  // Передаємо посилання на терейн
        }
        else
        {
            Debug.LogError("Терейн не вказано!");
        }
    }
}
