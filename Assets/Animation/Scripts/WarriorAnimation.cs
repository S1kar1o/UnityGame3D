using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarriorAnimation : VillagerAnimation
{
    protected const string IS_ATTACK = "IsAttack";
    protected float nextActionTime = 0f;
    protected float interval = 1.5f; // ≤нтервал у секундах
    [SerializeField] protected WarriorParametrs warriorParametrs;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void Update()
    {
        animator.SetBool(IS_DIE, warriorParametrs.IsDie());
        animator.SetBool(IS_STANDING, warriorParametrs.IsStanding());
        animator.SetBool(IS_RUNNING, warriorParametrs.IsRunning());
        animator.SetBool(IS_STANDING_IN_WATER, warriorParametrs.IsStandingInWater());
        animator.SetBool(IS_DROW, warriorParametrs.IsDrow());
        animator.SetBool(IS_SWIMMING, warriorParametrs.IsSwimming());
        AttackAnimationLogic();
    }

    public void AttackAnimationLogic()
    {
        animator.SetBool(IS_ATTACK, warriorParametrs.IsAttack());

        if (warriorParametrs.IsAttack())
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // ѕереконуЇмось, що ан≥мац≥€ атаки IsAttack уже запущена
            if (stateInfo.IsName("Base Layer.IsAttack"))
            {
                // якщо ан≥мац≥€ атаки на 50% або б≥льше
                if (stateInfo.normalizedTime >= 0.5f && Time.time >= nextActionTime)
                {
                    nextActionTime = Time.time + interval;

                    if (warriorParametrs.targetEnemy != null)
                    {
                        VillagerParametrs target = warriorParametrs.targetEnemy.GetComponent<VillagerParametrs>();
                        if (target != null)
                        {
                            BuildingStats bd= warriorParametrs.targetEnemy.GetComponent<BuildingStats>();
                            target.getDamage(50);
                            if (target.GetHp() <= 0)
                            {
                                warriorParametrs.targetEnemy = null;
                                warriorParametrs.SetAttack(false);
                            }
                        }
                        else
                        {
                            BuildingStats bd = warriorParametrs.targetEnemy.GetComponent<BuildingStats>();
                            bd.getDamage(50);
                            if (bd.GetHp() <= 0)
                            {
                                warriorParametrs.targetEnemy = null;
                                warriorParametrs.SetAttack(false);
                            }
                        }
                    }
                }
            }
        }
    }
}
