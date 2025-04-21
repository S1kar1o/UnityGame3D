using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class ButtonControler : MonoBehaviour
{
    public GameObject PanelResearch,PanelEndGame;
    public Button ResearchButton,ButtonEndGame;
    public TMP_Text txt;
    public bool researchPanelActive = false, endGamePanelIsActive = false;
    public bool hirePanel=false;
    private UnityTcpClient utp;
    void Start()
    {
        utp = UnityTcpClient.Instance;
        utp.buttonControler = this;
        PanelResearch.SetActive(false); 
    }
    public  void PanelResearchButton()
    {
        ResearchButton.gameObject.SetActive(researchPanelActive);
        researchPanelActive = !researchPanelActive;
        PanelResearch.SetActive(researchPanelActive);
    }
    public void PanelEndGameButton()
    {
        PanelEndGame.SetActive(endGamePanelIsActive);
        if(endGamePanelIsActive)
            utp.SendMessage("WON");

    }
    public void PanelEndGameFromServerButton()
    {
        txt.text = "You lose -25";
        PanelEndGame.SetActive(endGamePanelIsActive);

    }
    public void EndGameButton()
    {
        utp.SendMessage("WON");

        utp.enemyReady = false;
        utp.ReloadRscClient();
        SceneManager.LoadScene("SampleScene");
    }
}

