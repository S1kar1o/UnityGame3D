using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class LoginingToDB : MonoBehaviour
{
    [SerializeField] private TMP_InputField emailField, passwordField, nickNameField;
    [SerializeField] private UnityTcpClient tcpClient;
    [SerializeField] private Button finishInputButton,loginingButton,registrationButton,returnButton;
    private bool logOrCreate = false;

    private void Start()
    {
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
        Debug.Log(logOrCreate);
        if (logOrCreate)
            buttonRegistrationPressed();
        else
        buttonEnterToAccountPressed();

    }

    private bool AreFieldsValid(bool checkNick)
    {
        if (string.IsNullOrEmpty(emailField.text) || string.IsNullOrEmpty(passwordField.text))
            return false;

        if (checkNick && string.IsNullOrEmpty(nickNameField.text))
            return false;

        return true;
    }

    private async void buttonEnterToAccountPressed()
    {
        if (AreFieldsValid(false))
        {
            await tcpClient.SendMessage($"LOGINBYEMAIL {emailField.text} {passwordField.text}");
        }
    }



    private async void buttonRegistrationPressed()
    {
        if (AreFieldsValid(true))
        {
            await tcpClient.SendMessage($"REGISTRATE {nickNameField.text} {emailField.text} {passwordField.text}");
        }
    }
}
