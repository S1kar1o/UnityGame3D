using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
using UnityEngine.UI;
using System.Net.Sockets;
public class VillagerParametrs : MonoBehaviour
{
    [SerializeField] public Gradient hpBarCollor;
    public Image hpBarBackground;
    public Image hpBarForeground;
    protected float smoothSpeed = 0.5f; // Швидкість анімації втрати HP


    public Rigidbody rb;
    public NavMeshAgent agent;
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
    public BoxCollider colliderForActionsWithWater;
    private bool isGathering = false;
    protected float fallMultiplier = 2.5f;

    protected float depthBefore = 80f; // Глибина, на якій об'єкт починає витісняти воду
    public float displaycmentAmount = 6f; // Сила виштовхування
    public Transform buoyancyPoint; // Точка, яка визначає рівень занурення
    [SerializeField] private GameObject Axe, Pickaxe;

    public bool animationOfDeathEnded = false;
    public UnityTcpClient utp;

    public Vector3 targetPosition;
    public void Start()
    {

        hpBarForeground.color = hpBarCollor.Evaluate(hp / 100);

        agent.autoTraverseOffMeshLink = false; // Вимикаємо автоматичну телепортацію
        StartCoroutine(CheckForNavMeshLink());
       
        try
        {
            utp = UnityTcpClient.Instance;

        }
        catch { };
    }
    protected IEnumerator CheckForNavMeshLink()
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

    protected IEnumerator MoveAcrossLink()
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
            isStanding = false;
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

    private Vector3? targetPhysicsPosition = null; // Цільова позиція для rb.MovePosition()

    private void Update()
    {
        if (agent.isOnNavMesh && agent.isActiveAndEnabled)
        {
            if (agent.nextPosition.y > 59.4f && hp >= 0)
            {
                agent.updatePosition = true;
                targetPhysicsPosition = null;
            }
            else
            {
                agent.updatePosition = false;

                // Зберігаємо наступну позицію для FixedUpdate
                Vector3 nextPos = agent.nextPosition;
                nextPos.y = rb.position.y;
                targetPhysicsPosition = nextPos;
            }

            // Обробка смерті
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

                UnityTcpClient.Instance.cameraMoving.enemys.Remove(gameObject);
                agent.isStopped = true;
                return; // виходимо з Update, не виконуємо решту
            }

            // Агент дійшов до точки
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isRunning = false;
                isSwimming = false;

                if (isRunningToResource)
                {
                    isGathering = true;

                    if (targetResource.GetComponent<AmountResource>().typeResource == "Wood")
                        Axe.SetActive(true);
                    else
                        Pickaxe.SetActive(true);
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
            else // Агент ще рухається
            {
                if (!inWater)
                {
                    isRunning = true;
                }
                else
                {
                    ArchimedPower();
                    isSwimming = true;
                }

                isStanding = false;
                isStandingInWater = false;
                isGathering = false;
                agent.isStopped = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (targetPhysicsPosition.HasValue)
        {
            rb.MovePosition(targetPhysicsPosition.Value);
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
        Debug.Log(122);
        float newHP = Mathf.Clamp(hp - damage, 0, maxHP);
        StartCoroutine(UpdateHPBar(newHP));
        if(newHP<=0)
        {
            agent.updatePosition = false;
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
        // Отримуємо світові координати центру ресурсу
        Vector3 resourceCenter = resourceCollider.bounds.center;

        // Отримуємо розміри об'єктів у світових координатах
        Vector3 resourceSize = resourceCollider.bounds.size;
        Vector3 agentSize = agentCollider.bounds.size;

        // Визначаємо напрямок від агента до ресурсу
        Vector3 directionToResource = (resourceCenter - agentCollider.transform.position).normalized;

        // Розраховуємо точку на поверхні ресурсу з урахуванням розмірів обох об'єктів
        Vector3 closestPoint = resourceCenter - directionToResource * (resourceSize.magnitude + agentSize.magnitude) * 0.5f;

        // Обмежуємо точку межами ресурсу
        closestPoint.x = Mathf.Clamp(
            closestPoint.x,
            resourceCenter.x - resourceSize.x * 0.5f,
            resourceCenter.x + resourceSize.x * 0.5f
        );
        closestPoint.y = Mathf.Clamp(
            closestPoint.y,
            resourceCenter.y - resourceSize.y * 0.5f,
            resourceCenter.y + resourceSize.y * 0.5f
        );
        closestPoint.z = Mathf.Clamp(
            closestPoint.z,
            resourceCenter.z - resourceSize.z * 0.5f,
            resourceCenter.z + resourceSize.z * 0.5f
        );

        Debug.Log($"Resource Center: {resourceCenter}, Closest Point: {closestPoint}");
        return closestPoint;
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
            if (gameObject.tag == utp.tagOwner[utp.IDclient])
            {
                amrsc.Extraction(resourceAmountPerCycle);
                Debug.Log(amrsc.Amount);
            }
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
