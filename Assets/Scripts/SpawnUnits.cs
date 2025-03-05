using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpawnUnits : MonoBehaviour
{
    public List<GameObject> prefabUnit = new List<GameObject>();
    public int indexUnit=0;
    
    private UnityTcpClient tcpClient;
    
    void Start()
    {

        GameObject ut = GameObject.Find("UnityTcpClient");
        tcpClient = ut.GetComponent<UnityTcpClient>();
        // Знайти Canvas
       
        indexUnit = tcpClient.IDclient;
    }

    public async void SpawnUnit() // Changed to async void to call an async method.
    {
        string name = prefabUnit[indexUnit].name.Replace("(Clone)", "").Trim();

        Vector3 position = new Vector3(
            transform.GetChild(1).position.x + UnityEngine.Random.Range(3f, 10f),
            transform.GetChild(1).position.y,
            transform.GetChild(1).position.z + UnityEngine.Random.Range(3f, 10f)
        );
        float rotateY = prefabUnit[indexUnit].transform.rotation.y+ UnityEngine.Random.Range(0f, 360f);

        // Construct the spawn message. Use string interpolation for readability.
        string spawnMessage = $"SPAWN {name} {position.x} {position.y} {position.z} {0} {rotateY} {0}\n";

        //Try Send spawn message
        bool sendResult = await SpawnUnitOnServer(spawnMessage);
        if (sendResult)
        {
            // Only instantiate the unit if the server acknowledges the spawn.
            Instantiate(prefabUnit[indexUnit], position, Quaternion.Euler(0, rotateY, 0));
        }
        else
        {
            Debug.LogError("Failed to notify server, Unit will NOT be spawn.");
        }
    }
    private async Task<bool> SpawnUnitOnServer(string information)
    {
        if (tcpClient != null)
        {
            try
            {
                await tcpClient.SendMessage(information);
                return true; // Indicate success

            }
            catch (Exception e)
            {
                Debug.LogError("Error sending spawn command: " + e.Message);
                return false; // Indicate failure
            }
        }
        else
        {
            Debug.LogError("TCPClient is null.  Make sure it's assigned.");
            return false;
        }
    }

}
