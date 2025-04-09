using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "MainScene";
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText;

    [Header("Налаштування завантаження")]
    [SerializeField] private float minLoadTime = 3f;
    [SerializeField] private float fakeLoadSpeed = 0.5f;
    [SerializeField] private float activationDelay = 0.5f;

    private float loadStartTime;
    private float currentProgress = 0f;
    private UnityTcpClient tcpClient;
    private bool readyMessageSent = false;

    private void Start()
    {
        loadStartTime = Time.time;
        InitializeTcpClient();
        StartCoroutine(LoadSceneAsync());
    }

    private void InitializeTcpClient()
    {
        GameObject obj = GameObject.Find("UnityTcpClient");
        if (obj != null)
        {
            tcpClient = obj.GetComponent<UnityTcpClient>();
        }
        else
        {
            Debug.LogWarning("UnityTcpClient не знайдено - режим офлайн");
        }
    }

    private IEnumerator LoadSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            UpdateProgress(asyncLoad);
            yield return null;

            if (ShouldActivateScene(asyncLoad))
            {
                yield return FinalizeSceneActivation(asyncLoad);
                yield break;
            }
        }
    }

    private void UpdateProgress(AsyncOperation asyncLoad)
    {
        float realProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
        currentProgress = Mathf.MoveTowards(currentProgress, realProgress,
                          fakeLoadSpeed * Time.deltaTime);

        if (progressBar != null) progressBar.value = currentProgress;
        if (progressText != null) progressText.text = $"{currentProgress * 100f:0}%";

        SendReadyMessage(realProgress);
    }

    private void SendReadyMessage(float realProgress)
    {
        if (realProgress >= 0.9f && !readyMessageSent && tcpClient != null)
        {
            tcpClient.SendMessage("LOADED");
            readyMessageSent = true;
            Debug.Log("Відправлено повідомлення про готовність");
        }
    }

    private bool ShouldActivateScene(AsyncOperation asyncLoad)
    {
        bool minTimePassed = Time.time - loadStartTime >= minLoadTime;
        bool progressComplete = currentProgress >= 0.99f && asyncLoad.progress >= 0.9f;
        bool networkReady = tcpClient == null || tcpClient.enemyReady;

        return minTimePassed && progressComplete && networkReady;
    }

    private IEnumerator FinalizeSceneActivation(AsyncOperation asyncLoad)
    {
        // Фінальні підготовки перед активацією
        UpdateProgressUI(1f);
        yield return new WaitForSeconds(activationDelay);

        asyncLoad.allowSceneActivation = true;
        Debug.Log("Активація сцени: всі умови виконані");
    }

    private void UpdateProgressUI(float progress)
    {
        if (progressBar != null) progressBar.value = progress;
        if (progressText != null) progressText.text = $"{progress * 100f:0}%";
    }
}