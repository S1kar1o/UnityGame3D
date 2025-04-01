using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
using UnityEngine.UI;
public class VillagerParametrs : MonoBehaviour
{
    [SerializeField] public Gradient hpBarCollor;
    public Image hpBarBackground;
    public Image hpBarForeground;
    protected float smoothSpeed = 0.5f; // Швидкість анімації втрати HP


    protected Rigidbody rb;
    protected NavMeshAgent agent;
    public BoxCollider boxCollider;
    private GameObject targetResource;
    private bool isExtracting = false; // Якщо агент видобуває ресурс
    private float extractionInterval = 2.0f; // Час між циклами видобування
    private int resourceAmountPerCycle = 5; // Скільки ресурсу видобувається за один цикл

    protected float maxHP = 100f;
    protected float hp = 100;
    private Coroutine extractionCoroutine; // Змінна для збереження корутини
    protected bool isUpped=false,wasStandingInWater=false,isStandingInWater=false,inWater = false, isStanding = true,isDrow=false,isSwimming = false,
        isRunning = false, isRunningToResource = false, isDie = false;
    protected BoxCollider colliderForActionsWithWater;
    private bool isGathering = false;
    protected float fallMultiplier = 2.5f;

    protected float depthBefore = 80f; // Глибина, на якій об'єкт починає витісняти воду
    protected float displaycmentAmount = 6f; // Сила виштовхування
    protected Transform buoyancyPoint; // Точка, яка визначає рівень занурення
    [SerializeField] private GameObject Axe, Pickaxe;


    public UnityTcpClient utp;

    public Vector3 targetPosition;
    void Start()
    {

        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        Transform child = transform.Find("ColliderForWater");
        hpBarForeground.color = hpBarCollor.Evaluate(hp / 100);

        agent.autoTraverseOffMeshLink = false; // Вимикаємо автоматичну телепортацію
        StartCoroutine(CheckForNavMeshLink());
        if (child != null)
            colliderForActionsWithWater = child.GetComponent<BoxCollider>();
        else
            Debug.Log("Дочірній об'єкт не знайдено!");
        try
        {
            GameObject obj = GameObject.Find("UnityTcpClient");
            utp = obj.GetComponent<UnityTcpClient>();
        }
        catch { };
    }
    private IEnumerator CheckForNavMeshLink()
    {
        while (true)
        {
            if (agent.isOnOffMeshLink)
            {
                yield return StartCoroutine(MoveAcrossLink()); // Плавний перехід
            }
            yield return null;
        }
    }

