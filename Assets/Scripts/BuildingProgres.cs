using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingProgres : MonoBehaviour
{
    public int curentBuildingProgresProcents = 0;
    public GameObject buildingCompletedPref;
    public Vector3 buildingCorectionPos;
    
    public void UpdateProgresBuilding(int addProgress)
    {
        curentBuildingProgresProcents += addProgress;
    }
    public void Update()
    {
        checkBuildingProgres();
    }
    public void checkBuildingProgres()
    {
        if (curentBuildingProgresProcents >= 100)
        {
            ServerId id = gameObject.GetComponent<ServerId>();
            GameObject newBuilding= Instantiate(buildingCompletedPref,gameObject.transform.position+ buildingCorectionPos, Quaternion.identity);
            ServerId newObjId= newBuilding.GetComponent<ServerId>();
            newObjId.serverId = id.serverId;

            newBuilding.tag = gameObject.tag;
            Building bd= newBuilding.GetComponent<Building>();
            Destroy(bd);
            SpawnUnits sp = newBuilding.GetComponent<SpawnUnits>();
            if (sp != null)
                sp.enabled = true;
            Destroy(gameObject);

        }
    }
}
