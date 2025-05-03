using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Mathematics;
public class WarriorParametrs : VillagerParametrs
{

    protected bool isAttack = false;
    public GameObject targetEnemy;
    protected float RANGE_ATTACK = 20.0f;
    protected float realDistance;
    protected int damage = 50;
    [SerializeField] protected float attackRange = 10f; // Радіус атаки лучника

    void Awake()
    {
        maxHP = 200;
        hp = 200; 
    }
    private void Update()
    {
        if (agent.nextPosition.y > 59.4)
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
                isAttack = false;

                isDie = true;
                isRunning = false;
                isStanding = false;
            }
            else
            {
                isDrow = true;
            }
            agent.isStopped = true;
            UnityTcpClient.Instance.cameraMoving.enemys.Remove(gameObject);

        }
        else if (!agent.pathPending && (agent.remainingDistance <= agent.stoppingDistance))
        {
            isRunning = false;
            if (targetEnemy)
            {
                if (!inWater)
                {
                    isAttack = true;
                    updateTargetPosition();
                }
                else
                {
                    isAttack = false;

                    ArchimedPower();
                    isStandingInWater = true;
                }
                isStanding = false;
                isSwimming = false;
                isRunning = false;
                agent.isStopped = true;
            }
            else
            {
                if (!inWater)
                {
                    isStanding = true;
                    isAttack = false;

                }
                else
                {
                    isStandingInWater = true;
                    isSwimming = false;
                    ArchimedPower();

                }
            }
            agent.isStopped = true;
        }
        else
        {
            if (!inWater)
            {
                isRunning = true;
                isAttack = false;
            }
            else
            {
                ArchimedPower();
                isSwimming = true;
            }
            isAttack = false;

            isStanding = false;
            isStandingInWater = false;
            agent.isStopped = false;
        }
    }
    protected void updateTargetPosition()
    {
        if (targetEnemy == null) return;

        Vector3 enemyPosition = targetEnemy.transform.position;
        Vector3 directionToEnemy = (enemyPosition - transform.position).normalized;
        float distanceToEnemy = Vector3.Distance(transform.position, enemyPosition);

        // Якщо вже в радіусі атаки - зупиняємося і атакуємо
        if (distanceToEnemy <= RANGE_ATTACK)
        {
            agent.ResetPath(); // Зупиняємо рух
                               // Тут можна додати виклик методу атаки, наприклад:
                               // Attack();
            return;
        }

        // Якщо не в радіусі атаки - рухаємось до цільової позиції
        Vector3 targetPosition = enemyPosition - directionToEnemy * RANGE_ATTACK;
        agent.SetDestination(targetPosition);

        Debug.DrawLine(transform.position, targetPosition, Color.red); // Для візуалізації в редакторі
    }
    public void AttackEnemy(GameObject target)
    {
        targetEnemy = target;
        updateTargetPosition();
    }
    public bool getDistance()
    {
        if (targetEnemy != null)
        {
            Vector3 closestPointA = targetEnemy.GetComponent<BoxCollider>().ClosestPoint(boxCollider.transform.position);
            Vector3 closestPointB = boxCollider.ClosestPoint(targetEnemy.GetComponent<BoxCollider>().transform.position);

            float distance = Vector3.Distance(closestPointA, closestPointB);
            realDistance = distance;

            return realDistance <= RANGE_ATTACK;
        }
        else return false;
    }
    public void SetAttack(bool state)
    {
        isAttack = state;
    }
    public bool IsAttack()
    {
        return isAttack;
    }
    public int GetDamage()
    {
        return damage;
    }
}
