using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMoving : MonoBehaviour
{
    public float rotateSpeed = 20.0f, speed = 200f, zoomSpeed = 10000f;
    private float _mult = 1f;
    public bool isLocked = false;
    public List<GameObject> enemys = new List<GameObject>();
    private UnityTcpClient utp;

    private bool waitingStarted = false; // Флаг, щоб уникнути багаторазового запуску корутини
    public bool waiting = false;
    private void Start()
    {
        utp = UnityTcpClient.Instance;
        utp.cameraMoving = this;
        StartCoroutine(WaitingForSpawn()); // Запускаємо затримку лише раз
    }

    private void Update()
    {
        /*if (!waitingStarted) return; // Чекаємо, поки не завершиться затримка
        if (!waiting)
            isLocked = checkEnemy();
        else
            isLocked = true;
        if (!isLocked)*/
        {
            float rotate = 0.0f;

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (Input.GetKey(KeyCode.Q))
            {
                rotate = -1f;
            }
            else if (Input.GetKey(KeyCode.E))
            {
                rotate = 1f;
            }

            _mult = Input.GetKey(KeyCode.LeftShift) ? 2f : 1f;

            transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime * rotate * _mult, Space.Self);
            transform.Translate(new Vector3(horizontal, 0, vertical) * Time.deltaTime * _mult * speed, Space.Self);

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            Vector3 zoomMovement = new Vector3(0, -scroll * zoomSpeed * Time.deltaTime, 0);
            transform.position += zoomMovement;

            transform.position = new Vector3(
                transform.position.x,
                Mathf.Clamp(transform.position.y, 100f, 1000f),
                transform.position.z
            );
        }
    }

    private IEnumerator WaitingForSpawn()
    {
        yield return new WaitForSeconds(2f);
        waitingStarted = true; // Вмикаємо логіку лише після 2 секунд
    }

    private bool checkEnemy()
    {
        if (!utp.buttonControler.endGamePanelIsActive)
        {
            if (enemys.Count <= 0)
            {
                utp.SendMessage("WON");
                utp.buttonControler.endGamePanelIsActive = true;
                utp.buttonControler.PanelEndGameButton();
                return true;
            }
            utp.buttonControler.endGamePanelIsActive = false;
            utp.buttonControler.PanelEndGameButton();
            return false;
        }
        return true;
    }
}
