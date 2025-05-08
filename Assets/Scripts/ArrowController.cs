using UnityEngine;
using UnityEngine.AI;

public class ArrowController : MonoBehaviour
{
    public Transform target;
    public float speed = 500f;
    public float rotationSpeed = 720f;
    public float zRotationSpeed = 20f;
    public Collider boxCollider;
    public float damage = 30;
    private Vector3 direction;
    private bool hasHit = false, isEnterEndPoint = false;
    private Vector3 targetPoint;
    private Rigidbody rb;
    private float traveledDistance = 0f;
    private float ignoreDistance = 100f; // Ігноруємо перші 100 одиниць

    void Start()
    {

        if (target != null)
        {
            rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Піднімаємо стартову позицію
            Vector3 startPos = transform.position;
            startPos.y += 100;
            transform.position = startPos;

            targetPoint = PredictFuturePosition();
            direction = (targetPoint - transform.position).normalized;

            // Обертання до цілі
            Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 90, 0);
            transform.rotation = targetRotation;
        }
    }

    void FixedUpdate()
    {
        if (!isEnterEndPoint)
        {
            Vector3 newPosition = transform.position + direction * speed * Time.fixedDeltaTime;
            traveledDistance += Vector3.Distance(transform.position, newPosition);

            if (traveledDistance > ignoreDistance)
            {
                Vector3 raycastStartPos = transform.position + direction * 0.2f; // Зміщуємо точку трохи вперед
                float distanceToCheck = Vector3.Distance(transform.position, newPosition);

                Debug.DrawRay(raycastStartPos, direction * distanceToCheck, Color.red, 0.1f);

                if (Physics.SphereCast(raycastStartPos, 0.1f, direction, out RaycastHit hit, distanceToCheck))
                {
                    Debug.Log("Arrow hit: " + hit.collider.gameObject.name);
                    StickToTarget(hit.point, hit.transform);
                    giveDamage(target);

                    return;
                }
            }

            transform.position = newPosition;
            transform.Rotate(rotationSpeed * Time.fixedDeltaTime, 0f, 0f, Space.Self);
            transform.Rotate(0f, 0f, zRotationSpeed * Time.fixedDeltaTime, Space.Self);

            if (Vector3.Distance(transform.position, targetPoint) < 0.5f)
            {
                StickToTarget(targetPoint, target);
                giveDamage(target);
            }
        }
    }
    private void giveDamage(Transform obj)
    {
        GameObject rootObject = obj.root.gameObject;

        VillagerParametrs target = rootObject.GetComponent<VillagerParametrs>();
        BuildingStats building = rootObject.GetComponent<BuildingStats>();
        RiderParametrs rider = rootObject.GetComponent<RiderParametrs>();

        if (target != null)
        {
            Debug.Log("Hit villager");
            target.getDamage(damage);
        }
        else if (building != null)
        {
            Debug.Log("Hit building");
            building.getDamage(damage);
        }
        else if (rider != null)
        {
            Debug.Log("Hit rider");
            rider.getDamage(damage);
        }
        else
        {
            Debug.Log("No valid damage target on: " + rootObject.name);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(120);

        if (hasHit || traveledDistance <= ignoreDistance) return;

        hasHit = true;
        // Отримуємо головний об’єкт, якщо компонент на дочірньому
        GameObject rootObject = collision.collider.attachedRigidbody != null
            ? collision.collider.attachedRigidbody.gameObject
            : collision.collider.transform.root.gameObject;

        // Перевірка на параметри об'єктів
        VillagerParametrs target = rootObject.GetComponent<VillagerParametrs>();
        BuildingStats building = rootObject.GetComponent<BuildingStats>();
        RiderParametrs rider = rootObject.GetComponent<RiderParametrs>();

        if (target != null)
        {
            Debug.Log(120);
            target.getDamage(damage);
        }
        else if (building != null)
        {
            building.getDamage(damage);
        }
        else if (rider != null)
        {
            rider.getDamage(damage);
        }
        Debug.Log(120);

        // "Прилипання" до місця зіткнення
        StickToTarget(collision.contacts[0].point, collision.transform);

      
    }


    private void StickToTarget(Vector3 hitPoint, Transform hitObject)
    {
        isEnterEndPoint = true;

        // Коригуємо позицію стріли для точного попадання
        transform.position = hitPoint - direction * 0.1f; // Невеликий зсув назад

        transform.SetParent(hitObject);

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Destroy(gameObject, 2f);
        Destroy(this);
    }

    private Vector3 PredictFuturePosition()
    {
        if (!target.TryGetComponent(out NavMeshAgent agent))
        {
            return AdjustPosition(target.position);
        }

        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(agent.destination, path);

        if (path.corners.Length < 2)
        {
            return AdjustPosition(agent.destination);
        }

        Vector3 predictedPosition = target.position;
        float timeToTarget = (target.position - transform.position).magnitude / speed;
        float accumulatedDistance = 0f;

        for (int i = 1; i < path.corners.Length; i++)
        {
            Vector3 start = path.corners[i - 1];
            Vector3 end = path.corners[i];
            float segmentLength = Vector3.Distance(start, end);

            if (accumulatedDistance + segmentLength >= timeToTarget * agent.speed)
            {
                float remainingDistance = timeToTarget * agent.speed - accumulatedDistance;
                predictedPosition = Vector3.Lerp(start, end, remainingDistance / segmentLength);
                break;
            }

            accumulatedDistance += segmentLength;
        }

        return AdjustPosition(predictedPosition);
    }

    private Vector3 AdjustPosition(Vector3 position)
    {
        float heightOffset = 0.0f;

        if (target.TryGetComponent(out Collider col))
        {
            heightOffset = col.bounds.extents.y;
        }
        else if (target.TryGetComponent(out NavMeshAgent agent))
        {
            heightOffset = agent.height / 2;
        }

        position.y += heightOffset + 3;
        return position;
    }
}
