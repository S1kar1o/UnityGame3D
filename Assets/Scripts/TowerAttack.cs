using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    public GameObject arrowPref; // ������ �����
    public string[] attackTags = { "Enemy", "Unit" }; // ���� ��� ������
    public GameObject target; // ������� ����
    public float attackRange = 200f; // �������� ����� ���
    public float attackCooldown = 1f; // �������� �� ���������
    private bool isSearching;

    private float lastAttackTime; // ��� ���������� �������

    void Start()
    {
        lastAttackTime = -attackCooldown; // ��� ����� ���� ������� ������
    }

    void Update()
    {
        if (target == null && !isSearching)
        {
            StartCoroutine(FindTargetCoroutine());
        }
        else if (target != null)
        {
            // ���� ���� ������ �� �������
            if (Vector3.Distance(transform.position, target.transform.position) > attackRange)
            {
                target = null;
            }
            // �����, ���� �����
            else if (Time.time >= lastAttackTime + attackCooldown)
            {
                Shoot();
                lastAttackTime = Time.time;
            }
        }
    }
    // ����� ��������� ���
    IEnumerator FindTargetCoroutine()
    {
        isSearching = true;
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        GameObject closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            // �������� ���� (����������� ����� HashSet)
            if (System.Array.IndexOf(attackTags, hit.tag) != -1)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = hit.gameObject;
                }
            }

            // ���������� ���� ���� 10 ��'����
            if (System.Array.IndexOf(hits, hit) % 10 == 0)
            {
                yield return null;
            }
        }

        target = closestTarget;
        isSearching = false;
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