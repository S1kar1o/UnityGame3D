using UnityEngine;
using Esper.Freeloader; // ������ ������ ���� Freeloader
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public TextMeshProUGUI progressText; // TextMeshPro ��� ����������� �������
    public string sceneToLoad="Playble";          // �����, ��� ����� �����������
    public UnityTcpClient unityTcp;     // TCP �볺�� ��� ��������� �����

    private static float _additionalProgress; // ��� ����������� ��������
    private LoadingProgressTracker _tracker;  // ������ �������� Freeloader

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

        // ����������� ������� ��������
        _tracker = new LoadingProgressTracker(
            "������������...",
            () => _additionalProgress
        );

        // ������� ������������ ����� � ��������
        LoadingScreen.Instance.Load(sceneToLoad, _tracker);

        // ������ ����������� ������������ (�������)
        StartCoroutine(SimulateAdditionalLoading());
    }

    IEnumerator SimulateAdditionalLoading()
    {
        _additionalProgress = 0f;

        // ��������� ������������ ���������� �������
        while (_additionalProgress < 1f)
        {
            _additionalProgress += 0.05f;

            if (progressText != null)
            {
                progressText.text = $"������������: {Mathf.Floor(_additionalProgress * 100)}%";
            }

            yield return new WaitForSeconds(0.1f);
        }

        // ��������� ������ ���� ����������
        if (progressText != null)
        {
            progressText.text = "�������� ����-��� ������...";
        }
    }

    void Update()
    {
        // ��������� ����� ���� ������� ������������
        if (_additionalProgress >= 1f && Input.anyKeyDown)
        {
            // Freeloader ����������� ������ ����� ��� ���������
            // ��� ��� ������� ���� ��� ����������� �������
        }
    }
}