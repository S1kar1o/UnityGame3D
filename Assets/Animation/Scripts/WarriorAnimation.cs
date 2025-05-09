using UnityEngine;

public class WarriorAnimation : VillagerAnimation
{
    protected const string IS_ATTACK = "IsAttack";

    protected float nextActionTime = 0f;
    protected float interval = 1.5f;
    protected bool IsDie = false;
    protected bool hasPlayedDrownSound = false;

    public AudioSource audioSource;

    [SerializeField] protected WarriorParametrs warriorParametrs;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource.playOnAwake = false;
    }

    private void Update()
    {
        animator.SetBool(IS_DIE, warriorParametrs.IsDie());
        animator.SetBool(IS_STANDING, warriorParametrs.IsStanding());
        animator.SetBool(IS_RUNNING, warriorParametrs.IsRunning());
        animator.SetBool(IS_STANDING_IN_WATER, warriorParametrs.IsStandingInWater());
        animator.SetBool(IS_DROW, warriorParametrs.IsDrow());
        animator.SetBool(IS_SWIMMING, warriorParametrs.IsSwimming());

        UnitSoundPlayer.Instance.HandleRunningSound(audioSource, warriorParametrs.IsRunning());
        UnitSoundPlayer.Instance.HandleDeathSound(audioSource, warriorParametrs.IsDie(), ref IsDie);
        UnitSoundPlayer.Instance.HandleSwimmingSound(audioSource, warriorParametrs.IsSwimming());
        UnitSoundPlayer.Instance.HandleStandingInWaterSound(audioSource, warriorParametrs.IsStandingInWater());
        UnitSoundPlayer.Instance.HandleDrownSound(audioSource, warriorParametrs.IsDrow(), ref IsDie);

        AttackAnimationLogic();
    }


    


    public void AttackAnimationLogic()
    {
        animator.SetBool(IS_ATTACK, warriorParametrs.IsAttack());

        if (warriorParametrs.IsAttack())
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.IsName("Base Layer.IsAttack") &&
                stateInfo.normalizedTime >= 0.5f &&
                Time.time >= nextActionTime)
            {
                nextActionTime = Time.time + interval;

                UnitSoundPlayer.Instance.Play(UnitSoundPlayer.Instance.attackSound, audioSource); 

                if (warriorParametrs.targetEnemy != null)
                {
                    VillagerParametrs target = warriorParametrs.targetEnemy.GetComponent<VillagerParametrs>();
                    if (target != null)
                    {
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