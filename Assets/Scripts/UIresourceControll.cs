using TMPro;
using UnityEngine;
public class UIresourceControll : MonoBehaviour
{
    public TextMeshProUGUI AmoundOfGold;
    public TextMeshProUGUI AmoundOfWood;
    public TextMeshProUGUI AmoundOfRocks;
     
    private UnityTcpClient utp;
    void Start()
    {
        GameObject obj = GameObject.Find("UnityTcpClient");
        utp = obj.GetComponent<UnityTcpClient>();
        AmoundOfGold.text = utp.goldAmount.ToString();
        AmoundOfWood.text = utp.woodAmount.ToString();
        AmoundOfRocks.text = utp.rockAmount.ToString();
    }
    public void UpdateAmoundOfGold()
    {
        AmoundOfGold.text = utp.goldAmount.ToString();
    }
    public void UpdateAmoundOWood()
    {
        AmoundOfWood.text = utp.woodAmount.ToString();
    }
    public void UpdateAmoundOfRocks()
    {
        AmoundOfRocks.text = utp.rockAmount.ToString();
    }
}
