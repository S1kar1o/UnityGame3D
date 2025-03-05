using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HpBarRotation : MonoBehaviour
{
    public Image hpBarBackground;
    public Image hpBarForeground;
    void Update()
    {

        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0; // Фіксуємо нахил, щоб уникнути розтягування
        hpBarForeground.transform.rotation = transform.rotation = Quaternion.LookRotation(cameraForward);
        hpBarBackground.transform.rotation = transform.rotation = Quaternion.LookRotation(cameraForward);

    }
}
