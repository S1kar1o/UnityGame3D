using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class LoginingToDB : MonoBehaviour
{
    [SerializeField] private TMP_InputField emailField, passwordField, nickNameField;
    [SerializeField] private TMP_Text errorText, errorDuringApplyMessage;
    [SerializeField] private Button finishInputButton, loginingButton, registrationButton, returnButton, applyButton, closeGame;
    private bool logOrCreate = false;
    [SerializeField] private float errorDisplayTime = 5f;
    public Coroutine currentAnimationCoroutine;


    [SerializeField] private RectTransform rectComponent;
    private float rotateSpeed = 400f;
    public Image bacgroundDurringLoading;
    private void Start()
    {
        UnityTcpClient.Instance.loginController = this;
        SetFieldsVisibility(false, false);
    }

    public void SetFieldsVisibility(bool showFields, bool showNick)
    {
        logOrCreate = (showFields == showNick); // Якщо однакові значення → режим реєстрації

        emailField.gameObject.SetActive(showFields);
        passwordField.gameObject.SetActive(showFields);
        nickNameField.gameObject.SetActive(showNick);
        finishInputButton.gameObject.SetActive(showFields);
        returnButton.gameObject.SetActive(showFields);
        loginingButton.gameObject.SetActive(!showFields);
        registrationButton.gameObject.SetActive(!showFields);
        closeGame.gameObject.SetActive(!showFields);
    }

    public void CloseGame()
    {
        Debug.Log("Game is exiting...");
        Application.Quit();

#if UNITY_EDITOR
        // Якщо ви запускаєте гру в редакторі Unity, закриття гри не працюватиме, тому ми зупинимо редактор.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void registrationMethod()
    {
        SetFieldsVisibility(true, true);
    }

    public void enterToAccount()
    {
        SetFieldsVisibility(true, false);
    }
    public void returnButtonFunction()
    {
        SetFieldsVisibility(!true, false);
    }
    public void configuationComplete()
    {
        if (logOrCreate)
            buttonRegistrationPressed();
        else
            buttonEnterToAccountPressed();

    }
    public void ProblemAcceptGmail()
    {
        errorDuringApplyMessage.gameObject.SetActive(true);
        errorDuringApplyMessage.color = Color.red;
    }
    private bool AreFieldsValid(bool checkNick)
    {
        if (string.IsNullOrEmpty(emailField.text) || string.IsNullOrEmpty(passwordField.text))
            return false;

        if (checkNick && string.IsNullOrEmpty(nickNameField.text))
            return false;

        return true;
    }

    public async void buttonEnterToAccountPressed()
    {
        if (AreFieldsValid(false))
        {

            await UnityTcpClient.Instance.SendMessage($"LOGINBYEMAIL {emailField.text} {passwordField.text}");
            currentAnimationCoroutine = StartCoroutine(waitingAnimation());
        }
    }

    public IEnumerator waitingAnimation()
    {
        bacgroundDurringLoading.gameObject.SetActive(true);
        rectComponent.gameObject.SetActive(true);
        while (true)
        {
            if (rectComponent != null)
                rectComponent.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime);
            else
                yield break; // закінчити корутину, якщо компонент видалено

            yield return null;
        }
    }

    public void stopWaitingAnimation()
    {
        bacgroundDurringLoading.gameObject.SetActive(false);
        rectComponent.gameObject.SetActive(false);
        if (currentAnimationCoroutine != null)
            StopCoroutine(currentAnimationCoroutine);
    }
    public void toApplyMenu()
    {
        emailField.gameObject.SetActive(false);
        passwordField.gameObject.SetActive(false);
        nickNameField.gameObject.SetActive(false);
        finishInputButton.gameObject.SetActive(false);
        returnButton.gameObject.SetActive(false);
        loginingButton.gameObject.SetActive(false);
        registrationButton.gameObject.SetActive(false);

        errorDuringApplyMessage.gameObject.SetActive(true);

        applyButton.gameObject.SetActive(true);
    }
    public void problemDuringLogin(string exeptionDiscription)
    {
        errorText.text = exeptionDiscription;
    }

    private readonly Dictionary<string, string> errorMessages = new Dictionary<string, string>
    {
        {"Invalid format", "Невірний формат запиту"},
        {"Email exists", "Користувач з таким email вже існує"},
        {"Weak password", "Пароль занадто простий"},
        {"Invalid email", "Невірний формат email"},
        {"Database error", "Помилка бази даних, спробуйте пізніше"}
    };

    public void ShowRegistrationError(string errorMessage)
    {
        // Отримуємо локалізоване повідомлення або використовуємо оригінал
        string displayMessage = errorMessages.TryGetValue(errorMessage, out string localized) ?
            localized : errorMessage;

        errorText.text = displayMessage;
        errorText.gameObject.SetActive(true);

        // Автоматичне приховування помилки через вказаний час
        CancelInvoke(nameof(HideError));
        Invoke(nameof(HideError), errorDisplayTime);
    }
    private void HideError()
    {
        errorText.gameObject.SetActive(false);
    }

    public void ClearErrors()
    {
        errorText.text = "";
        errorText.gameObject.SetActive(false);
        CancelInvoke(nameof(HideError));
    }
    private async void buttonRegistrationPressed()
    {
        if (AreFieldsValid(true))
        {
            await UnityTcpClient.Instance.SendMessage($"REGISTRATE {nickNameField.text} {emailField.text} {passwordField.text}");
            currentAnimationCoroutine= StartCoroutine(waitingAnimation());
            errorText.gameObject.SetActive(true);
        }
    }
}
