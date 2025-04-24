using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class LoadingSceneController : MonoBehaviour
{
    [Header("Основні налаштування")]
    [SerializeField] private string sceneToLoad = "MainScene";
    [SerializeField] private Image progressBar;  // Changed to Image
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Image backgroundImage;

    [Header("Налаштування завантаження")]
    [SerializeField] private float minLoadTime = 3f;
    [SerializeField] private float fakeLoadSpeed = 0.5f;
    [SerializeField] private float activationDelay = 0.5f;

    [Header("Анімація фону")]
    [SerializeField] private List<Sprite> backgroundSprites;
    [SerializeField] private float animationSpeed = 0.5f;

    [Header("Підказки")]
    [SerializeField] private GameObject tooltipPrefab;
    [SerializeField] private Transform tooltipContainer;
    [SerializeField] private List<string> tooltipMessages;
    [SerializeField] private float tooltipDelay = 1f;

    private float loadStartTime;
    private float currentProgress = 0f;
    private UnityTcpClient tcpClient;
    private bool readyMessageSent = false;
    private int currentSpriteIndex = 0;
    private float lastTooltipTime;
    private GameObject activeTooltip;
    private bool tooltipIsFading = false;
    private Coroutine tooltipCoroutine;

    private void Start()
    {
        loadStartTime = Time.time;
        lastTooltipTime = Time.time;

        InitializeTcpClient();

        if (backgroundSprites.Count > 0)
        {
            StartCoroutine(AnimateBackground());
        }

        // 👇 Показати першу підказку одразу
        if (tooltipMessages.Count > 0)
        {
            string randomMessage = tooltipMessages[Random.Range(0, tooltipMessages.Count)];
            ShowTooltip(randomMessage);
        }

        StartCoroutine(ShowRandomTooltips());
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator AnimateBackground()
    {
        while (true)
        {
            if (backgroundImage != null && backgroundSprites.Count > 0)
            {
                Sprite nextSprite = backgroundSprites[currentSpriteIndex];
                yield return StartCoroutine(FadeToSprite(nextSprite, 1f));

                currentSpriteIndex = (currentSpriteIndex + 1) % backgroundSprites.Count;
                yield return new WaitForSeconds(animationSpeed);
            }
            else
            {
                yield return null;
            }
        }
    }

    private IEnumerator FadeToSprite(Sprite newSprite, float fadeDuration)
    {
        float time = 0f;
        Color originalColor = backgroundImage.color;

        // Fade out
        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            backgroundImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(1f, 0f, t));
            time += Time.deltaTime;
            yield return null;
        }

        backgroundImage.sprite = newSprite;
        time = 0f;

        // Fade in
        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            backgroundImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(0f, 1f, t));
            time += Time.deltaTime;
            yield return null;
        }

        backgroundImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
    }

    private IEnumerator ShowRandomTooltips()
    {
        while (true)
        {
            if (tooltipMessages.Count > 0 && tooltipPrefab != null && tooltipContainer != null)
            {
                if (Time.time - lastTooltipTime >= tooltipDelay)
                {
                    string randomMessage = tooltipMessages[Random.Range(0, tooltipMessages.Count)];
                    ShowTooltip(randomMessage);
                    lastTooltipTime = Time.time;
                }
            }
            yield return new WaitForSeconds(0.1f); // не перевіряй кожен кадр
        }
    }

    private void ShowTooltip(string message)
    {
        if (tooltipIsFading) return;

        if (activeTooltip == null)
        {
            activeTooltip = Instantiate(tooltipPrefab, tooltipContainer);
        }

        TMP_Text tooltipText = activeTooltip.GetComponentInChildren<TMP_Text>();
        CanvasGroup canvasGroup = activeTooltip.GetComponent<CanvasGroup>();

        if (tooltipText != null)
        {
            tooltipText.text = message;
        }

        if (canvasGroup != null)
        {
            if (tooltipCoroutine != null)
            {
                StopCoroutine(tooltipCoroutine);
            }

            canvasGroup.alpha = 0f;
            activeTooltip.SetActive(true);

            tooltipCoroutine = StartCoroutine(FadeInThenOutTooltip(canvasGroup, 1f, 2f));
        }
    }

    private IEnumerator FadeInThenOutTooltip(CanvasGroup canvasGroup, float fadeInTime, float stayTime)
    {
        tooltipIsFading = true;

        float elapsedTime = 0f;

        // Fade In
        while (elapsedTime < fadeInTime)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Зачекати, поки показана
        yield return new WaitForSeconds(stayTime);

        // Fade Out
        elapsedTime = 0f;
        float fadeOutTime = 1f;
        while (elapsedTime < fadeOutTime)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;
        activeTooltip.SetActive(false);

        tooltipIsFading = false;
    }

    private void InitializeTcpClient()
    {
        tcpClient = UnityTcpClient.Instance;
    }
    private IEnumerator LoadSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            UpdateProgress(asyncLoad);

            // Чекай, поки Unity реально завантажить все до 0.9
            if (asyncLoad.progress >= 0.9f && !readyMessageSent)
            {
                tcpClient.SendMessage("LOADED");
                readyMessageSent = true;
                Debug.Log("Повідомлення LOADED на 90%, чекаємо підтвердження");
            }

            // Чекаємо, поки інший клієнт теж буде готовий
            if (readyMessageSent && tcpClient.enemyReady && asyncLoad.progress >= 0.9f)
            {
                yield return FinalizeSceneActivation(asyncLoad);
                yield break;
            }

            yield return null;
        }
    }


    private void UpdateProgress(AsyncOperation asyncLoad)
    {
        // Реальний прогрес, переведений до діапазону від 0 до 1
        float realProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
        currentProgress = Mathf.MoveTowards(currentProgress, realProgress, fakeLoadSpeed * Time.deltaTime);

        // Оновлення fillAmount для заповнення прогрес-бару
        if (progressBar != null)
        {
            progressBar.fillAmount = currentProgress;
        }

        if (progressText != null)
        {
            progressText.text = $"{currentProgress * 100f:0}%";
        }

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
        UpdateProgressUI(1f);
        asyncLoad.allowSceneActivation = true;
        Debug.Log("Активація сцени — обидва гравці готові");
        yield return null;
    }

    private void UpdateProgressUI(float progress)
    {
        if (progressBar != null) progressBar.fillAmount = progress;
        if (progressText != null) progressText.text = $"{progress * 100f:0}%";
    }
}
