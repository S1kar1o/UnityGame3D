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
    protected bool deathHandled = false;

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
        if (riderParametrs.IsDie() && !deathHandled)
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
        riderParametrs.animationOfDeathEnded = true;

        // Затримка перед знищенням (можна опустити)
        yield return new WaitForSeconds(1f);

        Destroy(gameObject); // або gameObject.SetActive(false);

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

                if (stateInfo.normalizedTime >= 0.7f && Time.time >= nextActionTime)
                {
                    nextActionTime = Time.time + interval;

                    if (riderParametrs.targetEnemy != null)
                    {
                        VillagerParametrs target = riderParametrs.targetEnemy.GetComponent<VillagerParametrs>();
                        target.getDamage(50);
                        Debug.Log(120);
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
