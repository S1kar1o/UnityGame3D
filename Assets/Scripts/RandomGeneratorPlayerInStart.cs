using System;
using System.Collections;
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
        StartCoroutine(InitializeAfterDelay());
    }

    IEnumerator InitializeAfterDelay()
    {
        // ����� ��'���� UnityTcpClient
        GameObject ut = GameObject.Find("UnityTcpClient");
        if (ut == null)
        {
            Debug.LogError("UnityTcpClient object not found!");
            yield break;
        }

        // ��������� ����������
        tcpClient = ut.GetComponent<UnityTcpClient>();
        if (tcpClient == null)
        {
            Debug.LogError("UnityTcpClient component not found!");
            yield break;
        }

        // ������� �����
        indexUnit = tcpClient.IDclient;
        Vector3 randomPos = GetRandomNavMeshPosition();
        Debug.Log(randomPos);

        // ���������� ��'����
        GenerateMessageToServer(randomPos);

        // ������������ ������� ������
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
                Debug.LogWarning("��������� ServerId �� �������� �� ��'���.");
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