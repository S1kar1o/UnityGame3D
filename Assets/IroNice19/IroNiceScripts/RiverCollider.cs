using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RiverCollider : MonoBehaviour
{
    // Start is called before the first frame update
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"������ {collision.gameObject.name} � ����� {collision.gameObject.tag} ����� � ������� {gameObject.name}");


    }
    private void OnCollisionExit(Collision collision)
    {
        Debug.Log($"������ {collision.gameObject.name} � ����� {collision.gameObject.tag} ����� �� ������� {gameObject.name}");


    }

}
   

