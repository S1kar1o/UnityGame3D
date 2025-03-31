using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    public GameObject arrowPref; // ������ �����
    private List<string> tegAttack = new List<string> { "Enemy", "Unit" }; // ���� �����
    public GameObject target; // ������� ����
    public float attackRange = 200f; // �������� ����� ���
    public float attackCooldown = 1f; // �������� �� ���������

    private float lastAttackTime; // ��� ���������� �������

    void Start()
    {
        lastAttackTime = -attackCooldown; // ��� ����� ���� ������� ������
    }

    void Update()
    {
        // ������ ����, ���� �� ����
        if (target == null)
        {
            FindTarget();
        }

        // ���� � ���� � ����� ��� ��������, ��������
        if (target != null && Time.time >= lastAttackTime + attackCooldown)
        {
            Shoot();
            lastAttackTime = Time.time;
        }
    }

    // ����� ��������� ���
    void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        float closestDistance = 200;
        GameObject closestTarget = null;

        foreach (Collider hit in hits)
        {
            if (tegAttack.Contains(hit.tag)) // ���������� ���
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

    // ���������
    void Shoot()
    {
        if (arrowPref == null || target == null) return;

        // ��������� �����
        GameObject arrow = Instantiate(arrowPref, transform.position, Quaternion.identity);
        ArrowController arrowScript = arrow.GetComponent<ArrowController>();

        // �������� ��������� ����
        arrowScript.target = target.transform;
       
    }

    // ³��������� �������� ����� � ��������
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}