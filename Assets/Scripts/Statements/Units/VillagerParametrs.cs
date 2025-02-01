using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class VillagerParametrs : MonoBehaviour
{
    private NavMeshAgent agent;
    private GameObject targetResource;
    private bool isExtracting = false; // якщо агент видобуваЇ ресурс
    public float extractionInterval = 2.0f; // „ас м≥ж циклами видобуванн€
    public int resourceAmountPerCycle = 5; // —к≥льки ресурсу видобуваЇтьс€ за один цикл
    public int distanceAces = 15;
    private Coroutine extractionCoroutine; // «м≥нна дл€ збереженн€ корутини

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void MoveToResource(GameObject resource)
    {
        if (isExtracting)
        {
            Debug.Log(111);
            StopExtracting();
        }

        targetResource = resource;

        BoxCollider resourceCollider = resource.GetComponent<BoxCollider>();
        BoxCollider agentCollider = GetComponentInChildren<BoxCollider>();

        if (resourceCollider != null && agentCollider != null)
        {
            Vector3 targetPosition = GetClosestPointOnResource(resource, resourceCollider, agentCollider);
            Debug.Log("Adjusted target position near resource: " + targetPosition);

            agent.SetDestination(targetPosition);
            StartCoroutine(CheckArrival());
        }
        else
        {
            if (resourceCollider == null)
            {
                Debug.LogWarning("Resource does not have a BoxCollider component!");
            }
            if (agentCollider == null)
            {
                Debug.LogWarning("Agent does not have a BoxCollider component!");
            }
        }
    }

    private IEnumerator CheckArrival()
    {
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance || !agent.hasPath)
        {
            yield return null;
        }

        if (!isExtracting && targetResource != null)
        {
            extractionCoroutine = StartCoroutine(ExtractResourceCoroutine());
        }
    }

    public Vector3 GetClosestPointOnResource(GameObject resource, BoxCollider resourceCollider, BoxCollider agentCollider)
    {
        Vector3 resourceCenter = resourceCollider.transform.TransformPoint(resourceCollider.center);
        Vector3 resourceHalfSize = Vector3.Scale(resourceCollider.size * 0.5f, resourceCollider.transform.lossyScale);
        Vector3 agentHalfSize = Vector3.Scale(agentCollider.size * 0.5f, agentCollider.transform.lossyScale);
        Vector3 agentPosition = agentCollider.transform.position;

        float clampedX = Mathf.Clamp(agentPosition.x, resourceCenter.x - resourceHalfSize.x, resourceCenter.x + resourceHalfSize.x);
        float clampedY = Mathf.Clamp(agentPosition.y, resourceCenter.y - resourceHalfSize.y, resourceCenter.y + resourceHalfSize.y);
        float clampedZ = Mathf.Clamp(agentPosition.z, resourceCenter.z - resourceHalfSize.z, resourceCenter.z + resourceHalfSize.z);

        Vector3 adjustedPoint = new Vector3(clampedX, clampedY, clampedZ);
        Debug.Log($"Resource Center: {resourceCenter}, Adjusted Point: {adjustedPoint}");

        return adjustedPoint;
    }

    private IEnumerator ExtractResourceCoroutine()
    {
        if (targetResource == null)
        {
            yield break;
        }

        isExtracting = true;
        agent.ResetPath();
        agent.isStopped = true;

        while (targetResource != null)
        {
            AmountResource amrsc = targetResource.GetComponent<AmountResource>();
            if (amrsc == null)
            {
                Debug.LogWarning("Resource does not contain AmountResource component or has been destroyed!");
                StopExtracting();
                yield break;
            }

            yield return new WaitForSeconds(extractionInterval);

            amrsc.Extraction(resourceAmountPerCycle);
            Debug.Log(amrsc.Amount);

            if (amrsc.Amount <= 0)
            {
                targetResource = null;
                StopExtracting();
            }
        }
    }

    public void StopExtracting()
    {
        isExtracting = false;
        agent.isStopped = false;
        agent.ResetPath();

        if (extractionCoroutine != null)
        {
            StopCoroutine(extractionCoroutine);
            extractionCoroutine = null;
        }
    }
}
