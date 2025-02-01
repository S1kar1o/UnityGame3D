using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.UI;

public class SpawnUnits : MonoBehaviour
{
    public List<GameObject> prefabUnit = new List<GameObject>();
    public int Good;
    public GameObject hirePanel;
    public Button hireButton;
    public void ActivatePanelRecruiting()
    {
        hirePanel.SetActive(true); 

    }
    void Start()
    {
        if (hirePanel != null)
        {
            hirePanel.SetActive(false); // Приховуємо панель на старті
            hireButton.onClick.AddListener(SpawnUnit); // Викликаємо коректний метод

        }
    }

    void SpawnUnit()
    {
        Vector3 position= new Vector3(
            transform.GetChild(1).position.x+ UnityEngine.Random.Range(3f,10f),
            transform.GetChild(1).position.y,
            transform.GetChild(1).position.z+UnityEngine.Random.Range(3f,10f)
            );
    
        Instantiate(prefabUnit[Good],position, Quaternion.identity);
    }
}
