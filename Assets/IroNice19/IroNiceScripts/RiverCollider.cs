using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RiverCollider : MonoBehaviour
{
    // Start is called before the first frame update
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Объект {collision.gameObject.name} с тегом {collision.gameObject.tag} вошёл в триггер {gameObject.name}");


    }
    private void OnCollisionExit(Collision collision)
    {
        Debug.Log($"Объект {collision.gameObject.name} с тегом {collision.gameObject.tag} вышел из триггер {gameObject.name}");


    }

}
   

