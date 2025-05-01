using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcherParametrs : WarriorParametrs
{
    void Awake()
    {
        RANGE_ATTACK = 300;
        maxHP = 150;
        hp = 150; 
    }
    private void Update()
    {
        if (agent.nextPosition.y > 59.4)
        {
            agent.updatePosition = true;

        }
        else
        {
            agent.updatePosition = false; // �� ���������� ������� ������
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
  
}
