using UnityEngine;
using UnityEngine.UI;

public class ButtonControler : MonoBehaviour
{
    public GameObject PanelResearch;
    public Button ResearchButton;
    public bool researchPanelActive = false;
    public GameObject UnityTcpClient;
    public bool hirePanel=false;

    void Start()
    {
        UnityTcpClient = GameObject.Find("UnityTcpClient");
        PanelResearch.SetActive(false); 
    }
    public  void PanelResearchButton()
    {
        ResearchButton.gameObject.SetActive(researchPanelActive);
        researchPanelActive = !researchPanelActive;
        PanelResearch.SetActive(researchPanelActive);
    }
  
}

