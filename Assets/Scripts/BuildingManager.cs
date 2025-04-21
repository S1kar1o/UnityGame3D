using System.Collections;
using TMPro;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public GameObject prefab;
    public Terrain terrain;
    public int costOfTree, costOfRock, costOfGold;
    public TextMeshProUGUI textCostOfTree, textCostOfRock, textCostOfGold;
    private Coroutine resourceCheckCoroutine;

    private void OnEnable()
    {
        resourceCheckCoroutine = StartCoroutine(UpdateResourceColorsRoutine());
    }

    private void OnDisable()
    {
        if (resourceCheckCoroutine != null)
            StopCoroutine(resourceCheckCoroutine);
    }

    private IEnumerator UpdateResourceColorsRoutine()
    {
        while (true)
        {
            UpdateResourceColors();
            yield return new WaitForSeconds(0.3f);
        }
    }
    private void UpdateResourceColors()
    {
        UpdateTextColor(textCostOfGold, UnityTcpClient.Instance.goldAmount, costOfGold);
        UpdateTextColor(textCostOfRock, UnityTcpClient.Instance.rockAmount, costOfRock);
        UpdateTextColor(textCostOfTree, UnityTcpClient.Instance.woodAmount, costOfTree);
    }

    private void UpdateTextColor(TMP_Text text, int currentAmount, int cost)
    {
        text.color = currentAmount >= cost ? Color.green : Color.red;
    }
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
            buildingScript.costOfTree = costOfTree;
            buildingScript.costOfRock = costOfRock;
            buildingScript.costOfGold = costOfGold;
            buildingScript.enabled = true;
            buildingScript.terrain = terrain;  // Передаємо посилання на терейн
        }
        else
        {
            Debug.LogError("Терейн не вказано!");
        }
    }
}
