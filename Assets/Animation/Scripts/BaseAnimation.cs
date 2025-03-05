using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HorseAnimation: MonoBehaviour
{
    protected Animator animator;
    protected const string IS_RUNNING = "IsRunning";
    protected const string IS_STANDING = "IsStanding";
    private const string IS_GATHERING = "IsGathering";
    protected const string IS_DIE = "IsDie";
    protected const string IS_SWIMMING = "IsSwimming";
    protected const string IS_DROW = "IsDrow";
    protected const string IS_STANDING_IN_WATER = "IsStandingInWater";
    [SerializeField] private VillagerParametrs villagerParametrs;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void Update()
    {
        animator.SetBool(IS_DIE, villagerParametrs.IsDie());
        animator.SetBool(IS_GATHERING, villagerParametrs.IsGathering());
        animator.SetBool(IS_STANDING, villagerParametrs.IsStanding());
        animator.SetBool(IS_RUNNING, villagerParametrs.IsRunning());
        animator.SetBool(IS_STANDING_IN_WATER, villagerParametrs.IsStandingInWater());
        animator.SetBool(IS_DROW, villagerParametrs.IsDrow());
        animator.SetBool(IS_SWIMMING, villagerParametrs.IsSwimming());
    }
}
