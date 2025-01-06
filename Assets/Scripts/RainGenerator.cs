using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainGenerator : MonoBehaviour
{
    private ParticleSystem _rain;
    private bool IsRain = false;
    public Light DirectionalLight;

    private void Start()
    {
        _rain = GetComponent<ParticleSystem>();
        StartCoroutine(Weather());
    }
    private void Update()
    {
        if (IsRain && DirectionalLight.intensity > 0.3f)
            LightIntensivity(-1);
        else if (!IsRain && DirectionalLight.intensity < 0.6f)
            LightIntensivity(1);
    }
    private void LightIntensivity(int intensiveChange)
    {
        DirectionalLight.intensity += 0.1f * intensiveChange * Time.deltaTime;
    }
    IEnumerator Weather() { 
        while (true) { 
            yield return new WaitForSeconds(UnityEngine.Random.Range(5f,30f));

            if (IsRain)
            {
                _rain.Stop();
            }
            else
            {
                _rain.Play();

            }
            IsRain = !IsRain;
        }
    }
}
