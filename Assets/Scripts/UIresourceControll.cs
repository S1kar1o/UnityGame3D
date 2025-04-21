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
        utp = UnityTcpClient.Instance;
        
        utp.uIresource=this;
        AmoundOfGold.text = utp.goldAmount.ToString();
        AmoundOfWood.text = utp.woodAmount.ToString();
        AmoundOfRocks.text = utp.rockAmount.ToString();
    }
    public void UpdateAmoundOfResource()
    {
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
