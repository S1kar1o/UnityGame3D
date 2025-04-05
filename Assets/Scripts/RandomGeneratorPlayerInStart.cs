using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random; // Чітко вказуємо, який Random використовувати

public class RandomGeneratorPlayerInStart : MonoBehaviour
{
    public GameObject[] objectToSpawn; // Масив префабів
    private UnityTcpClient tcpClient;
    public int indexUnit = 0;
    void Start()
    {
        /*GameObject ut = GameObject.Find("UnityTcpClient");
        tcpClient = ut.GetComponent<UnityTcpClient>();*/
        Debug.Log(120);

        Vector3 randomPos = GetRandomNavMeshPosition();
        Debug.Log(randomPos);
        transform.Translate(randomPos);
        GenerateMessageToServer(randomPos);
        Vector3 cameraPos= new Vector3 (randomPos.x-160, gameObject.transform.position.y, randomPos.z-420);
        gameObject.transform.position=cameraPos;
    }

    private async void GenerateMessageToServer(Vector3 position)
    {
        string name = objectToSpawn[indexUnit].name.Replace("(Clone)", "").Trim();
        string spawnMessage = $"SPAWN {name} {position.x:F2} {position.y:F2} {position.z:F2} {0} {0} {0}\n";

        bool sendResult = await SpawnUnitOnServer(spawnMessage);

        if (sendResult)
        {
            Instantiate(objectToSpawn[indexUnit], position, Quaternion.Euler(0, 0, 0));
        }
        else
        {
            Debug.LogError("Failed to notify server. Unit will NOT be spawned.");
        }
    }

    private async Task<bool> SpawnUnitOnServer(string information)
    {
        return true;

        /* if (tcpClient != null)
         {
             try
             {
                 await tcpClient.SendMessage(information);
                 return true;
             }
             catch (Exception e)
             {
                 Debug.LogError("Error sending spawn command: " + e.Message);
                 return false;
             }
         }
         else
         {
             Debug.LogError("TCPClient is null. Make sure it's assigned.");
             return false;
         }*/
    }

    Vector3 GetRandomNavMeshPosition()
    {
        // Параметри для великої карти
        float searchRadius = 200f; // Збільшений радіус пошуку
        int maxAttempts = 50;      // Більше спроб для великої карти
        float navMeshCheckRadius = 100f; // Радіус перевірки NavMesh

        // Центр карти (припускаючи, що (0,0,0) - це центр)
        Vector3 mapCenter = Vector3.zero;
        float mapHalfSize = 2048f; // 4096 / 2

        for (int i = 0; i < maxAttempts; i++)
        {
            // Випадкова позиція в межах карти
            Vector3 randomPos = new Vector3(
                Random.Range(-mapHalfSize, mapHalfSize),
                0,
                Random.Range(-mapHalfSize, mapHalfSize)
            );

            // Перевірка, чи точка на NavMesh
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, navMeshCheckRadius, NavMesh.AllAreas))
            {
                // Додаткова перевірка, щоб не спавнити біля країв
                if (Mathf.Abs(hit.position.x) < mapHalfSize - 50f &&
                    Mathf.Abs(hit.position.z) < mapHalfSize - 50f)
                {
                    if(hit.position.y < 60)
                    {
                        return GetRandomNavMeshPosition();
                    }
                    return hit.position;
                }
            }
        }

        // Резервний варіант (якщо не знайшли підходящу точку)
        Debug.LogWarning("Не знайдено валідну позицію на NavMesh. Використовується випадкова точка на карті.");
        return new Vector3(
            Random.Range(-mapHalfSize + 100f, mapHalfSize - 100f),
            0,
            Random.Range(-mapHalfSize + 100f, mapHalfSize - 100f)
        );
    }
}