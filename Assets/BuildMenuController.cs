using UnityEngine;

public class BuildMenuController : MonoBehaviour
{
    public GameObject buildingsPanel; // Панель з кнопками будівель
    private bool isMenuOpen = false;

    void Start()
    {
        // Переконуємось, що панель прихована на старті
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
