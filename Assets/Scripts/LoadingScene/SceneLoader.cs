using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // ��� TextMeshPro
using UnityEngine.UI; // ��� Slider
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public Slider loadingSlider;         // UI Slider ��� ������ �������� ������������
    public TextMeshProUGUI progressText; // TextMeshPro ��� ����������� �������
    public string sceneToLoad;           // �����, ��� ����� �����������
    public UnityTcpClient unityTcp;      // TCP �볺�� ��� ��������� �����

    void Start()
    {
        unityTcp = FindObjectOfType<UnityTcpClient>();   // ��������� ������ UnityTcpClient
        sceneToLoad = unityTcp.SceneToMove;              // �������� ����� ��� ������������
        Debug.Log("Scene to load: " + sceneToLoad);      // �������� ����, ��� ����� �� ��������

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Scene to load is empty or null");
            return;
        }

        StartCoroutine(LoadAsync(sceneToLoad));          // �������� ������������ �����
    }

    private IEnumerator LoadAsync(string sceneName)
    {
        // ����� ������������ ����� � ���������� ��������
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError("Failed to load scene: " + sceneName);
            yield break; // ������������ ����� �� ��������, �������� � ��������
        }

        operation.allowSceneActivation = false;

        // �������, ���� ����� �������������
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f); // ������������ ��������

            if (loadingSlider != null && progressText != null)
            {
                loadingSlider.value = progress;
                progressText.text = Mathf.FloorToInt(progress * 100) + "%";
            }

            // ���� ������� ����� ��� ������� 0.9, ��������� ��������� �����
            if (operation.progress >= 0.9f)
            {
                if (progressText.text != "Press any key to continue...")
                {
                    progressText.text = "Press any key to continue...";
                }

                if (Input.anyKeyDown)
                {
                    operation.allowSceneActivation = true;  // ���� ���������
                    Debug.Log("Scene activated: " + sceneName); // ���, �� ����� ������������
                }
            }

            yield return null;
        }
    }
}
