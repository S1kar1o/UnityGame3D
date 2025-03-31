using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Esper.Freeloader.Examples
{
    public class Demo : MonoBehaviour
    {
        public string sceneToLoad;

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            StartCoroutine(LoadSceneWithProgress());
        }

        private IEnumerator LoadSceneWithProgress()
        {
            UIDocument uiDocument = LoadingScreen.Instance.GetComponent<UIDocument>();
            uiDocument.enabled = true;
            Debug.Log("UI увімкнено");

            if (!LoadingScreen.Instance.IsLoading)
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
                asyncLoad.allowSceneActivation = false; // Затримуємо активацію
                Debug.Log("Запущено завантаження сцени: " + sceneToLoad);

                // Імітація плавного прогресу
                float simulatedProgress = 0f;
                float loadingDuration = 3f; // Тривалість завантаження в секундах
                float timeElapsed = 0f;

                var process = new LoadingProgressTracker("Loading...", () =>
                {
                    return simulatedProgress; // Використовуємо лише симульований прогрес
                });
                LoadingScreen.Instance.Load(sceneToLoad, process);

                while (!asyncLoad.isDone)
                {
                    timeElapsed += Time.deltaTime;
                    simulatedProgress = Mathf.Clamp01(timeElapsed / loadingDuration) * 100f; // Плавний прогрес від 0 до 100
                    float progress = process.Progress;
                    Debug.Log("Прогрес завантаження: " + progress);

                    // Активуємо сцену лише після завершення симуляції
                    if (simulatedProgress >= 100f && asyncLoad.progress >= 0.9f)
                    {
                        asyncLoad.allowSceneActivation = true;
                    }
                    yield return null;
                }

                Debug.Log("Сцена повністю завантажена");
                uiDocument.enabled = false;
                Debug.Log("UI вимкнено");
            }
            else
            {
                Debug.LogWarning("Завантаження вже триває!");
            }
        }
    }
}