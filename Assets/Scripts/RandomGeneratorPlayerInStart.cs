using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random; // ׳��� �������, ���� Random ���������������

public class RandomGeneratorPlayerInStart : MonoBehaviour
{
    public GameObject[] objectToSpawn; // ����� �������
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
        // ��������� ��� ������ �����
        float searchRadius = 200f; // ��������� ����� ������
        int maxAttempts = 50;      // ������ ����� ��� ������ �����
        float navMeshCheckRadius = 100f; // ����� �������� NavMesh

        // ����� ����� (�����������, �� (0,0,0) - �� �����)
        Vector3 mapCenter = Vector3.zero;
        float mapHalfSize = 2048f; // 4096 / 2

        for (int i = 0; i < maxAttempts; i++)
        {
            // ��������� ������� � ����� �����
            Vector3 randomPos = new Vector3(
                Random.Range(-mapHalfSize, mapHalfSize),
                0,
                Random.Range(-mapHalfSize, mapHalfSize)
            );

            // ��������, �� ����� �� NavMesh
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, navMeshCheckRadius, NavMesh.AllAreas))
            {
                // ��������� ��������, ��� �� �������� ��� ����
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

        // ��������� ������ (���� �� ������� �������� �����)
        Debug.LogWarning("�� �������� ������ ������� �� NavMesh. ��������������� ��������� ����� �� ����.");
        return new Vector3(
            Random.Range(-mapHalfSize + 100f, mapHalfSize - 100f),
            0,
            Random.Range(-mapHalfSize + 100f, mapHalfSize - 100f)
        );
    }
}