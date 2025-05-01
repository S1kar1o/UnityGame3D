using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class ButtonControler : MonoBehaviour
{
    public GameObject PanelResearch, PanelEndGame, PanelBuilding,PanelOfFunction;
    public Button ResearchButton, ButtonEndGame, ButtonBuildingMenu;
    public TMP_Text txt;
    public bool researchPanelActive = false, endGamePanelIsActive = false, buildingMenuIsActive=false;
    public bool hirePanel = false;
    private UnityTcpClient utp;



    public RectTransform panel;       // “во€ панель
    public float animationTime = 0.5f; // Ўвидк≥сть ан≥мац≥њ
    public Vector2 hiddenPos;         //  уди з'њжджаЇ (зазвичай нижче)
    public Vector2 shownPos;          // ƒе панель маЇ бути, коли показана
    public RectTransform buttonTransform;

    private Vector2 originalPosition;

    private bool isVisible = true;
    private Coroutine currentCoroutine;
    void Start()
    {
        utp = UnityTcpClient.Instance;
        utp.buttonControler = this;
        PanelResearch.SetActive(false); PanelBuilding.SetActive(false);
        originalPosition = buttonTransform.anchoredPosition;

    }
    public void PanelResearchButton()
    {
        PanelOfFunction.SetActive(researchPanelActive);
        researchPanelActive = !researchPanelActive;
        PanelResearch.SetActive(researchPanelActive);
    }
    public void PanelBuilldingButton()
    {
        PanelOfFunction.SetActive(buildingMenuIsActive);
        buildingMenuIsActive = !buildingMenuIsActive;
        PanelBuilding.SetActive(buildingMenuIsActive);

    }
    public float liftAmount = 20f;

    private bool isFlipped = false;
    private Coroutine currentAnim;

    public void AnimateButton()
    {
        if (currentAnim != null)
            StopCoroutine(currentAnim);

        if (!isFlipped)
        {
            // ќбертаЇмо на 180 ≥ п≥дн≥маЇмо
            currentAnim = StartCoroutine(RotateAndLift(180, 0, originalPosition, originalPosition + new Vector2(0, liftAmount)));
        }
        else
        {
            // ќбертаЇмо назад ≥ повертаЇмо на початкову позиц≥ю
            currentAnim = StartCoroutine(RotateAndLift(0, 180, buttonTransform.anchoredPosition, originalPosition));
        }

        isFlipped = !isFlipped;
    }

    private IEnumerator RotateAndLift(float fromAngle, float toAngle, Vector2 fromPos, Vector2 toPos)
    {
        float elapsed = 0f;
        Vector3 startRotation = new Vector3(0, 0, fromAngle);
        Vector3 endRotation = new Vector3(0, 0, toAngle);

        while (elapsed < animationTime)
        {
            float t = elapsed / animationTime;
            buttonTransform.localEulerAngles = Vector3.Lerp(startRotation, endRotation, t);
            buttonTransform.anchoredPosition = Vector2.Lerp(fromPos, toPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        buttonTransform.localEulerAngles = endRotation;
        buttonTransform.anchoredPosition = toPos;
    }
    public void TogglePanel()
    {
        AnimateButton();
        // якщо попередн€ ан≥мац≥€ ще йде Ч зупин€Їмо
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        if (isVisible)
            currentCoroutine = StartCoroutine(Slide(panel.anchoredPosition, hiddenPos));
        else
            currentCoroutine = StartCoroutine(Slide(panel.anchoredPosition, shownPos));

        isVisible = !isVisible;
    }

    private IEnumerator Slide(Vector2 from, Vector2 to)
    {
        float elapsed = 0;
        while (elapsed < animationTime)
        {
            panel.anchoredPosition = Vector2.Lerp(from, to, elapsed / animationTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        panel.anchoredPosition = to;
    }
    public void PanelEndGameButton()
    {
        PanelEndGame.SetActive(endGamePanelIsActive);
        if (endGamePanelIsActive)
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

