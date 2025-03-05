using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RiderAnimator : MonoBehaviour
{
    protected Animator animator;
    protected const string IS_RUNNING = "IsRunning";
    protected const string IS_STANDING = "IsStanding";
    protected const string IS_ATTACK = "IsAttack";
    protected const string IS_DIE = "IsDie";
    [SerializeField] private WarriorParametrs riderParametrs;
    protected float nextActionTime = 0f;
    protected float interval = 1.5f; // Інтервал у секундах
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void Update()
    {
        animator.SetBool(IS_DIE, riderParametrs.IsDie());
        animator.SetBool(IS_STANDING, riderParametrs.IsStanding());
        animator.SetBool(IS_RUNNING, riderParametrs.IsRunning());
        AttackAnimationLogic();
    }
    public void AttackAnimationLogic()
    {
        animator.SetBool(IS_ATTACK, riderParametrs.IsAttack());

        if (riderParametrs.IsAttack())
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // Переконуємось, що анімація атаки IsAttack уже запущена
            if (stateInfo.IsName("Base Layer.IsAttack"))
            {
               
                if (stateInfo.normalizedTime >= 0.5f && Time.time >= nextActionTime)
                {
                    nextActionTime = Time.time + interval;

                    if (riderParametrs.targetEnemy != null)
                    {
                        VillagerParametrs target = riderParametrs.targetEnemy.GetComponent<VillagerParametrs>();
                        target.getDamage(50);

                        if (target.GetHp() <= 0)
                        {
                            riderParametrs.targetEnemy = null;
                            riderParametrs.SetAttack(false);
                        }
                    }
                }
            }
        }
    }
}
