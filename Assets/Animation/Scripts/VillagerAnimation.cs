using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillagerAnimation : MonoBehaviour
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

    protected bool deathHandled = false;

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

        if (villagerParametrs.IsDie() && !deathHandled)
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
        villagerParametrs.animationOfDeathEnded = true;

        // Затримка перед знищенням (можна опустити)
        yield return new WaitForSeconds(1f);

        Destroy(gameObject); // або gameObject.SetActive(false);
    }
}
