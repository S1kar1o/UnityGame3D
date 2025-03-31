using UnityEngine;
using UnityEngine.UIElements;

public class BridgePlacer : MonoBehaviour
{
    public GameObject bridgePrefab; // Префаб моста
    private Vector3 firstPoint;
    private Vector3 secondPoint;
    private bool isFirstPointSelected = false;
    private bool isPlacingBridge = false;

    void Update()
    {
        if (!isPlacingBridge) return; // Если режим строительства не включен, ничего не делаем

        if (Input.GetMouseButtonDown(0)) // Левая кнопка мыши
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (!isFirstPointSelected)
                {
                    firstPoint = hit.point;
                    isFirstPointSelected = true;
                    Debug.Log("Первая точка выбрана: " + firstPoint);
                }
                else
                {
                    secondPoint = hit.point;
                    isFirstPointSelected = false;
                    Debug.Log("Вторая точка выбрана: " + secondPoint);
                    PlaceBridge(); // Устанавливаем мост
                }
            }
        }
    }

    public void StartPlacingBridge()
    {
        isPlacingBridge = !isPlacingBridge;
        isFirstPointSelected = false;
        Debug.Log("Режим строительства моста: " + (isPlacingBridge ? "Включен" : "Выключен"));
    }

    private void PlaceBridge()
    {
        // Рассчитываем центр между двумя точками
        Vector3 midPoint = (firstPoint + secondPoint) / 2;

        // Направление от первой точки ко второй
        Vector3 direction = (secondPoint - firstPoint).normalized;

        // Поворот моста для корректного размещения
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, secondPoint - firstPoint);

        // Расстояние между точками
        float distance = Vector3.Distance(firstPoint, secondPoint);

        // Длина префаба (по оси Z)
        float bridgeLength = Mathf.Abs(bridgePrefab.GetComponentInChildren<Renderer>().bounds.size.z);

        // Количество префабов
        int bridgeCount = Mathf.Max(1, Mathf.FloorToInt(distance / bridgeLength) + 1);

        // Выводим все начальные данные в консоль
        Debug.Log("Первоначальные данные:");
        Debug.Log("Точка 1: " + firstPoint);
        Debug.Log("Точка 2: " + secondPoint);
        Debug.Log("Центр (midPoint): " + midPoint);
        Debug.Log("Направление (direction): " + direction);
        Debug.Log("Дистанция (distance): " + distance);
        Debug.Log("Длина префаба (bridgeLength): " + bridgeLength);
        Debug.Log("Количество префабов (bridgeCount): " + bridgeCount);

        float p1 = (bridgeCount * bridgeLength - distance) / 2;
        
        Vector3 position = firstPoint - direction * p1;


        
        Debug.Log("Поправка " + p1);

        for (int i = 0; i < bridgeCount; i++)
        {
           
            Debug.Log("Позиция моста " + i + ": " + position);

            Instantiate(bridgePrefab, position, rotation);
            position += direction * bridgeLength;
        }


       
    }




}
