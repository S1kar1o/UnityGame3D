using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    public GameObject arrowPref; // Префаб стріли
    public GameObject target; // Поточна ціль
    public float attackRange = 200f; // Дальність атаки вежі
    public float attackCooldown = 1f; // Затримка між пострілами
    public bool isSearching;
    private bool enemy;
    private float lastAttackTime; // Час останнього пострілу

    void Start()
    {
        lastAttackTime = -attackCooldown; // Щоб можна було стріляти одразу
        if (gameObject.tag == UnityTcpClient.Instance.tagOwner[1-UnityTcpClient.Instance.IDclient])
        {
            enemy = true;
        }
    }

    void Update()
    {
        if (target == null && !isSearching)
        {
            StartCoroutine(FindTargetCoroutine());
        }
        else if (target != null)
        {
            VillagerParametrs villager = target.GetComponent<VillagerParametrs>();
            BuildingStats building = target.GetComponent<BuildingStats>();
            RiderParametrs rider = target.GetComponent<RiderParametrs>();
            // Якщо ціль вийшла за діапазон
            if (Vector3.Distance(transform.position, target.transform.position) > attackRange)
            {
                target = null;
            }
            else if (villager != null || building != null || rider != null)
            {
                bool isVillagerDead = villager != null && villager.GetHp() <= 0;
                bool isBuildingDead = building != null && building.GetHp() <= 0;
                bool isRiderDead = rider != null && rider.GetHp() <= 0;

                if (isVillagerDead || isBuildingDead || isRiderDead)
                {
                    target = null;
                    StartCoroutine(FindTargetCoroutine()); // Запускаємо пошук нової цілі
                    isSearching = false;
                }
            }

            // Атака, якщо готові
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Shoot();
                lastAttackTime = Time.time;
            }
        }
    }
    // Пошук найближчої цілі
    IEnumerator FindTargetCoroutine()
    {
        isSearching = true;
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        GameObject closestTarget = null;
        float closestDistance = Mathf.Infinity;

        int hitsCount = hits.Length; // Отримуємо кількість об'єктів

        for (int i = 0; i < hitsCount; i++)
        {
            Collider hit = hits[i];

            // Перевірка тегу
            if (enemy)
            {
                if (hit.CompareTag(UnityTcpClient.Instance.tagOwner[UnityTcpClient.Instance.IDclient])||hit.CompareTag("Building"))
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTarget = hit.gameObject;
                    }
                }

            }
            else if (hit.CompareTag(UnityTcpClient.Instance.tagOwner[1 - UnityTcpClient.Instance.IDclient]))
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = hit.gameObject;
                }
            }

            // Пропускаємо кадр кожні 10 об'єктів
            if (i % 10 == 0)
            {
                yield return null;
            }
        }

        target = closestTarget;
        isSearching = false;
    }


    // Стрілянина
    void Shoot()
    {
        if (arrowPref == null || target == null) return;

        // Створюємо стрілу
        GameObject arrow = Instantiate(arrowPref, transform.position, Quaternion.identity);
        ArrowController arrowScript = arrow.GetComponent<ArrowController>();

        // Передаємо параметри стрілі
        arrowScript.target = target.transform;
       
    }

    // Візуалізація дальності атаки в редакторі
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}