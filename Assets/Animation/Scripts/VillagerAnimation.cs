using System.Collections;
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

    private bool deathHandled = false;

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
        // Чекаємо завершення анімації
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitWhile(() => state.IsName("Die") == false || state.normalizedTime < 1.0f);

        // Чекаємо 5 секунд
        yield return new WaitForSeconds(5f);

        // Починаємо зникнення (fade out)
        float duration = 2f; // тривалість зникнення
        float elapsed = 0f;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Material[] materials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            materials[i] = renderers[i].material;
            materials[i].SetFloat("_Mode", 2); // Fade mode
            materials[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            materials[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            materials[i].SetInt("_ZWrite", 0);
            materials[i].DisableKeyword("_ALPHATEST_ON");
            materials[i].EnableKeyword("_ALPHABLEND_ON");
            materials[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
            materials[i].renderQueue = 3000;
        }

        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            foreach (var mat in materials)
            {
                Color color = mat.color;
                color.a = alpha;
                mat.color = color;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        villagerParametrs.animationOfDeathEnded = true;
        yield return new WaitForSeconds(1f);
        Destroy(gameObject); // або gameObject.SetActive(false);
    }
}
