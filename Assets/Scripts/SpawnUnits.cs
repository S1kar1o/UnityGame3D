using UnityEngine;
using System.Threading.Tasks;
using System.Net.Sockets;
using System;

public class SpawnUnits : MonoBehaviour
{
    [SerializeField] private GameObject[] prefabUnit;
    [SerializeField] private int indexUnit = 0;
    [SerializeField] private UnityTcpClient tcpClient;


    void Start()
    {

        GameObject ut = GameObject.Find("UnityTcpClient");
        tcpClient = ut.GetComponent<UnityTcpClient>();

        indexUnit = tcpClient.IDclient;
    }

    public async void SpawnUnit()
    {
        try
        {
            string name = prefabUnit[indexUnit].name.Replace("(Clone)", "").Trim();

            Vector3 position = new Vector3(
                transform.GetChild(1).position.x + UnityEngine.Random.Range(3f, 10f),
                transform.GetChild(1).position.y,
                transform.GetChild(1).position.z + UnityEngine.Random.Range(3f, 10f)
            );

            float rotateY = prefabUnit[indexUnit].transform.rotation.y + UnityEngine.Random.Range(0f, 360f);
            string requestIdMessage = "ID_GENERATED";

            // Відправляємо запит на ID і чекаємо відповіді
            bool sendRequestResult = await SpawnUnitOnServer(requestIdMessage);

            if (!sendRequestResult)
            {
                Debug.LogError("Failed to notify server, Unit will NOT be spawned.");
                return;
            }

            // Чекаємо отримання ID від сервера
            bool idReceived = await WaitForIDAsync();

            if (!idReceived)
            {
                Debug.LogError("Failed to receive ID from server.");
                return;
            }

            // Відправляємо данні для спавну
            string spawnMessage = $"SPAWN {tcpClient.idUnitGeneratedAtServer} {name} {position.x} {position.y} {position.z} {0} {rotateY} {0}\n";
            bool sendResult = await SpawnUnitOnServer(spawnMessage);

            if (sendResult)
            {
                // Створюємо юніта
                GameObject spawnedUnit = Instantiate(
                    prefabUnit[indexUnit],
                    position,
                    Quaternion.Euler(0, rotateY, 0)
                );

                ServerId sr = spawnedUnit.GetComponent<ServerId>();
                sr.serverId = tcpClient.idUnitGeneratedAtServer;
                tcpClient.idUnitGeneratedAtServer = 0;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in SpawnUnit: {ex.Message}");
        }
    }

    private async Task<bool> WaitForIDAsync()
    {
        float timeout = 5f; // Таймаут 5 секунд
        float startTime = Time.time;

        while (tcpClient.idUnitGeneratedAtServer == 0)
        {
            if (Time.time - startTime > timeout)
            {
                Debug.LogError("Timeout waiting for ID");
                return false;
            }

            await Task.Yield(); // Аналог yield return null для async/await
        }

        return true;
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