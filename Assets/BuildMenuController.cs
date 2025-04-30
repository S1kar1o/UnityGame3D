using UnityEngine;

public class BuildMenuController : MonoBehaviour
{
    public GameObject buildingsPanel; // ������ � �������� �������
    private bool isMenuOpen = false;

    void Start()
    {
        // ������������, �� ������ ��������� �� �����
        if (buildingsPanel != null)
            buildingsPanel.SetActive(false);
    }

    public void ToggleBuildMenu()
    {
        if (buildingsPanel != null)
        {
            isMenuOpen = !isMenuOpen;
            buildingsPanel.SetActive(isMenuOpen);
        }
    }
}
