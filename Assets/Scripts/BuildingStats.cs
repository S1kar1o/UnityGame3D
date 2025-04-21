using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static System.Net.WebRequestMethods;

public class BuildingStats : MonoBehaviour
{

    public Gradient hpBarCollor;
    public Image hpBarBackground;
    public Image hpBarForeground;
    protected float smoothSpeed = 0.5f; // Швидкість анімації втрати HP

    public float maxHP = 100f;
    public float hp = 100;
    public void getDamage(float damage)
    {
        float newHP = Mathf.Clamp(hp - damage, 0, maxHP);
        StartCoroutine(UpdateHPBar(newHP));
        if (newHP <= 0)
        {
            Destroy(gameObject);
        }

    }
    public void Start()
    {
        hpBarForeground.color = hpBarCollor.Evaluate(hp / 100);
    }
    protected IEnumerator UpdateHPBar(float newHP)
    {
        // Оновлюємо колір HP
        hpBarForeground.color = hpBarCollor.Evaluate(newHP / maxHP);

        if (hpBarForeground.type == Image.Type.Filled)
        {
            hpBarForeground.fillAmount = newHP / maxHP;
        }
        else 
        {
            hpBarForeground.rectTransform.localScale = new Vector3(newHP / maxHP, 1, 1);
        }
        hp = newHP;

        // Чекаємо перед зменшенням чорного сліду (імітація втрати HP)
        yield return new WaitForSeconds(0.1f);

        // Плавне зменшення чорного HP-фону
        float elapsedTime = 0f;
        float startFill = hpBarBackground.fillAmount;

        while (elapsedTime < smoothSpeed)
        {
            elapsedTime += Time.deltaTime;
            hpBarBackground.fillAmount = Mathf.Lerp(startFill, newHP / maxHP, elapsedTime / smoothSpeed);
            yield return null;
        }

        hpBarBackground.fillAmount = newHP / maxHP;
    }
    public float GetHp()
    {
        return hp;
    }
}
