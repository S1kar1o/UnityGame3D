using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    public GameObject arrowPref; // Префаб стріли
    public string[] attackTags = { "Enemy", "Unit" }; // Теги для пошуку
    public GameObject target; // Поточна ціль
    public float attackRange = 200f; // Дальність атаки вежі
    public float attackCooldown = 1f; // Затримка між пострілами
    private bool isSearching;

    private float lastAttackTime; // Час останнього пострілу

    void Start()
    {
        lastAttackTime = -attackCooldown; // Щоб можна було стріляти одразу
    }

    void Update()
    {
        if (target == null && !isSearching)
        {
            StartCoroutine(FindTargetCoroutine());
        }
        else if (target != null)
        {
            // Якщо ціль вийшла за діапазон
            if (Vector3.Distance(transform.position, target.transform.position) > attackRange)
            {
                target = null;
            }
            // Атака, якщо готові
            else if (Time.time >= lastAttackTime + attackCooldown)
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

        foreach (Collider hit in hits)
        {
            // Перевірка тегу (оптимізовано через HashSet)
            if (System.Array.IndexOf(attackTags, hit.tag) != -1)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = hit.gameObject;
                }
            }

            // Пропускаємо кадр кожні 10 об'єктів
            if (System.Array.IndexOf(hits, hit) % 10 == 0)
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