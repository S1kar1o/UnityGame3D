using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TimeLighting : MonoBehaviour
{
    [SerializeField] Gradient directionalLightGradient;
    [SerializeField] Gradient ambientLightGradient;
    [SerializeField, Range(1, 3600)] float timeDayInSeconds = 60;
    [SerializeField, Range(0,1f)] float timeProgress;
    [SerializeField] Light dirLight;
    Vector3 defaultAngles;
    private void Start()=> defaultAngles= dirLight.transform.localEulerAngles;
    
    private void Update()
    {
        if(Application.isPlaying)
             timeProgress += Time.deltaTime / timeDayInSeconds;
        if (timeProgress > 1f)
            timeProgress = 0f;
        dirLight.color=directionalLightGradient.Evaluate(timeProgress);
        RenderSettings.ambientLight= ambientLightGradient.Evaluate(timeProgress);
        dirLight.transform.localEulerAngles= new Vector3(360* timeProgress-90,defaultAngles.x,defaultAngles.z);
    }
}
