using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonControler : MonoBehaviour
{
    public GameObject PanelResearch;
    public Button ResearchButton;
    public bool researchPanelActive = false;
    void Start()
    {
        PanelResearch.SetActive(false); 
    }
    public  void PanelResearchButton()
    {
        ResearchButton.gameObject.SetActive(researchPanelActive);
        researchPanelActive = !researchPanelActive;
        PanelResearch.SetActive(researchPanelActive);
        

    }
}
