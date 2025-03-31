using UnityEngine;
using UnityEngine.AI;

public class ArrowController : MonoBehaviour
{
    public Transform target;
    public float speed = 500f;
    public float rotationSpeed = 720f;
    public float zRotationSpeed = 20f;
    public Collider boxCollider;

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
                    return;
                }
            }

            transform.position = newPosition;
            transform.Rotate(rotationSpeed * Time.fixedDeltaTime, 0f, 0f, Space.Self);
            transform.Rotate(0f, 0f, zRotationSpeed * Time.fixedDeltaTime, Space.Self);

            if (Vector3.Distance(transform.position, targetPoint) < 0.5f)
            {
                StickToTarget(targetPoint, target);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit || traveledDistance <= ignoreDistance) return;
        hasHit = true;
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

        Destroy(gameObject, 3f);
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
