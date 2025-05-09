using System;
using System.Collections;
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
        StartCoroutine(InitializeAfterDelay());
    }

    IEnumerator InitializeAfterDelay()
    {
        // Пошук об'єкта UnityTcpClient
        GameObject ut = GameObject.Find("UnityTcpClient");
        if (ut == null)
        {
            Debug.LogError("UnityTcpClient object not found!");
            yield break;
        }

        // Отримання компонента
        tcpClient = ut.GetComponent<UnityTcpClient>();
        if (tcpClient == null)
        {
            Debug.LogError("UnityTcpClient component not found!");
            yield break;
        }

        // Основна логіка
        indexUnit = tcpClient.IDclient;
        Vector3 randomPos = GetRandomNavMeshPosition();
        Debug.Log(randomPos);

        // Переміщення об'єкта
        GenerateMessageToServer(randomPos);

        // Встановлення позиції камери
        Vector3 cameraPos = new Vector3(randomPos.x ,gameObject.transform.position.y+100, randomPos.z - 740);
       gameObject.transform.position = cameraPos;
    }

    private async void GenerateMessageToServer(Vector3 position)
    {
        string name = objectToSpawn[indexUnit].name.Replace("(Clone)", "").Trim();
        int id = 1676+tcpClient.IDclient;

        string spawnMessage = $"SPAWN {id} {name} {position.x:F2} {position.y:F2} {position.z:F2} {0} {0} {0}\n";

        bool sendResult = await SpawnUnitOnServer(spawnMessage);

        if (sendResult)
        {
            GameObject spawnedUnit = Instantiate(objectToSpawn[indexUnit], position, Quaternion.Euler(0, 0, 0));
            ServerId serverIdComponent = spawnedUnit.GetComponent<ServerId>();
            if (serverIdComponent != null)
            {
                serverIdComponent.serverId = id;
            }
            else
            {
                Debug.LogWarning("Компонент ServerId не знайдено на об'єкті.");
            }
        }
        else
        {
            Debug.LogError("Failed to notify server. Unit will NOT be spawned.");
        }
    }

    private async Task<bool> SpawnUnitOnServer(string information)
    {
        if (tcpClient != null)
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
        }
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