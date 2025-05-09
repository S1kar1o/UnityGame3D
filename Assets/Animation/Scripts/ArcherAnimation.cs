using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class ArcherAnimation : WarriorAnimation
{
    [SerializeField] protected ArcherParametrs archerParametrs;
    private void Update()
    {
        animator.SetBool(IS_DIE, archerParametrs.IsDie());
        animator.SetBool(IS_STANDING, archerParametrs.IsStanding());
        animator.SetBool(IS_RUNNING, archerParametrs.IsRunning());
        animator.SetBool(IS_STANDING_IN_WATER, archerParametrs.IsStandingInWater());
        animator.SetBool(IS_DROW, archerParametrs.IsDrow());
        animator.SetBool(IS_SWIMMING, archerParametrs.IsSwimming());

        UnitSoundPlayer.Instance.HandleRunningSound(audioSource, archerParametrs.IsRunning());
        UnitSoundPlayer.Instance.HandleDeathSound(audioSource, archerParametrs.IsDie(), ref IsDie);
        UnitSoundPlayer.Instance.HandleSwimmingSound(audioSource, archerParametrs.IsSwimming());
        UnitSoundPlayer.Instance.HandleStandingInWaterSound(audioSource, archerParametrs.IsStandingInWater());
        UnitSoundPlayer.Instance.HandleDrownSound(audioSource, archerParametrs.IsDrow(), ref IsDie);

        AttackAnimationLogic();
        if (archerParametrs.IsDie() && !deathHandled)
        {
            deathHandled = true;
            StartCoroutine(HandleDeath());
        }
    }

    private IEnumerator HandleDeath()
    {
        // Чекаємо завершення анімації "Die"
        while (true)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Die") && state.normalizedTime >= 1.0f)
                break;

            yield return null;
        }

        // Затримка після смерті
        yield return new WaitForSeconds(5f);

        // Збираємо всі матеріали рендерерів
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        List<Material> materials = new List<Material>();

        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                // Налаштовуємо material для fade
                mat.SetFloat("_Mode", 2);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;

                materials.Add(mat);
            }
        }

        // Плавне зникнення (fade out)
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            foreach (var mat in materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Гарантовано встановити повну прозорість
        foreach (var mat in materials)
        {
            if (mat.HasProperty("_Color"))
            {
                Color color = mat.color;
                color.a = 0f;
                mat.color = color;
            }
        }

        // Позначити, що анімація завершена
        archerParametrs.animationOfDeathEnded = true;

        // Затримка перед знищенням (можна опустити)
        yield return new WaitForSeconds(1f);

        Destroy(gameObject); // або gameObject.SetActive(false);
    }
    private void AttackAnimationLogic()
    {
        animator.SetBool(IS_ATTACK, archerParametrs.IsAttack());

        if (archerParametrs.IsAttack())
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // Переконуємось, що анімація атаки IsAttack уже запущена
            if (stateInfo.IsName("Base Layer.IsAttack"))
            {
                // Якщо анімація атаки на 50% або більше
                if (stateInfo.normalizedTime >= 0.7f && Time.time >= nextActionTime)
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
