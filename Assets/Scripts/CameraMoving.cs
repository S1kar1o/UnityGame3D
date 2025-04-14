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
    private void Start()
    {
        utp = UnityTcpClient.Instance;
    }
    private void Update()
    {
        isLocked=checkEnemy();
        if (!isLocked)
        {
            float rotate = 0.0f;

            // Отримуємо введення для руху та повороту
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

            // Множник швидкості (Shift для прискорення)
            _mult = Input.GetKey(KeyCode.LeftShift) ? 2f : 1f;

            // Поворот (на основі локальної осі)
            transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime * rotate * _mult, Space.Self);

            // Рух вперед-назад, вліво-вправо
            transform.Translate(new Vector3(horizontal, 0, vertical) * Time.deltaTime * _mult * speed, Space.Self);

            // Масштабування (зум) тільки по осі Y
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            Vector3 zoomMovement = new Vector3(0, -scroll * zoomSpeed * Time.deltaTime, 0);
            transform.position += zoomMovement;

            // Обмеження зуму
            transform.position = new Vector3(
                transform.position.x,
                Mathf.Clamp(transform.position.y, 100f, 1000f), // Мінімальна та максимальна висота
                transform.position.z
            );
        }
    }
    private bool checkEnemy()
    {
        if(enemys.Count <= 0)
        {
            utp.buttonControler.endGamePanelIsActive = true;
            utp.buttonControler.PanelEndGameButton();
            return true;
        }
        utp.buttonControler.endGamePanelIsActive = false;
        utp.buttonControler.PanelEndGameButton();
        return false;
    }
}
