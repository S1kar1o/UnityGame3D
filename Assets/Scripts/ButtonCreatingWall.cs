using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
public class ButtonCreatingWall : MonoBehaviour
{
    public GameObject prefabWall;
    public Terrain tr;

    public void CreateWall()
    {
        // Створюємо об'єкт
        GameObject newWall = Instantiate(prefabWall, Vector3.zero, Quaternion.identity, transform.parent);

        BuildingWalls bw = newWall.GetComponent<BuildingWalls>();
        bw.terrain = tr;
       
    }
}