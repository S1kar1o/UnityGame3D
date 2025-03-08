using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AmountResource : MonoBehaviour
{
    public int Amount; // Максимальний рівень HP
    public string typeResource;
    public UnityTcpClient utp;
    public void Start()
    {
        try
        {
            GameObject obj = GameObject.Find("UnityTcpClient");
            utp = obj.GetComponent<UnityTcpClient>();
        }
        catch { };
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
                break;
            case "Rock":
                utp.rockAmount += damage;
                break;
            default:
                utp.goldAmount += damage;
                break;
        }

        if (Amount <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} закінчився!");
        Destroy(gameObject); // Видалення об'єкта при загибелі
    }
}
