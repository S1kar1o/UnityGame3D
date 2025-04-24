using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcherAnimation : WarriorAnimation
{
    [SerializeField] protected ArcherParametrs archerParametrs;
    private void Update()
    {
/*        animator.SetBool(IS_DIE, archerParametrs.IsDie());
*/        animator.SetBool(IS_STANDING, archerParametrs.IsStanding());
        animator.SetBool(IS_RUNNING, archerParametrs.IsRunning());
   /*     animator.SetBool(IS_STANDING_IN_WATER, archerParametrs.IsStandingInWater());
        animator.SetBool(IS_DROW, archerParametrs.IsDrow());
        animator.SetBool(IS_SWIMMING, archerParametrs.IsSwimming());*/
        AttackAnimationLogic();
    }
    private void AttackAnimationLogic()
    {
        animator.SetBool(IS_ATTACK, archerParametrs.IsAttack());

        if (archerParametrs.IsAttack())
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // ѕереконуЇмось, що ан≥мац≥€ атаки IsAttack уже запущена
            if (stateInfo.IsName("Base Layer.IsAttack"))
            {
                // якщо ан≥мац≥€ атаки на 50% або б≥льше
                if (stateInfo.normalizedTime >= 0.5f && Time.time >= nextActionTime)
                {
                    nextActionTime = Time.time + interval;

                    if (archerParametrs.targetEnemy != null)
                    {
                        VillagerParametrs target = archerParametrs.targetEnemy.GetComponent<VillagerParametrs>();
                        if (target != null)
                        {
                            target.getDamage(50);
                            if (target.GetHp() <= 0)
                            {
                                archerParametrs.targetEnemy = null;
                                archerParametrs.SetAttack(false);
                            }
                        }
                        else
                        {
                            BuildingStats bd = archerParametrs.targetEnemy.GetComponent<BuildingStats>();
                            bd.getDamage(50);
                            if (bd.GetHp() <= 0)
                            {
                                archerParametrs.targetEnemy = null;
                                archerParametrs.SetAttack(false);
                            }
                        }
                    }
                }
            }
        }
    }
}
