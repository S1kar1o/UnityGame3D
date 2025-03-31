using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
using UnityEngine.UI;
using System.Threading.Tasks;
public class WarriorParametrs : VillagerParametrs
{

    protected int Hp = 200;
    protected bool isAttack = false;
    public GameObject targetEnemy;
    protected const float RANGE_ATTACK = 20.0f;
    protected float realDistance;
    protected int damage = 50;

    void Update()
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

        }
        else if (!agent.pathPending && (agent.remainingDistance <= agent.stoppingDistance))
        {
            isRunning = false;
            isSwimming = false;
            if (targetEnemy)
            {
                if (!inWater)
                {
                    isAttack = true;
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
    private void updateTargetPosition()
    {
        Debug.Log(targetEnemy.transform.position);
        agent.SetDestination(targetEnemy.transform.position);
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
