using System;
using System.Threading.Tasks;
using UnityEngine;

public class AmountResource : MonoBehaviour
{
    public int Amount; 
    public string typeResource;
    public UnityTcpClient utp;
    private int instanceID;

    public void Start()
    {
        instanceID = gameObject.GetInstanceID();

        try
        {
            GameObject obj = GameObject.Find("UnityTcpClient");
            if (obj != null)
            {
                utp = obj.GetComponent<UnityTcpClient>();
            }
            else
            {
                Debug.LogError("UnityTcpClient not found in the scene!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Помилка в Start: {ex.Message}");
        }
    }

    public void Extraction(int damage)
    {
        if (damage > Amount)
        {
            damage = Amount;
        }
        Amount -= damage;

        switch (typeResource)
        {
            case "Wood":
                utp.woodAmount += damage;
                utp.uIresource.UpdateAmoundOWood();
                break;
            case "Rock":
                utp.rockAmount += damage;
                utp.uIresource.UpdateAmoundOfRocks();
                break;
            default:
                utp.goldAmount += damage;
                utp.uIresource.UpdateAmoundOfGold();
                break;
        }

        if (Amount <= 0)
        {
            Die();
        }
        else
        {
            SendMessageAmoutExtraction(damage).ConfigureAwait(false);
        }
    }

    private async Task SendMessageAmoutExtraction(int amount)
    {
        string message = $"EXTRACTED {instanceID} {amount} \n";

        if (utp != null)
        {
            Debug.Log(message);
            try
            {
                await utp.SendMessage(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"Помилка при відправці TCP повідомлення: {e.Message}");
                // Можливо, тут варто повторити спробу або повідомити про помилку.
            }
        }
        else
        {
            Debug.LogError("UnityTcpClient not initialized!");
        }
    }

    private async Task Die()
    {
        string message = $"DIE {instanceID} \n";

        if (utp != null)
        {
            Debug.Log(message);
            try
            {
                await utp.SendMessage(message);
                Destroy(gameObject); // Видалення об'єкта при загибелі
            }
            catch (Exception e)
            {
                Debug.LogError($"Помилка при відправці TCP повідомлення: {e.Message}");
                // Можливо, тут варто повторити спробу або повідомити про помилку.
            }
        }
        else
        {
            Debug.LogError("UnityTcpClient not initialized!");
        }
    }
}