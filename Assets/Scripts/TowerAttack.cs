using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    public GameObject arrowPref; // Префаб стріли
    private List<string> tegAttack = new List<string> { "Enemy", "Unit" }; // Теги цілей
    public GameObject target; // Поточна ціль
    public float attackRange = 200f; // Дальність атаки вежі
    public float attackCooldown = 1f; // Затримка між пострілами

    private float lastAttackTime; // Час останнього пострілу

    void Start()
    {
        lastAttackTime = -attackCooldown; // Щоб можна було стріляти одразу
    }

    void Update()
    {
        // Шукаємо ціль, якщо її немає
        if (target == null)
        {
            FindTarget();
        }

        // Якщо є ціль і минув час затримки, стріляємо
        if (target != null && Time.time >= lastAttackTime + attackCooldown)
        {
            Shoot();
            lastAttackTime = Time.time;
        }
    }

    // Пошук найближчої цілі
    void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        float closestDistance = 200;
        GameObject closestTarget = null;

        foreach (Collider hit in hits)
        {
            if (tegAttack.Contains(hit.tag)) // Перевіряємо тег
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = hit.gameObject;
                }
            }
        }

        target = closestTarget;
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