using UnityEngine;
using Unity.AI.Navigation; // Якщо використовуєте AI Navigation Package

public class BridgeNavMeshUpdater : MonoBehaviour
{
    public NavMeshSurface groundSurface;  // Посилання на NavMesh поверхню землі
    public NavMeshSurface bridgeSurface;  // Посилання на NavMesh поверхню мосту

    void Start()
    {
        // Додаємо міст в навігацію
        UpdateNavMesh();
    }

    public void UpdateNavMesh()
    {
        // Спочатку оновлюємо NavMesh для мосту
        bridgeSurface.BuildNavMesh();

        // Тепер оновлюємо основну поверхню землі, включаючи міст
        groundSurface.BuildNavMesh();
    }
}
