using UnityEngine;
using Esper.Freeloader; // Додано простір імен Freeloader
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public TextMeshProUGUI progressText; // TextMeshPro для відображення статусу
    public string sceneToLoad="Playble";          // Сцена, яку треба завантажити
    public UnityTcpClient unityTcp;     // TCP клієнт для отримання сцени

    private static float _additionalProgress; // Для додаткового прогресу
    private LoadingProgressTracker _tracker;  // Трекер прогресу Freeloader

    void Start()
    {
        unityTcp = FindObjectOfType<UnityTcpClient>();
        sceneToLoad = unityTcp.SceneToMove;
        Debug.Log("Scene to load: " + sceneToLoad);

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Scene to load is empty or null");
            return;
        }

        // Ініціалізація трекера прогресу
        _tracker = new LoadingProgressTracker(
            "Завантаження...",
            () => _additionalProgress
        );

        // Початок завантаження сцени з трекером
        LoadingScreen.Instance.Load(sceneToLoad, _tracker);

        // Запуск додаткового завантаження (приклад)
        StartCoroutine(SimulateAdditionalLoading());
    }

    IEnumerator SimulateAdditionalLoading()
    {
        _additionalProgress = 0f;

        // Симуляція завантаження додаткових ресурсів
        while (_additionalProgress < 1f)
        {
            _additionalProgress += 0.05f;

            if (progressText != null)
            {
                progressText.text = $"Завантаження: {Mathf.Floor(_additionalProgress * 100)}%";
            }

            yield return new WaitForSeconds(0.1f);
        }

        // Оновлення тексту після завершення
        if (progressText != null)
        {
            progressText.text = "Натисніть будь-яку клавішу...";
        }
    }

    void Update()
    {
        // Активація сцени після повного завантаження
        if (_additionalProgress >= 1f && Input.anyKeyDown)
        {
            // Freeloader автоматично активує сцену при завершенні
            // Цей код потрібен лише для відображення підказки
        }
    }
}