    private IEnumerator MoveAcrossLink()
    {
        OffMeshLinkData linkData = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = linkData.endPos;

        float duration = Vector3.Distance(startPos, endPos) / agent.speed; // Розрахунок часу
        float elapsedTime = 0f;

        agent.updatePosition = false; // Вимикаємо примусове оновлення позиції

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            Vector3 newPosition = Vector3.Lerp(startPos, endPos, t);
            agent.transform.position = newPosition;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        agent.transform.position = endPos; // Гарантуємо точне попадання в кінцеву точку
        agent.CompleteOffMeshLink();
        agent.updatePosition = true; // Вмикаємо оновлення позиції назад
    }
    protected void ArchimedPower()
    {
        float depth = depthBefore - buoyancyPoint.position.y; // Перевіряємо, наскільки об'єкт занурений у воду
        float displacementMultiplier = Mathf.Clamp01(depth / depthBefore) * displaycmentAmount;

        rb.AddForce(Vector3.up * Mathf.Abs(Physics.gravity.y) * displacementMultiplier, ForceMode.Acceleration);
    }
    protected void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water") && colliderForActionsWithWater.bounds.Intersects(other.bounds))
        {
            Debug.Log("Персонаж зайшов у воду!");
            inWater = true;
            isRunning = false;
        }
    }

    protected void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water") && !colliderForActionsWithWater.bounds.Intersects(other.bounds))
        {
            Debug.Log("Персонаж вийшов з води!");
            inWater = false;
            isSwimming = false;
            isStandingInWater = false;
        }
    }
  
    private void Update(){

        if (agent.nextPosition.y > 59.4 && hp>=0)
        {
            agent.updatePosition = true;

        }
        else
        {            
            agent.updatePosition = false; // Не оновлювати позицію агента
            Vector3 nextPos = agent.nextPosition;
            nextPos.y = rb.position.y;
            rb.MovePosition(nextPos);
        }

        if (hp <= 0)
        {
            if (!inWater)
            {
                isDie = true;
                isRunning = false;
                isStanding = false;
                isGathering = false;
            }
            else
            {
                isDrow = true;
            }
            agent.isStopped = true;

        }
        else if (!agent.pathPending && (agent.remainingDistance <= agent.stoppingDistance))
        {
            isRunning = false;
            isSwimming = false;
            if (isRunningToResource)
            {
                isGathering = true;
                if (targetResource.GetComponent<AmountResource>().typeResource == "Wood")
                {
                    Axe.SetActive(true);

                }
                else
                {
                    Pickaxe.SetActive(true);

                }
            }
            else
            {
                if (!inWater)
                {
                    isStanding = true;
                }
                else
                {
                    isStandingInWater = true;
                    ArchimedPower();

                }
            }
            agent.isStopped = true;
        } 
        else
        {
            if (!inWater) { 
                isRunning = true;
            }
            else
            {
                ArchimedPower();
                isSwimming = true;
            }
            isStanding = false;
            isStandingInWater=false;
            isGathering = false;
            agent.isStopped = false;
        }
    }
    protected IEnumerator UpdateHPBar(float newHP)
    {
        // Оновлюємо колір HP
        hpBarForeground.color = hpBarCollor.Evaluate(newHP / maxHP);

        // Якщо `Image.type = Filled`, використовуємо fillAmount
        if (hpBarForeground.type == Image.Type.Filled)
        {
            hpBarForeground.fillAmount = newHP / maxHP;
        }
        else // Якщо `Image.type = Simple`, змінюємо розмір
        {
            hpBarForeground.rectTransform.localScale = new Vector3(newHP / maxHP, 1, 1);
        }
        hp = newHP;

        // Чекаємо перед зменшенням чорного сліду (імітація втрати HP)
        yield return new WaitForSeconds(0.1f);

        // Плавне зменшення чорного HP-фону
        float elapsedTime = 0f;
        float startFill = hpBarBackground.fillAmount;

        while (elapsedTime < smoothSpeed)
        {
            elapsedTime += Time.deltaTime;
            hpBarBackground.fillAmount = Mathf.Lerp(startFill, newHP / maxHP, elapsedTime / smoothSpeed);
            yield return null;
        }

        hpBarBackground.fillAmount = newHP / maxHP;
    }

    public void getDamage(float damage)
    {
        float newHP = Mathf.Clamp(hp - damage, 0, maxHP);
        StartCoroutine(UpdateHPBar(newHP));
        if(newHP<=0)
        {
            agent.updatePosition = false;
        }
    }
   
    void CallMethod(Component component, string methodName)
    {
        Type type = component.GetType();
        var method = type.GetMethod(methodName);

        if (method != null)
        {
            method.Invoke(component, null); // Виклик методу без параметрів
        }
        else
        {
            Debug.LogWarning($"Метод '{methodName}' не знайдено в {type.Name}");
        }
    }
    public void MoveToResource(GameObject resource)
    {
        if (isExtracting)
        {
            StopExtracting();
        }
        isRunningToResource = true;

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

    private Vector3 GetClosestPointOnResource(GameObject resource, BoxCollider resourceCollider, BoxCollider agentCollider)
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
        Axe.SetActive(false);
        Pickaxe.SetActive(false);
        isRunningToResource = false;
        isExtracting = false;
        agent.isStopped = false;
        agent.ResetPath();

        if (extractionCoroutine != null)
        {
            StopCoroutine(extractionCoroutine);
            extractionCoroutine = null;
        }
    }
   
    public bool IsDie()
    { return isDie; }
    public bool IsStanding()
    { return isStanding; }
    public bool IsGathering()
    { return isGathering;    }
   public void IsRunningToResource(bool action)
    { isRunningToResource = action; }
    public bool IsRunning()
    { return isRunning;  }
    public bool IsStandingInWater()
    { return isStandingInWater;    }
    public bool IsDrow()
    { return isDrow;  }
    public bool IsSwimming() { return isSwimming; }
    public float GetHp()
    {
        return hp;
    }
    public GameObject GetTargetResource()
    {
        return targetResource;
    }
}
