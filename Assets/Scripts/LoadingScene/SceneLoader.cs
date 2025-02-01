using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Для TextMeshPro
using UnityEngine.UI; // Для Slider
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public Slider loadingSlider;         // UI Slider для показу прогресу завантаження
    public TextMeshProUGUI progressText; // TextMeshPro для відображення відсотків
    public string sceneToLoad;           // Сцена, яку треба завантажити
    public UnityTcpClient unityTcp;      // TCP клієнт для отримання сцени

    void Start()
    {
        unityTcp = FindObjectOfType<UnityTcpClient>();   // Знаходимо скрипт UnityTcpClient
        sceneToLoad = unityTcp.SceneToMove;              // отримуємо сцену для завантаження
        Debug.Log("Scene to load: " + sceneToLoad);      // Перевірка того, яку сцену ви передаєте

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Scene to load is empty or null");
            return;
        }

        StartCoroutine(LoadAsync(sceneToLoad));          // Починаємо завантаження сцени
    }

    private IEnumerator LoadAsync(string sceneName)
    {
        // Логіка завантаження сцени з початковим статусом
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError("Failed to load scene: " + sceneName);
            yield break; // Завантаження сцени не відбулося, виходимо з корутини
        }

        operation.allowSceneActivation = false;

        // Очікуємо, поки сцена завантажиться
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f); // Завантаження прогресу

            if (loadingSlider != null && progressText != null)
            {
                loadingSlider.value = progress;
                progressText.text = Mathf.FloorToInt(progress * 100) + "%";
            }

            // Якщо прогрес більше або дорівнює 0.9, дозволити активацію сцени
            if (operation.progress >= 0.9f)
            {
                if (progressText.text != "Press any key to continue...")
                {
                    progressText.text = "Press any key to continue...";
                }

                if (Input.anyKeyDown)
                {
                    operation.allowSceneActivation = true;  // Зміна активації
                    Debug.Log("Scene activated: " + sceneName); // Лог, що сцена активувалася
                }
            }

            yield return null;
        }
    }
}
