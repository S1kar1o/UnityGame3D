using UnityEngine;
using TMPro;

public class FpsCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float deltaTime;

    void Start()
    {
        QualitySettings.vSyncCount = 0; // ������ VSync
        Application.targetFrameRate = -1; // ������� ��������� FPS
    }

    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;

        {
            float fps = 1.0f / deltaTime;
            fpsText.text = $"FPS: {Mathf.Ceil(fps)}";
        }
    }
}
