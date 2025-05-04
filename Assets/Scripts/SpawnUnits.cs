using UnityEngine;
using System.Threading.Tasks;
using System.Net.Sockets;
using System;
using TMPro;
using System.Collections;

public class SpawnUnits : MonoBehaviour
{
    [SerializeField] private GameObject[] prefabUnit;
    [SerializeField] private int indexUnit = 0;
    [SerializeField] private UnityTcpClient tcpClient;
    public int priceOfGold, priceOfWood, priceOfRock;
    public TextMeshProUGUI textCostOfTree, textCostOfRock, textCostOfGold;
    private Coroutine resourceCheckCoroutine;
    private MovingObjects movingObjects;
    void Start()
    {
        GameObject ut = GameObject.Find("UnityTcpClient");
        tcpClient = ut.GetComponent<UnityTcpClient>();
        movingObjects = FindObjectOfType<MovingObjects>();
        indexUnit = tcpClient.IDclient;
    }

    private void OnDisable()
    {
        if (resourceCheckCoroutine != null)
            StopCoroutine(resourceCheckCoroutine);
    }
    public void startCorutineUpdatePrice()
    {
        textCostOfGold.text = priceOfGold.ToString();
        textCostOfTree.text = priceOfWood.ToString();
        textCostOfRock.text = priceOfRock.ToString();
        if (resourceCheckCoroutine == null)
            resourceCheckCoroutine = StartCoroutine(UpdateResourceColorsRoutine());
    }
    private IEnumerator UpdateResourceColorsRoutine()
    {
        while (true)
        {
            if (gameObject == movingObjects.spawnBuilding && movingObjects.spawnBuilding != null)
            {
                UpdateResourceColors();
                yield return new WaitForSeconds(0.3f);
            }
            else
            {
                resourceCheckCoroutine = null;
                yield break; 
            }
        }
    }

    private void UpdateResourceColors()
    {
        UpdateTextColor(textCostOfGold, UnityTcpClient.Instance.goldAmount, priceOfGold);
        UpdateTextColor(textCostOfRock, UnityTcpClient.Instance.rockAmount, priceOfRock);
        UpdateTextColor(textCostOfTree, UnityTcpClient.Instance.woodAmount, priceOfWood);
    }

    private void UpdateTextColor(TMP_Text text, int currentAmount, int cost)
    {
        text.color = currentAmount >= cost ? Color.green : Color.red;
    }
    public async void SpawnUnit()
    {
        if (UnityTcpClient.Instance.goldAmount >= priceOfGold && UnityTcpClient.Instance.woodAmount >= priceOfWood && UnityTcpClient.Instance.rockAmount >= priceOfRock)
        {
            try
            {
                UnityTcpClient.Instance.goldAmount -= priceOfGold;
                UnityTcpClient.Instance.woodAmount -= priceOfWood;
                UnityTcpClient.Instance.rockAmount -= priceOfRock;

                string name = prefabUnit[indexUnit].name.Replace("(Clone)", "").Trim();

                Vector3 position = new Vector3(
                    transform.GetChild(1).position.x + UnityEngine.Random.Range(1f, 5f),
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